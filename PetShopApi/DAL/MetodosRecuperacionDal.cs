using MySqlConnector;
using PetShopApi.Models;
using System.Data;
using static PetShopApi.DAL.MetodosRecuperacionDal;

namespace PetShopApi.DAL
{
    public class MetodosRecuperacionDal: IMetodosRecuperacionDal
    {
        private readonly ConexionFll _conexionFll;
        public interface IMetodosRecuperacionDal
        {
            List<MetodosRecuperacionMod> MetodosRecuperacion();
        }
        public MetodosRecuperacionDal(ConexionFll conexionFll)
        {
            _conexionFll = conexionFll;
        }
        public List<MetodosRecuperacionMod> MetodosRecuperacion()
        {
            try
            {
                using (var db = _conexionFll.ObtenerConexion())
                {
                    db.Open();
                    using (var cmd = new MySqlCommand("sp_ObtenerMetodosRecuperacion", db))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var lista = new List<MetodosRecuperacionMod>();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new MetodosRecuperacionMod
                                {
                                    Metodo = reader["Id"]?.ToString(),
                                    Etiqueta = reader["Etiqueta"]?.ToString(),
                                    Placeholder = reader["Placeholder"]?.ToString()
                                });
                            }
                        }
                        return lista;
                    }
                }
            }
            catch (Exception)
            {
                return new List<MetodosRecuperacionMod>();
            }
        }
    }
}
