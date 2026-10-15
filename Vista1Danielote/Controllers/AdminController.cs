using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers
{
    public class AdminController : Controller
    {
        private readonly Conexion conexion = new Conexion();
        private bool EsAdmin() => HttpContext.Session.GetInt32("Rol") == 1;

        [HttpGet]
        public IActionResult Dashboard()
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Cliente", con))
                    ViewBag.Clientes = Convert.ToInt32(cmd.ExecuteScalar());
                using (SqlCommand cmd = new SqlCommand("SELECT COALESCE(SUM(monto), 0) FROM Pago WHERE estado_pago = 'Confirmado'", con))
                    ViewBag.Ingresos = Convert.ToDecimal(cmd.ExecuteScalar());
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Asistencia", con))
                    ViewBag.Asistencias = Convert.ToInt32(cmd.ExecuteScalar());
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Membresias WHERE fecha_fin BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 15, CAST(GETDATE() AS DATE))", con))
                    ViewBag.PorVencer = Convert.ToInt32(cmd.ExecuteScalar());
                var sucursales = new List<(string Nombre, decimal Ingresos, int Asistencias)>();
                using (SqlCommand cmd = new SqlCommand(@"SELECT s.nombre,
                    COALESCE((SELECT SUM(p.monto) FROM Pago p INNER JOIN Membresias m ON m.id_membresia = p.id_membresia WHERE m.id_sucursal = s.id_sucursal AND p.estado_pago = 'Confirmado'), 0) AS ingresos,
                    (SELECT COUNT(*) FROM Asistencia a INNER JOIN Reserva r ON a.id_reserva = r.id_reserva INNER JOIN Horario h ON r.id_horario = h.id_horario INNER JOIN Clase c ON h.id_clase = c.id_clase WHERE c.id_sucursal = s.id_sucursal) AS asistencias
                    FROM Sucursal s ORDER BY s.nombre", con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read()) sucursales.Add((reader.GetString(0), Convert.ToDecimal(reader[1]), Convert.ToInt32(reader[2])));
                ViewBag.Sucursales = sucursales;
                var proximas = new List<(string Cliente, string Plan, string Sucursal, DateTime Fecha)>();
                using (SqlCommand cmd = new SqlCommand(@"SELECT c.nombre, COALESCE(p.nombre, 'Sin plan'), COALESCE(s.nombre, 'Sin sucursal'), m.fecha_fin
                    FROM Membresias m INNER JOIN Cliente c ON c.id_cliente = m.id_cliente
                    LEFT JOIN Planes p ON p.id_plan = m.id_plan LEFT JOIN Sucursal s ON s.id_sucursal = m.id_sucursal
                    WHERE m.fecha_fin BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 15, CAST(GETDATE() AS DATE)) ORDER BY m.fecha_fin", con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read()) proximas.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetDateTime(3)));
                ViewBag.Proximas = proximas;
            }
            return View();
        }

        [HttpGet]
        public IActionResult Sucursales()
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            var lista = new List<SucursalAdminModelo>();
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(@"SELECT s.id_sucursal, s.nombre, s.ciudad, s.[dirección], s.telefono, s.horario_apertura, s.horario_cierre,
                    (SELECT COUNT(*) FROM Membresias m WHERE m.id_sucursal = s.id_sucursal AND m.fecha_fin >= CAST(GETDATE() AS DATE)) AS miembros
                    FROM Sucursal s ORDER BY s.id_sucursal", con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read()) lista.Add(new SucursalAdminModelo
                    {
                        Id = reader.GetInt32(0), Nombre = reader.GetString(1), Ciudad = reader.GetString(2),
                        Direccion = reader.GetString(3), Telefono = reader[4] == DBNull.Value ? "" : reader[4].ToString() ?? "",
                        Apertura = reader[5] == DBNull.Value ? TimeSpan.Zero : (TimeSpan)reader[5],
                        Cierre = reader[6] == DBNull.Value ? TimeSpan.Zero : (TimeSpan)reader[6], Miembros = reader.GetInt32(7)
                    });
            }
            return View("~/Views/VistasAdmin/AdminSucursales.cshtml", lista);
        }

        [HttpGet]
        public IActionResult FormularioSucursal(int? id)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            var modelo = new SucursalAdminModelo();
            if (id.HasValue)
            {
                using (SqlConnection con = conexion.GetConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT id_sucursal, nombre, ciudad, [dirección], telefono, horario_apertura, horario_cierre FROM Sucursal WHERE id_sucursal = @Id", con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return NotFound();
                            modelo.Id = reader.GetInt32(0); modelo.Nombre = reader.GetString(1);
                            modelo.Ciudad = reader.GetString(2); modelo.Direccion = reader.GetString(3);
                            modelo.Telefono = reader[4] == DBNull.Value ? "" : reader[4].ToString() ?? "";
                            modelo.Apertura = reader[5] == DBNull.Value ? TimeSpan.Zero : (TimeSpan)reader[5];
                            modelo.Cierre = reader[6] == DBNull.Value ? TimeSpan.Zero : (TimeSpan)reader[6];
                        }
                    }
                }
            }
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FormularioSucursal(SucursalAdminModelo modelo)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            if (modelo.Cierre <= modelo.Apertura) ModelState.AddModelError(nameof(modelo.Cierre), "El cierre debe ser posterior a la apertura");
            if (!ModelState.IsValid) return View(modelo);
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                string sql = modelo.Id == 0
                    ? "INSERT INTO Sucursal (nombre, ciudad, [dirección], telefono, horario_apertura, horario_cierre) VALUES (@Nombre, @Ciudad, @Direccion, @Telefono, @Apertura, @Cierre)"
                    : "UPDATE Sucursal SET nombre=@Nombre, ciudad=@Ciudad, [dirección]=@Direccion, telefono=@Telefono, horario_apertura=@Apertura, horario_cierre=@Cierre WHERE id_sucursal=@Id";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", modelo.Id); cmd.Parameters.AddWithValue("@Nombre", modelo.Nombre.Trim());
                    cmd.Parameters.AddWithValue("@Ciudad", modelo.Ciudad.Trim()); cmd.Parameters.AddWithValue("@Direccion", modelo.Direccion.Trim());
                    cmd.Parameters.AddWithValue("@Telefono", modelo.Telefono.Trim()); cmd.Parameters.AddWithValue("@Apertura", modelo.Apertura);
                    cmd.Parameters.AddWithValue("@Cierre", modelo.Cierre); cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction(nameof(Sucursales));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarSucursal(int id)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            try
            {
                using (SqlConnection con = conexion.GetConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Sucursal WHERE id_sucursal=@Id", con))
                    { cmd.Parameters.AddWithValue("@Id", id); cmd.ExecuteNonQuery(); }
                }
            }
            catch (SqlException) { TempData["Error"] = "No se puede eliminar esta sucursal porque tiene registros asociados."; }
            return RedirectToAction(nameof(Sucursales));
        }

        [HttpGet]
        public IActionResult Equipos(int? sucursal)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            var lista = new List<EquipoAdminModelo>();
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(@"SELECT e.id_equipo, e.nombre, e.tipo, e.estado, e.id_sucursal, s.nombre FROM Equipo e LEFT JOIN Sucursal s ON s.id_sucursal=e.id_sucursal WHERE @Sucursal IS NULL OR e.id_sucursal=@Sucursal ORDER BY e.id_equipo DESC", con))
                {
                    cmd.Parameters.Add("@Sucursal", System.Data.SqlDbType.Int).Value = sucursal.HasValue ? (object)sucursal.Value : DBNull.Value;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                        while (reader.Read()) lista.Add(new EquipoAdminModelo
                        {
                            Id = reader.GetInt32(0), Nombre = reader[1]?.ToString() ?? "", Tipo = reader[2]?.ToString() ?? "",
                            Estado = reader[3]?.ToString() ?? "", IdSucursal = reader[4] == DBNull.Value ? 0 : Convert.ToInt32(reader[4]),
                            Sucursal = reader[5] == DBNull.Value ? "Sin asignar" : reader[5].ToString() ?? ""
                        });
                }
            }
            ViewBag.Sucursales = LeerSucursales(); ViewBag.FiltroSucursal = sucursal;
            return View("~/Views/VistasAdmin/AdminEquipos.cshtml", lista);
        }

        [HttpGet]
        public IActionResult FormularioEquipo(int? id)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            var modelo = new EquipoAdminModelo();
            if (id.HasValue)
            {
                using (SqlConnection con = conexion.GetConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT id_equipo, nombre, tipo, estado, id_sucursal FROM Equipo WHERE id_equipo=@Id", con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return NotFound();
                            modelo.Id = reader.GetInt32(0); modelo.Nombre = reader[1]?.ToString() ?? "";
                            modelo.Tipo = reader[2]?.ToString() ?? ""; modelo.Estado = reader[3]?.ToString() ?? "Operativo";
                            modelo.IdSucursal = reader[4] == DBNull.Value ? 0 : Convert.ToInt32(reader[4]);
                        }
                    }
                }
            }
            ViewBag.Sucursales = LeerSucursales(); return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FormularioEquipo(EquipoAdminModelo modelo)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            if (modelo.Estado != "Operativo" && modelo.Estado != "Mantenimiento" && modelo.Estado != "Dañado")
                ModelState.AddModelError(nameof(modelo.Estado), "Estado no válido");
            if (!ModelState.IsValid) { ViewBag.Sucursales = LeerSucursales(); return View(modelo); }
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                string sql = modelo.Id == 0
                    ? "INSERT INTO Equipo (nombre, tipo, estado, id_sucursal) VALUES (@Nombre, @Tipo, @Estado, @Sucursal)"
                    : "UPDATE Equipo SET nombre=@Nombre, tipo=@Tipo, estado=@Estado, id_sucursal=@Sucursal WHERE id_equipo=@Id";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", modelo.Id); cmd.Parameters.AddWithValue("@Nombre", modelo.Nombre.Trim());
                    cmd.Parameters.AddWithValue("@Tipo", modelo.Tipo.Trim()); cmd.Parameters.AddWithValue("@Estado", modelo.Estado);
                    cmd.Parameters.AddWithValue("@Sucursal", modelo.IdSucursal); cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction(nameof(Equipos));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarEquipo(int id)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("DELETE FROM Equipo WHERE id_equipo=@Id", con))
                { cmd.Parameters.AddWithValue("@Id", id); cmd.ExecuteNonQuery(); }
            }
            return RedirectToAction(nameof(Equipos));
        }

        private List<SucursalAdminModelo> LeerSucursales()
        {
            var sucursales = new List<SucursalAdminModelo>();
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT id_sucursal, nombre FROM Sucursal ORDER BY nombre", con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read()) sucursales.Add(new SucursalAdminModelo { Id = reader.GetInt32(0), Nombre = reader.GetString(1) });
            }
            return sucursales;
        }
    }
}
