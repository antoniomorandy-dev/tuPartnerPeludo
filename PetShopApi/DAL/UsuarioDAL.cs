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
        public async Task<(int? codigo, string? mensaje)> RegistrarUsuario(Usuario user, string tokenEmail, string codigoWhatsApp)
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

                    return (pCodigo.Value is int codigo ? codigo : (int?)null, pMensaje.Value?.ToString());
                }
            }
        }
        public async Task<(SalidaMod)> EliminaRegistroUsuario(Usuario user)
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
                            // 1. Intentamos leer la fila del usuario
                            if (await reader.ReadAsync())
                            {
                                usuarioEncontrado = new Usuario
                                {
                                    UsuarioID = Convert.ToInt32(reader["UsuarioID"]),
                                    Nombre = reader["Nombre"]?.ToString(),
                                    Email = reader["Email"]?.ToString(),
                                    Telefono = reader["Telefono"]?.ToString(),
                                    IntentosFallidos = reader["IntentosFallidos"] != DBNull.Value ? Convert.ToInt32(reader["IntentosFallidos"]) : 0,
                                    FechaBloqueo = reader["FechaBloqueo"] != DBNull.Value ? Convert.ToDateTime(reader["FechaBloqueo"]) : (DateTime?)null
                                };
                                string hashAlmacenado = reader["PasswordHash"]?.ToString() ?? string.Empty;
                                
                                reader.Close();

                                if (usuarioEncontrado.FechaBloqueo.HasValue)
                                {
                                    TimeSpan tiempoTranscurrido = DateTime.Now - usuarioEncontrado.FechaBloqueo.Value;

                                    if (tiempoTranscurrido.TotalMinutes < 15)
                                    {
                                        int minutosRestantes = 15 - (int)Math.Floor(tiempoTranscurrido.TotalMinutes);
                                        salida.Codigo = 0;
                                        salida.Mensaje = $"Cuenta bloqueada por seguridad. Intente nuevamente en {minutosRestantes} minutos.";
                                        return (usuarioEncontrado, salida, token);
                                    }
                                    else 
                                    { 
                                        string queryDes = "UPDATE Usuarios SET IntentosFallidos = 0, FechaBloqueo = NULL WHERE Email = @Email";
                                        using (var cmdDesbloq = new MySqlCommand(queryDes, conexion))
                                        {
                                            cmdDesbloq.Parameters.AddWithValue("@Email", email);
                                            cmdDesbloq.ExecuteNonQuery();
                                        }
                                    }
                                }

                                int codigo = Convert.ToInt32(pCodigo.Value);
                                string? mensaje = pMensaje.Value?.ToString();

                                string deleteQuery = "DELETE FROM SesionesActivas WHERE UsuarioID = @UsuarioID";
                                using (var cmdDel = new MySqlCommand(deleteQuery, conexion))
                                {
                                    cmdDel.Parameters.AddWithValue("@UsuarioID", usuarioEncontrado.UsuarioID);
                                    cmdDel.ExecuteNonQuery();
                                }

                                string query = "";

                                if (codigo == 1 && BCrypt.Net.BCrypt.Verify(password, hashAlmacenado))
                                {
                                    
                                    query = "INSERT INTO SesionesActivas (UsuarioID, Token, FechaExpiracion) VALUES (@UsuarioID, @Token, DATE_ADD(NOW(), INTERVAL 1 DAY))";
                                    using (var cmd2 = new MySqlCommand(query, conexion))
                                    {
                                        cmd2.Parameters.AddWithValue("@UsuarioID", usuarioEncontrado.UsuarioID);
                                        cmd2.Parameters.AddWithValue("@Token", token);
                                        cmd2.ExecuteNonQuery();
                                    }
                                    query = "UPDATE Usuarios SET IntentosFallidos = 0, FechaBloqueo = NULL WHERE Email = @Email";
                                    using (var cmdUpd = new MySqlCommand(query, conexion))
                                    {
                                        cmdUpd.Parameters.AddWithValue("@Email", email);
                                        cmdUpd.ExecuteNonQuery();
                                    }
                                    salida.Codigo = codigo;
                                    salida.Mensaje = mensaje;
                                    return (usuarioEncontrado, salida, token);
                                }
                                else
                                {
                                    query = "UPDATE Usuarios SET IntentosFallidos = IntentosFallidos + 1 WHERE Email = @Email";
                                    using (var cmdInt = new MySqlCommand(query, conexion))
                                    {
                                        cmdInt.Parameters.AddWithValue("@Email", email);
                                        cmdInt.ExecuteNonQuery();
                                    }
                                    if (usuarioEncontrado.IntentosFallidos >= 3)
                                    {
                                        DateTime fechaBloqueo = DateTime.Now.AddMinutes(15);
                                        query = "UPDATE Usuarios SET FechaBloqueo = @FechaBloqueo WHERE Email = @Email AND (FechaBloqueo IS NULL OR FechaBloqueo <= NOW())";
                                        using (var cmdBloq = new MySqlCommand(query, conexion))
                                        {
                                            cmdBloq.Parameters.AddWithValue("@FechaBloqueo", fechaBloqueo);
                                            cmdBloq.Parameters.AddWithValue("@Email", email);
                                            cmdBloq.ExecuteNonQuery();
                                        }
                                        TimeSpan diferencia = fechaBloqueo - DateTime.Now;
                                        int minutosFaltantes = (int)Math.Ceiling(diferencia.TotalMinutes);
                                        if (minutosFaltantes > 0)
                                        {
                                            salida.Codigo = 0;
                                            salida.Mensaje = $"Cuenta bloqueada por 15 minutos. Intente nuevamente en {minutosFaltantes} minutos.";
                                            return (usuarioEncontrado, salida, token);
                                        }
                                        else
                                        {
                                            query = "UPDATE Usuarios SET IntentosFallidos = 0, FechaBloqueo = NULL WHERE Email = @Email";
                                            using (var cmdDesBloq = new MySqlCommand(query, conexion))
                                            {
                                                cmdDesBloq.Parameters.AddWithValue("@Email", email);
                                                cmdDesBloq.ExecuteNonQuery();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        salida.Codigo = 0;
                                        salida.Mensaje = $"Credenciales incorrectas. Intentos fallidos: {usuarioEncontrado.IntentosFallidos}";
                                    }
                                    return (usuarioEncontrado, salida, token);
                                }
                            }
                            else
                            {
                                // Usuario no existe (el reader no devolvió filas)
                                reader.Close();
                                salida.Codigo = Convert.ToInt32(pCodigo.Value);
                                salida.Mensaje = pMensaje.Value?.ToString() ?? "Usuario no encontrado.";
                                return (null, salida, token);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                salida.Codigo = -1;
                salida.Mensaje = "Error interno: " + ex.Message;
                return (null, salida, token);
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