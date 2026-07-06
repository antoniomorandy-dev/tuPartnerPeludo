using MySqlConnector;
using PetShopApi.Models;

namespace PetShopApi.DAL
{
    public class ProductosDal
    {
        public readonly ConexionFll _conexionFll;
        public ProductosDal(ConexionFll conexionFll)
        {
            _conexionFll = conexionFll;
        }
        public (SalidaMod salida, List<ProductosMod> productos) ObtenerProductos()
        {
            try
            {
                var lista = new List<ProductosMod>();
                using (var db = _conexionFll.ObtenerConexion())
                {
                    db.Open();
                    var cmd = new MySqlCommand("SELECT * FROM Productos", db);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new ProductosMod
                            {
                                Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                                Nombre = reader["Nombre"].ToString() ?? string.Empty,
                                Descripcion = reader["Descripcion"].ToString() ?? string.Empty,
                                Precio = reader["Precio"] != DBNull.Value ? Convert.ToDecimal(reader["Precio"]) : 0,
                                Stock = reader["Stock"] != DBNull.Value ? Convert.ToInt32(reader["Stock"]) : 0,
                                UrlImagen = reader["UrlImagen"].ToString() ?? string.Empty
                            });
                        }
                    }
                }
                return (new SalidaMod { Codigo = 1, Mensaje = "Productos obtenidos correctamente" }, lista);
            }
            catch (Exception ex)
            {
                return (new SalidaMod { Codigo = -1, Mensaje = ex.Message }, new List<ProductosMod>());
            }
        }
        public (SalidaMod, ProductosMod) GuardarProducto(ProductosMod producto)
        {
            try
            {
                using (var db = _conexionFll.ObtenerConexion())
                {
                    db.Open();
                    using (var cmd = new MySqlCommand("INSERT INTO Productos (Nombre, Descripcion, Precio, Stock, UrlImagen) VALUES (@Nombre, @Descripcion, @Precio, @Stock, @UrlImagen)", db))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", producto.Nombre);
                        cmd.Parameters.AddWithValue("@Descripcion", producto.Descripcion);
                        cmd.Parameters.AddWithValue("@Precio", producto.Precio);
                        cmd.Parameters.AddWithValue("@Stock", producto.Stock);
                        cmd.Parameters.AddWithValue("@UrlImagen", producto.UrlImagen);
                        cmd.ExecuteNonQuery();
                    }
                }
                return (new SalidaMod { Codigo = 1, Mensaje = "Producto guardado correctamente" }, producto);
            }
            catch (Exception ex)
            {
                return (new SalidaMod { Codigo = -1, Mensaje = ex.Message }, producto);
            }
        }
        public SalidaMod ActualizarProducto(int id, ProductosMod producto)
        {
            try
            {
                using (var db = _conexionFll.ObtenerConexion())
                {
                    db.Open();
                    string query = @"UPDATE Productos 
                             SET Nombre = @Nombre, 
                                 Descripcion = @Descripcion, 
                                 Precio = @Precio, 
                                 UrlImagen = @UrlImagen,
                                 Stock = @Stock
                             WHERE Id = @Id";

                    using (var cmd = new MySqlCommand(query, db))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Nombre", producto.Nombre);
                        cmd.Parameters.AddWithValue("@Descripcion", producto.Descripcion);
                        cmd.Parameters.AddWithValue("@Precio", producto.Precio);
                        cmd.Parameters.AddWithValue("@Stock", producto.Stock);
                        cmd.Parameters.AddWithValue("@UrlImagen", producto.UrlImagen);

                        int filasAfectadas = cmd.ExecuteNonQuery();

                        if (filasAfectadas > 0)
                        {
                            return new SalidaMod { Codigo = 1, Mensaje = "Producto actualizado con éxito" };
                        }
                        else
                        {
                            return new SalidaMod { Codigo = 0, Mensaje = "No se encontró el producto para actualizar" };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new SalidaMod { Codigo = -1, Mensaje = ex.Message };
            }
        }
        public SalidaMod EliminarProducto(int id)
        {
            try
            {
                using (var db = _conexionFll.ObtenerConexion())
                {
                    db.Open();
                    string query = "DELETE FROM Productos WHERE Id = @Id";
                    using (var cmd = new MySqlCommand(query, db))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        int filasAfectadas = cmd.ExecuteNonQuery();
                        if (filasAfectadas > 0)
                        {
                            return new SalidaMod { Codigo = 1, Mensaje = "Producto eliminado con éxito" };
                        }
                        else
                        {
                            return new SalidaMod { Codigo = 0, Mensaje = "No se encontró el producto para eliminar" };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new SalidaMod { Codigo = -1, Mensaje = ex.Message };
            }
        }
        public (SalidaMod, ProductosMod) ObtenerProductoPorId(int id)
        {
            ProductosMod producto = new ProductosMod();
            SalidaMod salida = new SalidaMod();
            try
            {
                using (var db = _conexionFll.ObtenerConexion())
                {
                    db.Open();
                    string query = "SELECT * FROM Productos WHERE Id = @Id";
                    using (var cmd = new MySqlCommand(query, db))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                producto = new ProductosMod
                                {
                                    Id = reader.GetInt32("Id"),
                                    Nombre = reader.GetString("Nombre"),
                                    Descripcion = reader.GetString("Descripcion"),
                                    Precio = reader.GetDecimal("Precio"),
                                    Stock = reader.GetInt32("Stock"),
                                    UrlImagen = reader.GetString("UrlImagen")
                                };
                                salida = new SalidaMod { Codigo = 1, Mensaje = "Producto encontrado" };
                                return (salida, producto);
                            }
                            else
                            {
                                salida = new SalidaMod { Codigo = 0, Mensaje = "Producto no encontrado" };
                                return (salida, producto);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                salida = new SalidaMod { Codigo = -1, Mensaje = ex.Message };
                return (salida, producto);
            }
        }
    }
}