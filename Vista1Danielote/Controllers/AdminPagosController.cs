
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;
using Vistasdebeltran.Models;

namespace Vistasdebeltran.Controllers
{
    public class AdminPagosController : Controller
    {
        private readonly Conexion conexion = new Conexion();

        private bool EsAdmin()
        {
            return HttpContext.Session.GetInt32("Rol") == 1;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            AdminPagosModelo modelo = new AdminPagosModelo();

            modelo.NombreAdmin =
                HttpContext.Session.GetString("NombreUsuario")
                ?? "Administrador";

            modelo.RolAdmin = "Administrador";
            modelo.PagosRecientes = new List<TransaccionItem>();
            modelo.PromocionesActivas = new List<PromocionItem>();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sqlPagos = @"
    SELECT TOP 100
        IdPago,
        Cliente,
        COALESCE([Plan], 'Sin plan'),
        COALESCE(MetodoPago, 'Sin registrar'),
        FechaPago,
        Monto,
        COALESCE(EstadoPago, 'Pendiente')
    FROM dbo.vw_PagosDetalle
    WHERE Cliente IS NOT NULL
    ORDER BY IdPago DESC";

                using (SqlCommand cmd = new SqlCommand(sqlPagos, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TransaccionItem pago = new TransaccionItem();

                            pago.IdPago = reader.GetInt32(0);
                            pago.Id = "PG-" + pago.IdPago;
                            pago.Cliente = reader[1]?.ToString() ?? "";
                            pago.Membresia = reader[2]?.ToString() ?? "";
                            pago.Metodo = reader[3]?.ToString() ?? "";

                            pago.Fecha = reader[4] == DBNull.Value
                                ? ""
                                : Convert.ToDateTime(reader[4])
                                    .ToString("dd/MM/yyyy");

                            pago.Monto = reader[5] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader[5]);

                            pago.Estado = reader[6]?.ToString() ?? "";

                            modelo.PagosRecientes.Add(pago);
                        }
                    }
                }

                string sqlPromociones = @"
    SELECT
        IdPromocion,
        Promocion,
        COALESCE([Plan], 'Sin plan'),
        Descuento,
        FechaInicio,
        FechaFin
    FROM dbo.vw_PromocionesPlanes
    ORDER BY FechaInicio DESC";

                using (SqlCommand cmd = new SqlCommand(sqlPromociones, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            PromocionItem promocion = new PromocionItem();

                            DateTime inicio = reader[4] == DBNull.Value
                                ? DateTime.MinValue
                                : reader.GetDateTime(4);

                            DateTime fin = reader[5] == DBNull.Value
                                ? DateTime.MaxValue
                                : reader.GetDateTime(5);

                            decimal descuento = reader[3] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader[3]);

                            promocion.Id = reader.GetInt32(0);
                            promocion.Nombre = reader[1]?.ToString() ?? "";
                            promocion.PlanAplicable =
                                "Plan: " + reader[2]?.ToString();

                            promocion.Porcentaje =
                                "-" + descuento.ToString("0.##") + "%";

                            if (inicio == DateTime.MinValue ||
                                fin == DateTime.MaxValue)
                            {
                                promocion.RangoFechas =
                                    "Sin fechas definidas";
                            }
                            else
                            {
                                promocion.RangoFechas =
                                    inicio.ToString("dd/MM/yyyy")
                                    + " - " + fin.ToString("dd/MM/yyyy");
                            }

                            if (inicio <= DateTime.Today &&
                                fin >= DateTime.Today)
                            {
                                promocion.Estado = "Activa";
                            }
                            else
                            {
                                promocion.Estado = "Finalizada";
                            }

                            modelo.PromocionesActivas.Add(promocion);
                        }
                    }
                }
            }

            return View(modelo);
        }

        [HttpPost]
        public IActionResult Confirmar(int id)
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                DateTime fechaInicio = DateTime.Today;
                string duracion = "";
                bool pagoPendiente = false;

                string sqlConsultar = @"
                    SELECT m.fecha_inicio, pl.duracion
                    FROM Pago p
                    INNER JOIN Membresias m
                        ON m.id_membresia = p.id_membresia
                    INNER JOIN Planes pl
                        ON pl.id_plan = m.id_plan
                    WHERE p.id_pago = @Pago
                    AND p.estado_pago = 'Pendiente'
                    AND m.estado = 'Pendiente'";

                using (SqlCommand cmd = new SqlCommand(sqlConsultar, con))
                {
                    cmd.Parameters.AddWithValue("@Pago", id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            pagoPendiente = true;
                            fechaInicio = reader.GetDateTime(0);
                            duracion = reader[1]?.ToString() ?? "";
                        }
                    }
                }

                if (!pagoPendiente)
                {
                    TempData["Error"] =
                        "El pago no existe o ya fue confirmado.";

                    return RedirectToAction("Index");
                }

                if (fechaInicio.Date < DateTime.Today)
                {
                    fechaInicio = DateTime.Today;
                }

                DateTime? fechaFin = DuracionPlan.CalcularFin(
                    fechaInicio,
                    duracion
                );

                if (fechaFin == null)
                {
                    TempData["Error"] =
                        "No se puede confirmar: duración del plan no válida.";

                    return RedirectToAction("Index");
                }

                string sqlMembresia = @"
                    UPDATE m
                    SET m.estado = 'Activa',
                        m.fecha_inicio = @Inicio,
                        m.fecha_fin = @Fin
                    FROM Membresias m
                    INNER JOIN Pago p
                        ON p.id_membresia = m.id_membresia
                    WHERE p.id_pago = @Pago
                    AND p.estado_pago = 'Pendiente'
                    AND m.estado = 'Pendiente'";

                try
                {
                    using (SqlCommand cmd =
                        new SqlCommand(sqlMembresia, con))
                    {
                        cmd.Parameters.AddWithValue("@Pago", id);
                        cmd.Parameters.AddWithValue(
                            "@Inicio", fechaInicio);
                        cmd.Parameters.AddWithValue(
                            "@Fin", fechaFin.Value);

                        int afectados = cmd.ExecuteNonQuery();

                        if (afectados == 0)
                        {
                            TempData["Error"] =
                                "No se pudo activar la membresía.";

                            return RedirectToAction("Index");
                        }
                    }

                    string sqlPago = @"
                        UPDATE Pago
                        SET estado_pago = 'Confirmado'
                        WHERE id_pago = @Pago
                        AND estado_pago = 'Pendiente'";

                    using (SqlCommand cmd =
                        new SqlCommand(sqlPago, con))
                    {
                        cmd.Parameters.AddWithValue("@Pago", id);

                        cmd.ExecuteNonQuery();
                    }
                }
                catch (SqlException)
                {
                    TempData["Error"] =
                        "Ocurrió un error al confirmar el pago.";
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Promocion(int? id)
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            PromocionAdminModelo modelo =
                new PromocionAdminModelo();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                if (id.HasValue)
                {
                    string sql = @"
                        SELECT id_promocion,
                               nombre,
                               descuento,
                               fecha_inicio,
                               fecha_fin,
                               id_plan
                        FROM Promocion
                        WHERE id_promocion = @Id";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id.Value);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return NotFound();
                            }

                            modelo.Id = reader.GetInt32(0);
                            modelo.Nombre = reader[1]?.ToString() ?? "";

                            modelo.Descuento = reader[2] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader[2]);

                            modelo.FechaInicio = reader[3] == DBNull.Value
                                ? DateTime.Today
                                : reader.GetDateTime(3);

                            modelo.FechaFin = reader[4] == DBNull.Value
                                ? DateTime.Today.AddMonths(1)
                                : reader.GetDateTime(4);

                            modelo.IdPlan = reader[5] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(reader[5]);
                        }
                    }
                }

                ViewBag.Planes = LeerPlanes(con);
            }

            return View(modelo);
        }

        [HttpPost]
        public IActionResult Promocion(PromocionAdminModelo modelo)
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            if (modelo.FechaFin.Date < modelo.FechaInicio.Date)
            {
                ModelState.AddModelError(
                    "FechaFin",
                    "La fecha final debe ser posterior a la inicial."
                );
            }

            if (modelo.FechaFin.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    "FechaFin",
                    "La fecha de finalización no puede ser anterior a la fecha actual."
                );
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                if (!ModelState.IsValid)
                {
                    ViewBag.Planes = LeerPlanes(con);
                    return View(modelo);
                }

                string sql;

                if (modelo.Id == 0)
                {
                    sql = @"
                        INSERT INTO Promocion
                        (nombre, descuento, fecha_inicio, fecha_fin, id_plan)
                        VALUES
                        (@Nombre, @Descuento, @Inicio, @Fin, @Plan)";
                }
                else
                {
                    sql = @"
                        UPDATE Promocion
                        SET nombre = @Nombre,
                            descuento = @Descuento,
                            fecha_inicio = @Inicio,
                            fecha_fin = @Fin,
                            id_plan = @Plan
                        WHERE id_promocion = @Id";
                }

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", modelo.Id);
                    cmd.Parameters.AddWithValue(
                        "@Nombre", modelo.Nombre.Trim());
                    cmd.Parameters.AddWithValue(
                        "@Descuento", modelo.Descuento);
                    cmd.Parameters.AddWithValue(
                        "@Inicio", modelo.FechaInicio.Date);
                    cmd.Parameters.AddWithValue(
                        "@Fin", modelo.FechaFin.Date);
                    cmd.Parameters.AddWithValue(
                        "@Plan", modelo.IdPlan);

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult EliminarPromocion(int id)
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql = @"
                    DELETE FROM Promocion
                    WHERE id_promocion = @Id";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }

        private List<OpcionModelo> LeerPlanes(SqlConnection con)
        {
            List<OpcionModelo> lista = new List<OpcionModelo>();

            string sql = @"
                SELECT id_plan, nombre
                FROM Planes
                ORDER BY nombre";

            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        OpcionModelo plan = new OpcionModelo();

                        plan.Id = reader.GetInt32(0);
                        plan.Nombre = reader.GetString(1);

                        lista.Add(plan);
                    }
                }
            }

            return lista;
        }
    }
}
