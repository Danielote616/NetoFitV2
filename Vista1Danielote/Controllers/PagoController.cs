
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
        public IActionResult Index(int id, string? tipo = null)
        {
            int? idCliente = HttpContext.Session.GetInt32("IdCliente");

            if (HttpContext.Session.GetInt32("Rol") != 2 ||
                idCliente == null)
            {
                return RedirectToAction("Login", "Account");
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                PagosModelo? modelo = PreciosPlanDAL.Cargar(con, id);

                if (modelo == null)
                {
                    return RedirectToAction("Planes", "Home");
                }

                modelo.TipoOperacion = "nuevo";

                if (tipo == "renovar" || tipo == "cambiar")
                {
                    modelo.TipoOperacion = tipo;
                }

                string sqlCliente = @"SELECT nombre, email
                                      FROM Cliente
                                      WHERE id_cliente = @Id";

                using (SqlCommand cmd = new SqlCommand(sqlCliente, con))
                {
                    cmd.Parameters.AddWithValue("@Id", idCliente.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return RedirectToAction("Login", "Account");
                        }

                        modelo.Nombre = reader.GetString(0);
                        modelo.Correo = reader.GetString(1);
                    }
                }

                ViewBag.Sucursales = CargarSucursales(con);

                return View(modelo);
            }
        }

        [HttpPost]
        
        public IActionResult ProcesarPago(PagosModelo modelo)
        {
            int? idCliente = HttpContext.Session.GetInt32("IdCliente");

            if (HttpContext.Session.GetInt32("Rol") != 2 ||
                idCliente == null)
            {
                return RedirectToAction("Login", "Account");
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                PagosModelo? plan = PreciosPlanDAL.Cargar(
                    con,
                    modelo.IdPlan
                );

                if (plan == null)
                {
                    return RedirectToAction("Planes", "Home");
                }

                plan.IdSucursal = modelo.IdSucursal;
                plan.TipoOperacion = modelo.TipoOperacion;

                if (plan.TipoOperacion != "renovar" &&
                    plan.TipoOperacion != "cambiar")
                {
                    plan.TipoOperacion = "nuevo";
                }

                List<SucursalAdminModelo> sucursales =
                    CargarSucursales(con);

                bool sucursalValida = false;

                foreach (SucursalAdminModelo sucursal in sucursales)
                {
                    if (sucursal.Id == modelo.IdSucursal)
                    {
                        sucursalValida = true;
                    }
                }

                if (!sucursalValida)
                {
                    ModelState.AddModelError(
                        "IdSucursal",
                        "Selecciona una sucursal válida."
                    );
                }

                string sqlCliente = @"SELECT nombre, email
                                      FROM Cliente
                                      WHERE id_cliente = @Id";

                using (SqlCommand cmd = new SqlCommand(sqlCliente, con))
                {
                    cmd.Parameters.AddWithValue("@Id", idCliente.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return RedirectToAction("Login", "Account");
                        }

                        plan.Nombre = reader.GetString(0);
                        plan.Correo = reader.GetString(1);
                    }
                }

                string sqlPendientes = @"SELECT COUNT(*)
                                         FROM Membresias
                                         WHERE id_cliente = @Cliente
                                         AND estado = 'Pendiente'";

                using (SqlCommand cmd = new SqlCommand(sqlPendientes, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Cliente",
                        idCliente.Value
                    );

                    int pendientes = Convert.ToInt32(
                        cmd.ExecuteScalar()
                    );

                    if (pendientes > 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Ya tienes una solicitud de membresía pendiente."
                        );
                    }
                }

                bool tieneMembresiaActiva = false;
                int idPlanActual = 0;
                DateTime fechaFinActual = DateTime.Today;

                string sqlActiva = @"SELECT TOP 1 id_plan, fecha_fin
                                     FROM Membresias
                                     WHERE id_cliente = @Cliente
                                     AND estado = 'Activa'
                                     AND fecha_inicio <= @Hoy
                                     AND fecha_fin >= @Hoy
                                     ORDER BY fecha_fin DESC";

                using (SqlCommand cmd = new SqlCommand(sqlActiva, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Cliente",
                        idCliente.Value
                    );

                    cmd.Parameters.AddWithValue(
                        "@Hoy",
                        DateTime.Today
                    );

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            tieneMembresiaActiva = true;
                            idPlanActual = reader.GetInt32(0);
                            fechaFinActual = reader.GetDateTime(1);
                        }
                    }
                }

                if (tieneMembresiaActiva)
                {
                    if (plan.TipoOperacion == "nuevo")
                    {
                        ModelState.AddModelError(
                            "",
                            "Ya tienes una membresía activa. Puedes renovarla o cambiar de plan."
                        );
                    }
                    else if (plan.TipoOperacion == "renovar" &&
                             idPlanActual != plan.IdPlan)
                    {
                        ModelState.AddModelError(
                            "",
                            "Para renovar debes seleccionar tu plan actual."
                        );
                    }
                    else if (plan.TipoOperacion == "cambiar" &&
                             idPlanActual == plan.IdPlan)
                    {
                        ModelState.AddModelError(
                            "",
                            "Para conservar el mismo plan utiliza Renovar."
                        );
                    }
                }

                DateTime fechaInicio = DateTime.Today;

                if (tieneMembresiaActiva)
                {
                    fechaInicio = fechaFinActual.Date.AddDays(1);
                }

                DateTime? fechaFin = DuracionPlan.CalcularFin(
                    fechaInicio,
                    plan.DetallePlan
                );

                if (fechaFin == null)
                {
                    ModelState.AddModelError(
                        "",
                        "La duración de este plan no es válida."
                    );
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Sucursales = sucursales;
                    return View("Index", plan);
                }

                try
                {
                    string sqlMembresia = @"
                        INSERT INTO Membresias
                        (fecha_inicio, fecha_fin, estado,
                         id_cliente, id_plan, id_sucursal)
                        OUTPUT INSERTED.id_membresia
                        VALUES
                        (@Inicio, @Fin, 'Pendiente',
                         @Cliente, @Plan, @Sucursal)";

                    int idMembresia;

                    using (SqlCommand cmd =
                        new SqlCommand(sqlMembresia, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@Inicio", fechaInicio);

                        cmd.Parameters.AddWithValue(
                            "@Fin", fechaFin.Value);

                        cmd.Parameters.AddWithValue(
                            "@Cliente", idCliente.Value);

                        cmd.Parameters.AddWithValue(
                            "@Plan", plan.IdPlan);

                        cmd.Parameters.AddWithValue(
                            "@Sucursal", plan.IdSucursal);

                        idMembresia = Convert.ToInt32(
                            cmd.ExecuteScalar()
                        );
                    }

                    string sqlPago = @"
                        INSERT INTO Pago
                        (monto, fecha, estado_pago, metodo_pago,
                         id_cliente, id_membresia)
                        OUTPUT INSERTED.id_pago
                        VALUES
                        (@Monto, @Fecha, 'Pendiente', 'Presencial',
                         @Cliente, @Membresia)";

                    int idPago;

                    using (SqlCommand cmd =
                        new SqlCommand(sqlPago, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@Monto", plan.Total);

                        cmd.Parameters.AddWithValue(
                            "@Fecha", DateTime.Today);

                        cmd.Parameters.AddWithValue(
                            "@Cliente", idCliente.Value);

                        cmd.Parameters.AddWithValue(
                            "@Membresia", idMembresia);

                        idPago = Convert.ToInt32(
                            cmd.ExecuteScalar()
                        );
                    }

                    HttpContext.Session.SetInt32(
                        "UltimaSolicitudPago",
                        idPago
                    );

                    return RedirectToAction("Confirmacion");
                }
                catch (SqlException)
                {
                    ModelState.AddModelError(
                        "",
                        "No fue posible registrar la solicitud de pago."
                    );

                    ViewBag.Sucursales = sucursales;

                    return View("Index", plan);
                }
            }
        }

        [HttpGet]
        public IActionResult Confirmacion()
        {
            int? idCliente = HttpContext.Session.GetInt32("IdCliente");

            int? idPago =
                HttpContext.Session.GetInt32("UltimaSolicitudPago");

            if (HttpContext.Session.GetInt32("Rol") != 2 ||
                !idCliente.HasValue ||
                !idPago.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql = @"
                    SELECT pl.nombre,
                           p.monto,
                           m.fecha_inicio,
                           m.fecha_fin,
                           p.estado_pago
                    FROM Pago p
                    INNER JOIN Membresias m
                        ON m.id_membresia = p.id_membresia
                    INNER JOIN Planes pl
                        ON pl.id_plan = m.id_plan
                    WHERE p.id_pago = @Pago
                    AND p.id_cliente = @Cliente";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Pago", idPago.Value);

                    cmd.Parameters.AddWithValue(
                        "@Cliente", idCliente.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return RedirectToAction(
                                "Index",
                                "Perfil"
                            );
                        }

                        PagosModelo modelo = new PagosModelo();

                        modelo.NombrePlan = reader.GetString(0);
                        modelo.Total = reader.GetDecimal(1);
                        modelo.FechaInicio = reader.GetDateTime(2);
                        modelo.FechaFin = reader.GetDateTime(3);
                        modelo.Estado = reader[4]?.ToString() ?? "Pendiente";

                        return View(modelo);
                    }
                }
            }
        }

        private List<SucursalAdminModelo> CargarSucursales(
            SqlConnection con)
        {
            List<SucursalAdminModelo> lista =
                new List<SucursalAdminModelo>();

            string sql = @"SELECT id_sucursal, nombre
                           FROM Sucursal
                           ORDER BY nombre";

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

                        lista.Add(sucursal);
                    }
                }
            }

            return lista;
        }
    }
}
