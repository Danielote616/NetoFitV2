using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Conexion conexion = new Conexion();
        public HomeController(ILogger<HomeController> logger) { _logger = logger; }
        public IActionResult Index() { return View(); }

        [HttpGet]
        public IActionResult Sucursales(string? busqueda, string? ciudad)
        {
            var sucursales = new List<SucursalAdminModelo>();
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                string sql = @"SELECT id_sucursal, nombre, ciudad, [dirección], telefono, horario_apertura, horario_cierre
                    FROM Sucursal WHERE (@Busqueda = '' OR nombre LIKE @Filtro OR ciudad LIKE @Filtro)
                    AND (@Ciudad = '' OR ciudad=@Ciudad) ORDER BY nombre";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Busqueda", busqueda?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@Filtro", "%" + (busqueda?.Trim() ?? "") + "%");
                    cmd.Parameters.AddWithValue("@Ciudad", ciudad?.Trim() ?? "");
                    using (SqlDataReader reader = cmd.ExecuteReader())
                        while (reader.Read()) sucursales.Add(new SucursalAdminModelo
                        {
                            Id = reader.GetInt32(0), Nombre = reader.GetString(1), Ciudad = reader.GetString(2), Direccion = reader.GetString(3),
                            Telefono = reader[4] == DBNull.Value ? "" : reader[4].ToString() ?? "",
                            Apertura = reader[5] == DBNull.Value ? TimeSpan.Zero : (TimeSpan)reader[5],
                            Cierre = reader[6] == DBNull.Value ? TimeSpan.Zero : (TimeSpan)reader[6]
                        });
                }
            }
            ViewBag.Busqueda = busqueda ?? ""; ViewBag.Ciudad = ciudad ?? "";
            return View("~/Views/VistasCliente/Sucursales.cshtml", sucursales);
        }

        [HttpGet]
        public IActionResult Planes()
        {
            var lista = new List<PlanAdminModelo>();
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT id_plan, nombre, precio, duracion, beneficios FROM Planes ORDER BY precio", con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read()) lista.Add(new PlanAdminModelo
                    {
                        Id = reader.GetInt32(0), Nombre = reader.GetString(1), Precio = reader.GetDecimal(2),
                        Duracion = reader[3] == DBNull.Value ? "" : reader[3].ToString() ?? "",
                        Beneficios = reader[4] == DBNull.Value ? "" : reader[4].ToString() ?? ""
                    });
            }
            return View("~/Views/VistasCliente/Planes.cshtml", lista);
        }
        public IActionResult Privacy() { return View(); }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() { return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }); }
    }
}
