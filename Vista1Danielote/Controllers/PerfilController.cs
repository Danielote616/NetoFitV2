
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers
{
    public class PerfilController : Controller
    {
        private readonly Conexion conexion = new Conexion();

        [HttpGet]
        public IActionResult Index(string seccion = "membresia")
        {
            int? idCliente = HttpContext.Session.GetInt32("IdCliente");

            if (HttpContext.Session.GetInt32("Rol") != 2 || !idCliente.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (seccion == "reservas" ||
                seccion == "rutina" ||
                seccion == "evaluaciones")
            {
                ViewBag.Seccion = seccion;
            }
            else
            {
                ViewBag.Seccion = "membresia";
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sqlCliente = @"SELECT nombre, email, ruta_imagen
                                      FROM Cliente
                                      WHERE id_cliente = @IdCliente";

                using (SqlCommand cmd = new SqlCommand(sqlCliente, con))
                {
                    cmd.Parameters.AddWithValue("@IdCliente", idCliente.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return RedirectToAction("Login", "Account");
                        }

                        ViewBag.Usuario = reader["nombre"].ToString();
                        ViewBag.Correo = reader["email"].ToString();

                        string nombre = reader["nombre"].ToString() ?? "";

                        ViewBag.RutaImagen = reader["ruta_imagen"] == DBNull.Value? "": reader["ruta_imagen"].ToString() ?? "";
                    }
                }

                ViewBag.Plan = "SIN PLAN";
                ViewBag.Sede = "Sin sucursal asignada";
                ViewBag.MiembroDesde = "Sin membresía";
                ViewBag.SubtituloMiembro = "Todavía no tienes una membresía registrada";
                ViewBag.EtiquetaMembresia = "MEMBRESÍA NO REGISTRADA";
                ViewBag.Vence = "No aplica";
                ViewBag.SucursalBase = "Sin asignar";
                ViewBag.Precio = "No aplica";
                ViewBag.EstadoMembresia = "Sin membresía";
                ViewBag.IdPlanActual = 0;
                ViewBag.ProximoPlan = null;
                ViewBag.TieneSolicitudPendiente = false;

                string sqlMembresia = @"
                    SELECT TOP 1
                        m.fecha_inicio,
                        m.fecha_fin,
                        m.estado,
                        p.nombre AS plan_nombre,
                        p.precio,
                        s.nombre AS sucursal_nombre,
                        m.id_plan
                    FROM Membresias m
                    LEFT JOIN Planes p
                        ON m.id_plan = p.id_plan
                    LEFT JOIN Sucursal s
                        ON m.id_sucursal = s.id_sucursal
                    WHERE m.id_cliente = @IdCliente
                    ORDER BY
                        CASE
                            WHEN m.estado = 'Activa'
                                 AND m.fecha_inicio <= CAST(GETDATE() AS DATE)
                                 AND m.fecha_fin >= CAST(GETDATE() AS DATE)
                                THEN 0
                            WHEN m.estado = 'Pendiente'
                                THEN 1
                            WHEN m.estado = 'Activa'
                                 AND m.fecha_inicio > CAST(GETDATE() AS DATE)
                                THEN 2
                            ELSE 3
                        END,
                        m.fecha_fin DESC";

                using (SqlCommand cmd = new SqlCommand(sqlMembresia, con))
                {
                    cmd.Parameters.AddWithValue("@IdCliente", idCliente.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string plan = reader["plan_nombre"] == DBNull.Value
                                ? "SIN PLAN"
                                : reader["plan_nombre"].ToString() ?? "SIN PLAN";

                            string sede = reader["sucursal_nombre"] == DBNull.Value
                                ? "Sin asignar"
                                : reader["sucursal_nombre"].ToString() ?? "Sin asignar";

                            DateTime inicio = Convert.ToDateTime(
                                reader["fecha_inicio"]
                            );

                            DateTime fin = Convert.ToDateTime(
                                reader["fecha_fin"]
                            );

                            string estado = reader["estado"] == DBNull.Value
                                ? "Sin especificar"
                                : reader["estado"].ToString() ?? "Sin especificar";

                            ViewBag.Plan = plan;
                            ViewBag.Sede = sede;
                            ViewBag.MiembroDesde = inicio.Year.ToString();
                            ViewBag.SubtituloMiembro =
                                "Miembro " + plan + " desde " + inicio.Year;

                            ViewBag.Vence = fin.ToString(
                                "dd MMM yyyy",
                                CultureInfo.GetCultureInfo("es-SV")
                            );

                            ViewBag.SucursalBase = sede;

                            ViewBag.Precio = reader["precio"] == DBNull.Value
                                ? "Sin precio"
                                : "$" + Convert.ToDecimal(reader["precio"])
                                    .ToString("0.00", CultureInfo.InvariantCulture);

                            if (estado == "Pendiente")
                            {
                                ViewBag.EtiquetaMembresia = "PAGO PENDIENTE";
                                ViewBag.EstadoMembresia = "Pendiente";
                            }
                            else if (estado == "Activa" &&
                                     inicio.Date > DateTime.Today)
                            {
                                ViewBag.EtiquetaMembresia = "MEMBRESÍA PROGRAMADA";
                                ViewBag.EstadoMembresia = "Programada";
                            }
                            else if (estado == "Activa" &&
                                     fin.Date >= DateTime.Today)
                            {
                                ViewBag.EtiquetaMembresia = "MEMBRESÍA ACTIVA";
                                ViewBag.EstadoMembresia = "Activa";

                                ViewBag.IdPlanActual = reader["id_plan"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(reader["id_plan"]);
                            }
                            else
                            {
                                ViewBag.EtiquetaMembresia = "MEMBRESÍA VENCIDA";
                                ViewBag.EstadoMembresia = estado;
                            }
                        }
                    }
                }

                string sqlPendiente = @"
                    SELECT TOP 1
                        p.nombre,
                        m.fecha_inicio
                    FROM Membresias m
                    INNER JOIN Planes p
                        ON p.id_plan = m.id_plan
                    WHERE m.id_cliente = @Cliente
                      AND m.estado = 'Pendiente'
                    ORDER BY m.fecha_inicio DESC";

                using (SqlCommand cmd = new SqlCommand(sqlPendiente, con))
                {
                    cmd.Parameters.AddWithValue("@Cliente", idCliente.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ViewBag.TieneSolicitudPendiente = true;

                            ViewBag.ProximoPlan =
                                "Solicitud pendiente: " +
                                reader.GetString(0) +
                                " | Inicio previsto: " +
                                reader.GetDateTime(1).ToString("dd/MM/yyyy");
                        }
                    }
                }

                RutinaModelo? rutina = null;
                int idRutina = 0;

                string sqlRutina = @"
                    SELECT TOP 1
                        r.id_rutina,
                        r.objetivo,
                        e.nombre AS entrenador
                    FROM Rutina r
                    LEFT JOIN Entrenador e
                        ON r.id_entrenador = e.id_entrenador
                    WHERE r.id_cliente = @IdCliente
                    ORDER BY r.fecha_inicio DESC, r.id_rutina DESC";

                using (SqlCommand cmd = new SqlCommand(sqlRutina, con))
                {
                    cmd.Parameters.AddWithValue("@IdCliente", idCliente.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idRutina = Convert.ToInt32(reader["id_rutina"]);

                            rutina = new RutinaModelo
                            {
                                Objetivo = reader["objetivo"] == DBNull.Value
                                    ? "Sin objetivo registrado"
                                    : reader["objetivo"].ToString() ?? "Sin objetivo registrado",

                                Entrenador = reader["entrenador"] == DBNull.Value
                                    ? "Sin asignar"
                                    : reader["entrenador"].ToString() ?? "Sin asignar"
                            };
                        }
                    }
                }

                if (rutina != null)
                {
                    string sqlEjercicios = @"
                        SELECT
                            e.nombre,
                            d.Series,
                            d.Repeticiones,
                            d.Peso
                        FROM Rutina_detalle d
                        INNER JOIN Ejercicio e
                            ON d.id_ejercicio = e.id_ejercicio
                        WHERE d.id_rutina = @IdRutina";

                    using (SqlCommand cmd = new SqlCommand(sqlEjercicios, con))
                    {
                        cmd.Parameters.AddWithValue("@IdRutina", idRutina);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rutina.Ejercicios.Add(new EjercicioRutinaModelo
                                {
                                    Nombre = reader["nombre"].ToString() ?? "Ejercicio",

                                    Series = reader["Series"] == DBNull.Value
                                        ? 0
                                        : Convert.ToInt32(reader["Series"]),

                                    Repeticiones = reader["Repeticiones"] == DBNull.Value
                                        ? ""
                                        : reader["Repeticiones"].ToString() ?? "",

                                    Peso = reader["Peso"] == DBNull.Value
                                        ? "Sin registrar"
                                        : Convert.ToDecimal(reader["Peso"])
                                            .ToString("0.##", CultureInfo.InvariantCulture) + " kg"
                                });
                            }
                        }
                    }
                }

                ViewBag.Rutina = rutina;

                EvaluacionModelo? evaluacion = null;

                string sqlEvaluacion = @"
                    SELECT fecha, peso, imc, porcentaje_grasa
                    FROM Evaluacion
                    WHERE id_cliente = @IdCliente
                    ORDER BY fecha, id_evaluacion";

                using (SqlCommand cmd = new SqlCommand(sqlEvaluacion, con))
                {
                    cmd.Parameters.AddWithValue("@IdCliente", idCliente.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        decimal primerPeso = 0;
                        decimal primerImc = 0;
                        decimal primeraGrasa = 0;
                        bool primera = true;

                        while (reader.Read())
                        {
                            if (evaluacion == null)
                            {
                                evaluacion = new EvaluacionModelo();
                            }

                            DateTime fecha = reader["fecha"] == DBNull.Value
                                ? DateTime.Today
                                : Convert.ToDateTime(reader["fecha"]);

                            decimal peso = reader["peso"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader["peso"]);

                            decimal imc = reader["imc"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader["imc"]);

                            decimal grasa = reader["porcentaje_grasa"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(reader["porcentaje_grasa"]);

                            if (primera)
                            {
                                primerPeso = peso;
                                primerImc = imc;
                                primeraGrasa = grasa;
                                primera = false;
                            }

                            evaluacion.PesoActual = peso;
                            evaluacion.ImcActual = imc;
                            evaluacion.GrasaActual = grasa;
                            evaluacion.Anio = fecha.Year;

                            evaluacion.Historial.Add(
                                new RegistroEvaluacionModelo
                                {
                                    Mes = fecha.ToString(
                                        "MMM",
                                        CultureInfo.GetCultureInfo("es-SV")
                                    ),
                                    Peso = peso,
                                    Grasa = grasa
                                }
                            );
                        }

                        if (evaluacion != null)
                        {
                            evaluacion.CambioPeso =
                                evaluacion.PesoActual - primerPeso;

                            evaluacion.CambioImc =
                                evaluacion.ImcActual - primerImc;

                            evaluacion.CambioGrasa =
                                evaluacion.GrasaActual - primeraGrasa;
                        }
                    }
                }

                ViewBag.Evaluacion = evaluacion;
            }

            List<ReservaModelo> reservas = new List<ReservaModelo>();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sqlReservas = @"
                    SELECT
                        r.id_reserva,
                        r.id_horario,
                        h.dia_fecha
                    FROM Reserva r
                    INNER JOIN Horario h
                        ON h.id_horario = r.id_horario
                    WHERE r.id_cliente = @IdCliente
                      AND r.estado = 'Confirmada'
                    ORDER BY h.dia_fecha, h.hora_inicio";

                using (SqlCommand cmd = new SqlCommand(sqlReservas, con))
                {
                    cmd.Parameters.AddWithValue("@IdCliente", idCliente.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        ClasesDAL datos = new ClasesDAL();

                        while (reader.Read())
                        {
                            int idReserva = Convert.ToInt32(
                                reader["id_reserva"]
                            );

                            int idHorario = Convert.ToInt32(
                                reader["id_horario"]
                            );

                            ClaseHorarioModelo? clase =
                                datos.ObtenerPorId(idHorario);

                            if (clase == null)
                            {
                                continue;
                            }

                            DateTime fecha = reader["dia_fecha"] == DBNull.Value
                                ? clase.ProximaFecha
                                : Convert.ToDateTime(reader["dia_fecha"]);

                            reservas.Add(new ReservaModelo
                            {
                                IdReserva = idReserva,
                                IdClase = idHorario,
                                Clase = clase,
                                Fecha = fecha
                            });
                        }
                    }
                }
            }

            return View(reservas);
        }

        [HttpGet]
        public IActionResult Editar()
        {
            int? id = HttpContext.Session.GetInt32("IdCliente");
            if (HttpContext.Session.GetInt32("Rol") != 2 || !id.HasValue) return RedirectToAction("Login", "Account");
            var modelo = new ClienteFormularioAdminModelo();
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT nombre, email, telefono, fecha_nacimiento FROM Cliente WHERE id_cliente=@Id", con))
                {
                    cmd.Parameters.AddWithValue("@Id", id.Value);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return RedirectToAction("Login", "Account");
                        modelo.Nombre = reader.GetString(0); modelo.Correo = reader.GetString(1);
                        modelo.Telefono = reader[2]?.ToString() ?? "";
                        modelo.FechaNacimiento = reader[3] == DBNull.Value ? null : Convert.ToDateTime(reader[3]);
                    }
                }
            }
            return View(modelo);
        }

        [HttpPost]
        
        public IActionResult Editar(ClienteFormularioAdminModelo modelo)
        {
            int? id = HttpContext.Session.GetInt32("IdCliente");
            if (HttpContext.Session.GetInt32("Rol") != 2 || !id.HasValue) return RedirectToAction("Login", "Account");
            string correo = modelo.Correo?.Trim().ToLowerInvariant() ?? "";
            if (modelo.FechaNacimiento.HasValue && modelo.FechaNacimiento.Value.Date > DateTime.Today)
                ModelState.AddModelError(nameof(modelo.FechaNacimiento), "La fecha no puede ser futura");
            if (!string.IsNullOrWhiteSpace(modelo.Contrasena) && (modelo.Contrasena.Length < 8 || !modelo.Contrasena.Any(char.IsLetter) || !modelo.Contrasena.Any(char.IsDigit)))
                ModelState.AddModelError(nameof(modelo.Contrasena), "La nueva contraseña necesita al menos ocho caracteres, letras y números");
            if (!ModelState.IsValid) return View(modelo);
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(@"SELECT COUNT(*) FROM Cliente WHERE email=@Email AND id_cliente<>@Id
                    UNION ALL SELECT COUNT(*) FROM Administrador WHERE email=@Email", con))
                {
                    cmd.Parameters.AddWithValue("@Email", correo); cmd.Parameters.AddWithValue("@Id", id.Value);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        int cantidad = 0;
                        while (reader.Read()) cantidad += Convert.ToInt32(reader[0]);
                        if (cantidad > 0) ModelState.AddModelError(nameof(modelo.Correo), "Este correo ya está registrado");
                    }
                }
                if (!ModelState.IsValid) return View(modelo);
                string sql = string.IsNullOrWhiteSpace(modelo.Contrasena)
                    ? "UPDATE Cliente SET nombre=@Nombre, email=@Email, telefono=@Telefono, fecha_nacimiento=@Fecha WHERE id_cliente=@Id"
                    : "UPDATE Cliente SET nombre=@Nombre, email=@Email, telefono=@Telefono, fecha_nacimiento=@Fecha, [contraseña]=@Clave WHERE id_cliente=@Id";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id.Value); cmd.Parameters.AddWithValue("@Nombre", modelo.Nombre.Trim());
                    cmd.Parameters.AddWithValue("@Email", correo); cmd.Parameters.AddWithValue("@Telefono", modelo.Telefono.Trim());
                    cmd.Parameters.AddWithValue("@Fecha", modelo.FechaNacimiento!.Value.Date);

                    if (!string.IsNullOrWhiteSpace(modelo.Contrasena))
                    {
                        cmd.Parameters.AddWithValue("@Clave", modelo.Contrasena);
                    }

                    cmd.ExecuteNonQuery();
                }
            }
            HttpContext.Session.SetString("Usuario", correo);
            HttpContext.Session.SetString("NombreUsuario", modelo.Nombre.Trim());
            return RedirectToAction(nameof(Index));
        }
    }
}
