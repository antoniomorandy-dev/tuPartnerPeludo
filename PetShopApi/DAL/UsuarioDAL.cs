using Microsoft.AspNetCore.Mvc.ModelBinding;
using MySqlConnector;
using PetShopApi.Mmodels;
using PetShopApi.Models;
using System.Data;
using System.Diagnostics.Eventing.Reader;

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
        public async Task<SalidaMod> RegistrarUsuario(Usuario user, string tokenEmail, string codigoWhatsApp)
        {
            //string codigoWhatsApp = new Random().Next(100000, 999999).ToString();
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

                    return new SalidaMod
                    {
                        Codigo = pCodigo.Value != null && pCodigo.Value != DBNull.Value ? Convert.ToInt32(pCodigo.Value) : 0,
                        Mensaje = pMensaje.Value?.ToString()
                    };
                }
            }
        }
        public async Task<SalidaMod> EliminaRegistroUsuario(Usuario user)
        {
            using (var conexion = _conexionFll.ObtenerConexion())
            {
                await conexion.OpenAsync();
                string sql = @"DELETE FROM Usuarios WHERE Email = @Email AND Telefono = @Telefono";
                using (var cmd = new MySqlCommand(sql, conexion))
                {
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Telefono", user.Telefono);

                    int filas = await cmd.ExecuteNonQueryAsync();
                    if (filas > 0)
                        return new SalidaMod { Codigo = 1, Mensaje = "Usuario eliminado correctamente." };
                    else
                        return new SalidaMod { Codigo = 0, Mensaje = "No se encontró un usuario." };
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
        public async Task<(Usuario? usuario, SalidaMod salida, string token)> Login(string email, string password, string token)
        {
            var salida = new SalidaMod();
            try
            {
                using (var conexion = _conexionFll.ObtenerConexion())
                {
                    await conexion.OpenAsync();

                    // Llamamos al SP para ver si el usuario puede intentar loguearse
                    using (var cmd = new MySqlCommand("sp_ValidarLogin", conexion))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@p_Email", email);
                        var pCodigo = new MySqlParameter("@p_Codigo", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
                        var pMensaje = new MySqlParameter("@p_Mensaje", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
                        cmd.Parameters.Add(pCodigo); cmd.Parameters.Add(pMensaje);
                        await cmd.ExecuteNonQueryAsync();

                        int codigo = Convert.ToInt32(pCodigo.Value);
                        if (codigo <= 0)
                        {
                            salida.Codigo = codigo; salida.Mensaje = pMensaje.Value != null ? pMensaje.Value.ToString() : string.Empty;
                            return (null, salida, token);
                        }
                    }

                    // Si llegamos aquí, el usuario existe y no está bloqueado. Obtenemos sus datos reales.
                    var cmdUser = new MySqlCommand("SELECT UsuarioID, Nombre, Email, PasswordHash, IntentosFallidos FROM Usuarios WHERE Email = @Email", conexion);
                    cmdUser.Parameters.AddWithValue("@Email", email);

                    using (var reader = await cmdUser.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var usuario = new Usuario
                            {
                                UsuarioID = (int)reader["UsuarioID"],
                                Nombre = reader["Nombre"].ToString(),
                                IntentosFallidos = reader["IntentosFallidos"] != DBNull.Value ? (int)reader["IntentosFallidos"] : 0
                            };
                            string hash = reader["PasswordHash"]?.ToString() ?? string.Empty;
                            reader.Close();

                            // VERIFICACIÓN DE PASSWORD
                            if (BCrypt.Net.BCrypt.Verify(password, hash))
                            {
                                // ÉXITO: Resetear intentos
                                await EjecutarQuery("UPDATE Usuarios SET IntentosFallidos = 0, FechaBloqueo = NULL WHERE Email = @Email", email, conexion);
                                // Crear sesión (código previo de insertar SesionesActivas...)
                                salida.Codigo = 1; salida.Mensaje = "¡Bienvenido!";
                                return (usuario, salida, token);
                            }
                            else
                            {
                                // FALLO: Incrementar intentos
                                int nuevosIntentos = (usuario.IntentosFallidos ?? 0) + 1;
                                string msg = "Contraseña incorrecta.";

                                if (nuevosIntentos >= 3)
                                {
                                    await EjecutarQuery("UPDATE Usuarios SET IntentosFallidos = @Intentos, FechaBloqueo = DATE_ADD(NOW(), INTERVAL 5 MINUTE) WHERE Email = @Email", email, conexion, nuevosIntentos);
                                    salida.Mensaje = "Cuenta bloqueada por 5 minutos.";
                                }
                                else
                                {
                                    await EjecutarQuery("UPDATE Usuarios SET IntentosFallidos = @Intentos WHERE Email = @Email", email, conexion, nuevosIntentos);

                                    if (nuevosIntentos == 2)
                                        salida.Mensaje = "Contraseña incorrecta. Te queda 1 intento antes del bloqueo.";
                                    else
                                        salida.Mensaje = "Contraseña incorrecta.";
                                }

                                salida.Codigo = 0; salida.Mensaje = msg;
                                return (null, salida, token);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { salida.Codigo = -1; salida.Mensaje = ex.Message; }
            return (null, salida, token);
        }
        private async Task EjecutarQuery(string query, string email, MySqlConnection conexion, int? intentos = null)
        {
            using (var cmd = new MySqlCommand(query, conexion))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                if (intentos.HasValue)
                {
                    cmd.Parameters.AddWithValue("@Intentos", intentos.Value);
                }
                await cmd.ExecuteNonQueryAsync();
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
        public async Task<Usuario> ObtenerPorTelefono(string telefono, SalidaMod salida)
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
        public async Task<Usuario?> ObtenerPorEmail(string email, SalidaMod salida)
        {
            Usuario? usuario = null;
            try
            {
                using (var conexion = _conexionFll.ObtenerConexion())
                {
                    await conexion.OpenAsync();
                    string query = "SELECT UsuarioID, Nombre, Email, Telefono FROM Usuarios WHERE Email = @Email";

                    using (var cmd = new MySqlCommand(query, conexion))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                usuario = new Usuario
                                {
                                    UsuarioID = Convert.ToInt32(reader["UsuarioID"]),
                                    Nombre = reader["Nombre"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    Telefono = reader["Telefono"].ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                salida.Codigo = -1;
                salida.Mensaje = "Error al consultar usuario: " + ex.Message;
            }
            return usuario;
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
        public async Task<bool> ValidarTokenEnBD(string token)
        {
            bool esValido = false;
            try
            {
                using (var conexion = _conexionFll.ObtenerConexion())
                {
                    string deleteQuery = "DELETE FROM SesionesActivas WHERE FechaExpiracion < GETDATE()";

                    using (var cmdDel = new MySqlCommand(deleteQuery, conexion))
                    {
                        cmdDel.ExecuteNonQuery();
                    }
                    string sql = "SELECT COUNT(*) FROM SesionesActivas WHERE Token = @Token AND FechaExpiracion > GETDATE()";

                    using (var cmd = new MySqlCommand(sql, conexion))
                    {
                        cmd.Parameters.AddWithValue("@Token", token);
                        int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        esValido = (count > 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error validando token: " + ex.Message);
                esValido = false;
            }
            return esValido;
        }
    }
}