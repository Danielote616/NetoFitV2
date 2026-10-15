using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;
using Vistasdebeltran.Models;

namespace Vistasdebeltran.Controllers
{
    public class PagoController : Controller
    {
        private readonly Conexion conexion = new Conexion();

        [HttpGet]
        public IActionResult Index(int id)
        {
            int? idCliente = HttpContext.Session.GetInt32("IdCliente");
            if (HttpContext.Session.GetInt32("Rol") != 2 || idCliente == null) return RedirectToAction("Login", "Account");
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                var modelo = CargarPlan(con, id);
                if (modelo == null) return RedirectToAction("Planes", "Home");
                using (SqlCommand cmd = new SqlCommand("SELECT nombre, email FROM Cliente WHERE id_cliente=@Id", con))
                {
                    cmd.Parameters.AddWithValue("@Id", idCliente.Value);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return RedirectToAction("Login", "Account");
                        modelo.Nombre = reader.GetString(0); modelo.Correo = reader.GetString(1);
                    }
                }
                ViewBag.Sucursales = CargarSucursales(con);
                return View(modelo);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcesarPago(PagosModelo modelo)
        {
            int? idCliente = HttpContext.Session.GetInt32("IdCliente");
            if (HttpContext.Session.GetInt32("Rol") != 2 || idCliente == null) return RedirectToAction("Login", "Account");
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                PagosModelo? plan = CargarPlan(con, modelo.IdPlan);
                if (plan == null) return RedirectToAction("Planes", "Home");
                List<SucursalAdminModelo> sucursales = CargarSucursales(con);
                if (!sucursales.Any(x => x.Id == modelo.IdSucursal)) ModelState.AddModelError(nameof(modelo.IdSucursal), "Sucursal no válida");
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Membresias WHERE id_cliente=@Cliente AND estado IN ('Pendiente', 'Activa') AND fecha_fin >= CAST(GETDATE() AS DATE)", con))
                {
                    cmd.Parameters.AddWithValue("@Cliente", idCliente.Value);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) ModelState.AddModelError("", "Ya tienes una membresía activa o una solicitud pendiente.");
                }
                using (SqlCommand cmd = new SqlCommand("SELECT nombre, email FROM Cliente WHERE id_cliente=@Id", con))
                {
                    cmd.Parameters.AddWithValue("@Id", idCliente.Value);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) { plan.Nombre = reader.GetString(0); plan.Correo = reader.GetString(1); }
                    }
                }
                plan.IdSucursal = modelo.IdSucursal;
                if (!ModelState.IsValid) { ViewBag.Sucursales = sucursales; return View("Index", plan); }
                using (SqlTransaction tr = con.BeginTransaction())
                {
                    try
                    {
                        int idMembresia;
                        using (SqlCommand cmd = new SqlCommand(@"INSERT INTO Membresias (fecha_inicio, fecha_fin, estado, id_cliente, id_plan, id_sucursal)
                            OUTPUT INSERTED.id_membresia VALUES (@Inicio, @Fin, 'Pendiente', @Cliente, @Plan, @Sucursal)", con, tr))
                        {
                            cmd.Parameters.AddWithValue("@Inicio", DateTime.Today);
                            cmd.Parameters.AddWithValue("@Fin", DateTime.Today.AddMonths(1));
                            cmd.Parameters.AddWithValue("@Cliente", idCliente.Value);
                            cmd.Parameters.AddWithValue("@Plan", modelo.IdPlan);
                            cmd.Parameters.AddWithValue("@Sucursal", modelo.IdSucursal);
                            idMembresia = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        int idPago;
                        using (SqlCommand cmd = new SqlCommand(@"INSERT INTO Pago (monto, fecha, estado_pago, metodo_pago, id_cliente, id_membresia)
                            OUTPUT INSERTED.id_pago VALUES (@Monto, @Fecha, 'Pendiente', 'Presencial', @Cliente, @Membresia)", con, tr))
                        {
                            cmd.Parameters.AddWithValue("@Monto", plan.Total);
                            cmd.Parameters.AddWithValue("@Fecha", DateTime.Today);
                            cmd.Parameters.AddWithValue("@Cliente", idCliente.Value);
                            cmd.Parameters.AddWithValue("@Membresia", idMembresia);
                            idPago = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        tr.Commit();
                        HttpContext.Session.SetInt32("UltimaSolicitudPago", idPago);
                        return RedirectToAction(nameof(Confirmacion));
                    }
                    catch
                    {
                        tr.Rollback();
                        ModelState.AddModelError("", "No fue posible registrar la solicitud. Intenta nuevamente.");
                        ViewBag.Sucursales = sucursales;
                        return View("Index", plan);
                    }
                }
            }
        }

        [HttpGet]
        public IActionResult Confirmacion()
        {
            int? idCliente = HttpContext.Session.GetInt32("IdCliente");
            int? idPago = HttpContext.Session.GetInt32("UltimaSolicitudPago");
            if (HttpContext.Session.GetInt32("Rol") != 2 || !idCliente.HasValue || !idPago.HasValue) return RedirectToAction("Login", "Account");
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(@"SELECT pl.nombre, p.monto, m.fecha_fin, p.estado_pago
                    FROM Pago p INNER JOIN Membresias m ON m.id_membresia = p.id_membresia
                    INNER JOIN Planes pl ON pl.id_plan = m.id_plan
                    WHERE p.id_pago=@Pago AND p.id_cliente=@Cliente", con))
                {
                    cmd.Parameters.AddWithValue("@Pago", idPago.Value);
                    cmd.Parameters.AddWithValue("@Cliente", idCliente.Value);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return RedirectToAction("Index", "Perfil");
                        return View(new PagosModelo
                        {
                            NombrePlan = reader.GetString(0), Total = reader.GetDecimal(1),
                            FechaFin = reader.GetDateTime(2), Estado = reader[3]?.ToString() ?? "Pendiente"
                        });
                    }
                }
            }
        }

        private static PagosModelo? CargarPlan(SqlConnection con, int id)
        {
            using (SqlCommand cmd = new SqlCommand("SELECT id_plan, nombre, precio, duracion FROM Planes WHERE id_plan=@Id", con))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return new PagosModelo
                    {
                        IdPlan = reader.GetInt32(0), NombrePlan = reader.GetString(1),
                        Total = reader.GetDecimal(2), Subtotal = reader.GetDecimal(2),
                        DetallePlan = reader[3]?.ToString() ?? "", NombrePromo = "Sin promoción", DescuentoPromo = 0
                    };
                }
            }
        }
        private static List<SucursalAdminModelo> CargarSucursales(SqlConnection con)
        {
            var lista = new List<SucursalAdminModelo>();
            using (SqlCommand cmd = new SqlCommand("SELECT id_sucursal, nombre FROM Sucursal ORDER BY nombre", con))
            using (SqlDataReader reader = cmd.ExecuteReader())
                while (reader.Read()) lista.Add(new SucursalAdminModelo { Id = reader.GetInt32(0), Nombre = reader.GetString(1) });
            return lista;
        }
    }
}
