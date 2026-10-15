using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vistasdebeltran.Models;

namespace Vistasdebeltran.Controllers
{
    public class AdminPagosController : Controller
    {
        private readonly Conexion conexion = new Conexion();
        private bool EsAdmin() => HttpContext.Session.GetInt32("Rol") == 1;

        [HttpGet]
        public IActionResult Index()
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            var modelo = new AdminPagosModelo
            {
                NombreAdmin = HttpContext.Session.GetString("NombreUsuario") ?? "Administrador",
                RolAdmin = "Administrador",
                PagosRecientes = new List<TransaccionItem>(),
                PromocionesActivas = new List<PromocionItem>()
            };
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(@"SELECT TOP 100 p.id_pago, c.nombre, COALESCE(pl.nombre, 'Sin plan'), COALESCE(p.metodo_pago, 'Sin registrar'), p.fecha, p.monto, COALESCE(p.estado_pago, 'Pendiente')
                    FROM Pago p INNER JOIN Cliente c ON c.id_cliente = p.id_cliente
                    LEFT JOIN Membresias m ON m.id_membresia = p.id_membresia
                    LEFT JOIN Planes pl ON pl.id_plan = m.id_plan ORDER BY p.id_pago DESC", con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int idPago = reader.GetInt32(0);
                        modelo.PagosRecientes.Add(new TransaccionItem
                        {
                            IdPago = idPago, Id = "PG-" + idPago, Cliente = reader.GetString(1), Membresia = reader.GetString(2),
                            Metodo = reader.GetString(3), Fecha = reader[4] == DBNull.Value ? "Sin fecha" : Convert.ToDateTime(reader[4]).ToString("dd/MM/yyyy"),
                            Monto = reader[5] == DBNull.Value ? 0 : Convert.ToDecimal(reader[5]), Estado = reader.GetString(6)
                        });
                    }
                }
                using (SqlCommand cmd = new SqlCommand(@"SELECT pr.id_promocion, pr.nombre, COALESCE(pl.nombre,'Sin plan'), pr.descuento, pr.fecha_inicio, pr.fecha_fin
                    FROM Promocion pr LEFT JOIN Planes pl ON pl.id_plan = pr.id_plan ORDER BY pr.fecha_inicio DESC", con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var inicio = reader[4] == DBNull.Value ? DateTime.MinValue : reader.GetDateTime(4);
                        var fin = reader[5] == DBNull.Value ? DateTime.MaxValue : reader.GetDateTime(5);
                        modelo.PromocionesActivas.Add(new PromocionItem
                        {
                            Id = reader.GetInt32(0), Nombre = reader[1]?.ToString() ?? "", PlanAplicable = "Plan: " + reader[2]?.ToString(),
                            Porcentaje = "-" + (reader[3] == DBNull.Value ? 0m : Convert.ToDecimal(reader[3])).ToString("0.##") + "%",
                            RangoFechas = inicio == DateTime.MinValue || fin == DateTime.MaxValue ? "Sin fechas definidas" : inicio.ToString("dd/MM/yyyy") + " - " + fin.ToString("dd/MM/yyyy"),
                            Estado = inicio <= DateTime.Today && fin >= DateTime.Today ? "Activa" : "Finalizada"
                        });
                    }
                }
            }
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Confirmar(int id)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlTransaction tr = con.BeginTransaction())
                {
                    int afectados;
                    using (SqlCommand cmd = new SqlCommand("UPDATE Pago SET estado_pago='Confirmado' WHERE id_pago=@Id AND estado_pago='Pendiente'", con, tr))
                    { cmd.Parameters.AddWithValue("@Id", id); afectados = cmd.ExecuteNonQuery(); }
                    if (afectados > 0)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"UPDATE m SET estado='Activa', fecha_inicio=CAST(GETDATE() AS DATE), fecha_fin=DATEADD(MONTH, 1, CAST(GETDATE() AS DATE))
                            FROM Membresias m INNER JOIN Pago p ON p.id_membresia=m.id_membresia WHERE p.id_pago=@Id", con, tr))
                        { cmd.Parameters.AddWithValue("@Id", id); cmd.ExecuteNonQuery(); }
                    }
                    tr.Commit();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Promocion(int? id)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            var modelo = new PromocionAdminModelo();
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                if (id.HasValue)
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT id_promocion, nombre, descuento, fecha_inicio, fecha_fin, id_plan FROM Promocion WHERE id_promocion=@Id", con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return NotFound();
                            modelo.Id = reader.GetInt32(0); modelo.Nombre = reader[1]?.ToString() ?? "";
                            modelo.Descuento = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);
                            modelo.FechaInicio = reader[3] == DBNull.Value ? DateTime.Today : reader.GetDateTime(3);
                            modelo.FechaFin = reader[4] == DBNull.Value ? DateTime.Today.AddMonths(1) : reader.GetDateTime(4);
                            modelo.IdPlan = reader[5] == DBNull.Value ? 0 : Convert.ToInt32(reader[5]);
                        }
                    }
                }
                ViewBag.Planes = LeerPlanes(con);
            }
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Promocion(PromocionAdminModelo modelo)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            if (modelo.FechaFin.Date < modelo.FechaInicio.Date) ModelState.AddModelError(nameof(modelo.FechaFin), "La fecha final debe ser posterior a la inicial");
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                if (!ModelState.IsValid) { ViewBag.Planes = LeerPlanes(con); return View(modelo); }
                string sql = modelo.Id == 0
                    ? "INSERT INTO Promocion (nombre, descuento, fecha_inicio, fecha_fin, id_plan) VALUES (@Nombre, @Descuento, @Inicio, @Fin, @Plan)"
                    : "UPDATE Promocion SET nombre=@Nombre, descuento=@Descuento, fecha_inicio=@Inicio, fecha_fin=@Fin, id_plan=@Plan WHERE id_promocion=@Id";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", modelo.Id); cmd.Parameters.AddWithValue("@Nombre", modelo.Nombre.Trim());
                    cmd.Parameters.AddWithValue("@Descuento", modelo.Descuento);
                    cmd.Parameters.AddWithValue("@Inicio", modelo.FechaInicio.Date);
                    cmd.Parameters.AddWithValue("@Fin", modelo.FechaFin.Date);
                    cmd.Parameters.AddWithValue("@Plan", modelo.IdPlan); cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarPromocion(int id)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("DELETE FROM Promocion WHERE id_promocion=@Id", con))
                { cmd.Parameters.AddWithValue("@Id", id); cmd.ExecuteNonQuery(); }
            }
            return RedirectToAction(nameof(Index));
        }

        private static List<(int Id, string Nombre)> LeerPlanes(SqlConnection con)
        {
            var lista = new List<(int Id, string Nombre)>();
            using (SqlCommand cmd = new SqlCommand("SELECT id_plan, nombre FROM Planes ORDER BY nombre", con))
            using (SqlDataReader reader = cmd.ExecuteReader())
                while (reader.Read()) lista.Add((reader.GetInt32(0), reader.GetString(1)));
            return lista;
        }
    }
}
