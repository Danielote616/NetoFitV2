
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers
{
    public class HomeController : Controller
    {
        private readonly Conexion conexion = new Conexion();

        public IActionResult Index()
        {
            List<SucursalAdminModelo> sucursales =
                new List<SucursalAdminModelo>();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql = @"SELECT TOP (3)
                                      id_sucursal,
                                      nombre,
                                      ciudad,
                                      [dirección],
                                      horario_apertura,
                                      horario_cierre,
                                      ruta_imagen
                               FROM Sucursal
                               ORDER BY id_sucursal DESC";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SucursalAdminModelo sucursal =
                                new SucursalAdminModelo();

                            sucursal.Id = reader.GetInt32(0);
                            sucursal.Nombre = reader.GetString(1);
                            sucursal.Ciudad = reader.GetString(2);
                            sucursal.Direccion = reader.GetString(3);

                            sucursal.Apertura = reader[4] == DBNull.Value
                                ? TimeSpan.Zero
                                : (TimeSpan)reader[4];

                            sucursal.Cierre = reader[5] == DBNull.Value
                                ? TimeSpan.Zero
                                : (TimeSpan)reader[5];

                            sucursal.RutaImagen = reader[6] == DBNull.Value
                                ? null
                                : reader[6].ToString();

                            sucursales.Add(sucursal);
                        }
                    }
                }
            }

            List<PlanAdminModelo> planes =
                new List<PlanAdminModelo>();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql = @"SELECT id_plan, nombre, precio,
                                      duracion, beneficios
                               FROM Planes
                               ORDER BY precio";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            PlanAdminModelo plan =
                                new PlanAdminModelo();

                            plan.Id = reader.GetInt32(0);
                            plan.Nombre = reader.GetString(1);
                            plan.Precio = reader.GetDecimal(2);

                            plan.Duracion = reader[3] == DBNull.Value
                                ? ""
                                : reader[3].ToString() ?? "";

                            plan.Beneficios = reader[4] == DBNull.Value
                                ? ""
                                : reader[4].ToString() ?? "";

                            planes.Add(plan);
                        }
                    }
                }
            }

            ViewBag.Planes = planes;

            return View(sucursales);
        }

        [HttpGet]
        public IActionResult Sucursales(string? busqueda, string? ciudad)
        {
            List<SucursalAdminModelo> sucursales =
                new List<SucursalAdminModelo>();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql = @"SELECT id_sucursal, nombre, ciudad,
                                      [dirección], telefono,
                                      horario_apertura, horario_cierre,
                                      ruta_imagen
                               FROM Sucursal
                               WHERE (@Busqueda = ''
                                      OR nombre LIKE @Filtro
                                      OR ciudad LIKE @Filtro)
                               AND (@Ciudad = '' OR ciudad = @Ciudad)
                               ORDER BY nombre";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Busqueda",
                        busqueda?.Trim() ?? ""
                    );

                    cmd.Parameters.AddWithValue(
                        "@Filtro",
                        "%" + (busqueda?.Trim() ?? "") + "%"
                    );

                    cmd.Parameters.AddWithValue(
                        "@Ciudad",
                        ciudad?.Trim() ?? ""
                    );

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SucursalAdminModelo sucursal =
                                new SucursalAdminModelo();

                            sucursal.Id = reader.GetInt32(0);
                            sucursal.Nombre = reader.GetString(1);
                            sucursal.Ciudad = reader.GetString(2);
                            sucursal.Direccion = reader.GetString(3);

                            sucursal.Telefono = reader[4] == DBNull.Value
                                ? ""
                                : reader[4].ToString() ?? "";

                            sucursal.Apertura = reader[5] == DBNull.Value
                                ? TimeSpan.Zero
                                : (TimeSpan)reader[5];

                            sucursal.Cierre = reader[6] == DBNull.Value
                                ? TimeSpan.Zero
                                : (TimeSpan)reader[6];

                            sucursal.RutaImagen = reader[7] == DBNull.Value
                                ? null
                                : reader[7].ToString();

                            sucursales.Add(sucursal);
                        }
                    }
                }
            }

            ViewBag.Busqueda = busqueda ?? "";
            ViewBag.Ciudad = ciudad ?? "";

            return View(
                "~/Views/VistasCliente/Sucursales.cshtml",
                sucursales
            );
        }

        [HttpGet]
        public IActionResult Planes(string? tipo = null)
        {
            List<PlanAdminModelo> lista =
                new List<PlanAdminModelo>();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql = @"SELECT id_plan, nombre, precio,
                                      duracion, beneficios
                               FROM Planes
                               ORDER BY precio";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new PlanAdminModelo
                            {
                                Id = reader.GetInt32(0),
                                Nombre = reader.GetString(1),
                                Precio = reader.GetDecimal(2),

                                Duracion = reader[3] == DBNull.Value
                                    ? ""
                                    : reader[3].ToString() ?? "",

                                Beneficios = reader[4] == DBNull.Value
                                    ? ""
                                    : reader[4].ToString() ?? ""
                            });
                        }
                    }
                }
            }

            if (tipo == "renovar" || tipo == "cambiar")
            {
                ViewBag.TipoOperacion = tipo;
            }
            else
            {
                ViewBag.TipoOperacion = "nuevo";
            }

            return View(
                "~/Views/VistasCliente/Planes.cshtml",
                lista
            );
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}
