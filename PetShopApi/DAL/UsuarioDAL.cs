using MySqlConnector;
using PetShopApi.Mmodels;
using PetShopApi.Models;
using System.Data;

namespace PetShopApi.DAL
{
    public class UsuarioDAL
    {
        private readonly ConexionFll _conexionFll;

        public UsuarioDAL(ConexionFll conexionFll)
        {
            _conexionFll = conexionFll;
        }
        //public async Task<bool> RegistrarUsuario(Usuario user, string tokenEmail, string codigoWhatsApp)
        //{
        //    using (var conexion = _conexionFll.ObtenerConexion())
        //    {
        //        await conexion.OpenAsync();

        //        string sql = @"INSERT INTO Usuarios 
        //               (Nombre, Apellido, Email, Telefono, PasswordHash, TokenValidacion, CodigoWhatsApp, EmailValidado) 
        //               VALUES 
        //               (@Nombre, @Apellido, @Email, @Telefono, @Pass, @TokenEmail, @CodigoWS, 0)";

        //        using (var cmd = new MySqlCommand(sql, conexion))
        //        {
        //            cmd.Parameters.AddWithValue("@Nombre", user.Nombre);
        //            cmd.Parameters.AddWithValue("@Apellido", user.Apellido ?? "");
        //            cmd.Parameters.AddWithValue("@Email", user.Email);
        //            cmd.Parameters.AddWithValue("@Telefono", user.Telefono);
        //            cmd.Parameters.AddWithValue("@Pass", BCrypt.Net.BCrypt.HashPassword(user.Password));

        //            cmd.Parameters.AddWithValue("@TokenEmail", tokenEmail);
        //            cmd.Parameters.AddWithValue("@CodigoWS", codigoWhatsApp);

        //            int filasAfectadas = await cmd.ExecuteNonQueryAsync();
        //            return filasAfectadas > 0;
        //        }
        //    }
        //}
        public async Task<(int? codigo, string? mensaje)> RegistrarUsuario(Usuario user, string tokenEmail, string codigoWhatsApp)
        {
            using (var conexion = _conexionFll.ObtenerConexion())
            {
                await conexion.OpenAsync();
                using (var cmd = new MySqlCommand("sp_RegistrarUsuario", conexion))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Parámetros de ENTRADA (IN)
                    cmd.Parameters.AddWithValue("@p_Nombre", user.Nombre);
                    cmd.Parameters.AddWithValue("@p_Apellido", user.Apellido?? "Sin apellido");
                    cmd.Parameters.AddWithValue("@p_Email", user.Email);
                    cmd.Parameters.AddWithValue("@p_Password", BCrypt.Net.BCrypt.HashPassword(user.Password));
                    cmd.Parameters.AddWithValue("@p_Telefono", user.Telefono);
                    cmd.Parameters.AddWithValue("@p_TokenEmail", tokenEmail);
                    cmd.Parameters.AddWithValue("@p_CodigoWS", codigoWhatsApp);

                    // Parámetros de SALIDA (OUT)
                    var pCodigo = new MySqlParameter("@p_codigo", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
                    var pMensaje = new MySqlParameter("@p_mensaje", MySqlDbType.VarChar) { Size = 255, Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(pCodigo);
                    cmd.Parameters.Add(pMensaje);

                    await cmd.ExecuteNonQueryAsync();

                    return (pCodigo.Value is int codigo ? codigo : (int?)null, pMensaje.Value?.ToString());
                }
            }
        }
        public async Task<bool> RegistrarUsuarioConToken(Usuario user, string token)
        {
            using (var conexion = _conexionFll.ObtenerConexion())
            {
                await conexion.OpenAsync();
                string sql = @"INSERT INTO Usuarios (Nombre, Apellido, Email, PasswordHash, TokenValidacion, EmailValidado) 
                       VALUES (@Nombre, @Apellido, @Email, @Pass, @Token, 0)";

                using (var cmd = new MySqlCommand(sql, conexion))
                {
                    cmd.Parameters.AddWithValue("@Nombre", user.Nombre);
                    cmd.Parameters.AddWithValue("@Apellido", user.Apellido ?? "");
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Pass", BCrypt.Net.BCrypt.HashPassword(user.Password));
                    cmd.Parameters.AddWithValue("@Token", token);

                    int filas = await cmd.ExecuteNonQueryAsync();
                    return filas > 0;
                }
            }
        }
        public async Task<bool> ConfirmarEmail(string token)
        {
            using (var conexion = _conexionFll.ObtenerConexion())
            {
                await conexion.OpenAsync();
                string sql = @"UPDATE Usuarios 
                       SET EmailValidado = 1, TokenValidacion = NULL 
                       WHERE TokenValidacion = @token";

                using (var cmd = new MySqlCommand(sql, conexion))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    int filasAfec = await cmd.ExecuteNonQueryAsync();
                    return filasAfec > 0;
                }
            }
        }
        public async Task<(Usuario? usuario, SalidaMod salida)> Login(string email, string password)
        {
            var salida = new SalidaMod();
            Usuario? usuarioEncontrado = null;
            try
            {
                using (var conexion = _conexionFll.ObtenerConexion())
                {
                    await conexion.OpenAsync();
                    using (var cmd = new MySqlCommand("sp_ValidarLogin", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@p_Email", email);

                        var pCodigo = new MySqlParameter("@p_Codigo", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
                        var pMensaje = new MySqlParameter("@p_Mensaje", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
                        cmd.Parameters.Add(pCodigo);
                        cmd.Parameters.Add(pMensaje);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string hashAlmacenado = reader["PasswordHash"].ToString() ?? string.Empty;

                                if (BCrypt.Net.BCrypt.Verify(password, hashAlmacenado))
                                {
                                    usuarioEncontrado = new Usuario
                                    {
                                        Nombre = reader["Nombre"].ToString(),
                                        Email = reader["Email"].ToString(),
                                        Telefono = reader["Telefono"].ToString()
                                    };
                                }
                                else
                                {
                                    salida.Codigo = 0; salida.Mensaje = "La contraseña ingresada es incorrecta.";
                                    return (usuarioEncontrado, salida);
                                }
                            }
                        }
                        salida.Codigo = Convert.ToInt32(pCodigo.Value);
                        salida.Mensaje = pMensaje.Value?.ToString();
                    }
                }
                return (usuarioEncontrado, salida);
            }
            catch (Exception ex)
            {
                salida.Codigo = -1; salida.Mensaje = ex.Message;
                return (usuarioEncontrado, salida);
            }
        }
        public async Task<bool> ValidarCodigoWhatsApp(string email, string codigo)
        {
            using (var conexion = _conexionFll.ObtenerConexion())
            {
                await conexion.OpenAsync();

                string sql = @"UPDATE Usuarios 
                       SET EstaValidado = 1, EmailValidado = 1 
                       WHERE Email = @Email AND CodigoWhatsApp = @Codigo";

                using (var cmd = new MySqlCommand(sql, conexion))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Codigo", codigo);

                    int filasAfectadas = await cmd.ExecuteNonQueryAsync();

                    return filasAfectadas > 0;
                }
            }
        }
        // Método 1: Buscar usuario por teléfono
        public async Task<Usuario> ObtenerPorTelefono(string telefono,SalidaMod salida)
        {
            try
            {
                Usuario usuario = new();
                using (var conexion = _conexionFll.ObtenerConexion())
                {
                    await conexion.OpenAsync();
                    string sql = "SELECT * FROM Usuarios WHERE Telefono = @Telefono LIMIT 1";

                    using var cmd = new MySqlCommand(sql, conexion);
                    cmd.Parameters.AddWithValue("@Telefono", telefono);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        return new Usuario
                        {
                            UsuarioID = reader.GetInt32("UsuarioID"),
                            Nombre = reader.GetString("Nombre"),
                            Telefono = reader.GetString("Telefono")
                            // Agrega aquí los demás campos que necesites mapear
                        };
                    }
                }
                salida = new SalidaMod { Codigo = 0, Mensaje = "No se encontró un usuario con ese teléfono." };
                return usuario;
            }
            catch (Exception ex)
            {
                salida = new SalidaMod { Codigo = -1, Mensaje = $"Ocurrió un error al buscar el usuario: {ex.Message}" };
                return new Usuario(); // Retorna un usuario vacío en caso de error
            }
            
        }

        // Método 2: Guardar el Token de recuperación
        public async Task<bool> ActualizarTokenRecuperacion(int idUsuario, string token)
        {
            try
            {
                DateTime expiracion = DateTime.Now.AddMinutes(15); // El token expirará en 15 minutos
                using var conexion = _conexionFll.ObtenerConexion();
                await conexion.OpenAsync();
                string sql = @"UPDATE Usuarios 
                       SET TokenRecuperacion = @Token, 
                           FechaExpiracionToken = @Expiracion 
                       WHERE IdUsuario = @Id";

                using var cmd = new MySqlCommand(sql, conexion);
                cmd.Parameters.AddWithValue("@Token", token);
                cmd.Parameters.AddWithValue("@Expiracion", expiracion);
                cmd.Parameters.AddWithValue("@Id", idUsuario);

                int filas = await cmd.ExecuteNonQueryAsync();
                //salida = new SalidaMod { Codigo = filas > 0 ? 1 : 0, Mensaje = filas > 0 ? "Token actualizado correctamente." : "No se pudo actualizar el token." };
                return filas > 0;
            }
            catch (Exception)
            {
                //salida = new SalidaMod { Codigo = -1, Mensaje = "Ocurrió un error al actualizar el token." };
                return false;
            }
        }
        public async Task<bool> RestablecerPasswordFinal(string token, string nuevoPasswordHash)
        {
            using (var conexion = _conexionFll.ObtenerConexion())
            {
                await conexion.OpenAsync();

                // Solo actualiza si el token coincide y no ha pasado la fecha de expiración
                string sql = @"UPDATE Usuarios 
                       SET PasswordHash = @Pass, 
                           TokenRecuperacion = NULL, 
                           FechaExpiracionToken = NULL 
                       WHERE TokenRecuperacion = @Token 
                       AND FechaExpiracionToken > @Ahora";

                using (var cmd = new MySqlCommand(sql, conexion))
                {
                    cmd.Parameters.AddWithValue("@Pass", nuevoPasswordHash);
                    cmd.Parameters.AddWithValue("@Token", token);
                    cmd.Parameters.AddWithValue("@Ahora", DateTime.Now);

                    int filasAfectadas = await cmd.ExecuteNonQueryAsync();
                    return filasAfectadas > 0;
                }
            }
        }
    }
}