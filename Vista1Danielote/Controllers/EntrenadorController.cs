
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers
{
    public class EntrenadorController : Controller
    {
        private readonly Conexion conexion = new Conexion();

        private int? EntrenadorActual()
        {
            if (HttpContext.Session.GetInt32("Rol") != 3)
            {
                return null;
            }

            return HttpContext.Session.GetInt32("IdEntrenador");
        }

        [HttpGet]
        public IActionResult Index()
        {
            int? id = EntrenadorActual();

            if (!id.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            EntrenadorPanelModelo modelo = new EntrenadorPanelModelo();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sqlDatos = @"
                    SELECT e.nombre,
                           e.especialidad,
                           e.certificaciones,
                           COALESCE(s.nombre, 'Sin sucursal')
                    FROM Entrenador e
                    LEFT JOIN Sucursal s
                        ON s.id_sucursal = e.id_sucursal
                    WHERE e.id_entrenador = @Id";

                using (SqlCommand cmd = new SqlCommand(sqlDatos, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            HttpContext.Session.Clear();
                            return RedirectToAction("Login", "Account");
                        }

                        modelo.Nombre = reader[0]?.ToString() ?? "";
                        modelo.Especialidad = reader[1]?.ToString() ?? "";
                        modelo.Certificaciones = reader[2]?.ToString() ?? "";
                        modelo.Sucursal = reader[3]?.ToString() ?? "";
                    }
                }

                string sqlClases = @"
                    SELECT c.nombre,
                           h.dia_fecha,
                           h.hora_inicio,
                           h.hora_fin,
                           COALESCE(s.nombre, 'Sin sucursal'),
                           (
                               SELECT COUNT(*)
                               FROM Reserva r
                               WHERE r.id_horario = h.id_horario
                               AND r.estado = 'Confirmada'
                           ),
                           c.capacidad_max
                    FROM Horario h
                    INNER JOIN Clase c
                        ON c.id_clase = h.id_clase
                    LEFT JOIN Sucursal s
                        ON s.id_sucursal = c.id_sucursal
                    WHERE h.id_entrenador = @Id
                    AND h.dia_fecha >= @Hoy
                    ORDER BY h.dia_fecha, h.hora_inicio";

                using (SqlCommand cmd = new SqlCommand(sqlClases, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id.Value);
                    cmd.Parameters.AddWithValue("@Hoy", DateTime.Today);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            EntrenadorClaseModelo clase =
                                new EntrenadorClaseModelo();

                            clase.Nombre = reader[0]?.ToString() ?? "";
                            clase.Fecha = Convert.ToDateTime(reader[1]);
                            clase.HoraInicio = (TimeSpan)reader[2];
                            clase.HoraFin = (TimeSpan)reader[3];
                            clase.Sucursal = reader[4]?.ToString() ?? "";
                            clase.Reservas = Convert.ToInt32(reader[5]);
                            clase.Capacidad = Convert.ToInt32(reader[6]);

                            modelo.Clases.Add(clase);
                        }
                    }
                }

                string sqlAlumnos = @"
                    SELECT DISTINCT
                           c.id_cliente,
                           c.nombre,
                           c.email
                    FROM Cliente c
                    INNER JOIN Reserva r
                        ON r.id_cliente = c.id_cliente
                    INNER JOIN Horario h
                        ON h.id_horario = r.id_horario
                    WHERE h.id_entrenador = @Id
                    AND r.estado = 'Confirmada'
                    ORDER BY c.nombre";

                using (SqlCommand cmd = new SqlCommand(sqlAlumnos, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            EntrenadorAlumnoModelo alumno =
                                new EntrenadorAlumnoModelo();

                            alumno.Id = Convert.ToInt32(reader[0]);
                            alumno.Nombre = reader[1]?.ToString() ?? "";
                            alumno.Correo = reader[2]?.ToString() ?? "";

                            modelo.Alumnos.Add(alumno);
                        }
                    }
                }

                string sqlRutinas = @"
                    SELECT c.nombre,
                           r.fecha_inicio,
                           r.objetivo
                    FROM Rutina r
                    INNER JOIN Cliente c
                        ON c.id_cliente = r.id_cliente
                    WHERE r.id_entrenador = @Id
                    ORDER BY r.fecha_inicio DESC";

                using (SqlCommand cmd = new SqlCommand(sqlRutinas, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            EntrenadorRegistroModelo rutina =
                                new EntrenadorRegistroModelo();

                            rutina.Cliente = reader[0]?.ToString() ?? "";
                            rutina.Fecha = Convert.ToDateTime(reader[1]);
                            rutina.Detalle = reader[2]?.ToString() ?? "";

                            modelo.Rutinas.Add(rutina);
                        }
                    }
                }

                string sqlEvaluaciones = @"
                    SELECT c.nombre,
                           e.fecha,
                           e.peso,
                           e.imc,
                           e.porcentaje_grasa
                    FROM Evaluacion e
                    INNER JOIN Cliente c
                        ON c.id_cliente = e.id_cliente
                    WHERE e.id_entrenador = @Id
                    ORDER BY e.fecha DESC";

                using (SqlCommand cmd = new SqlCommand(sqlEvaluaciones, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            EntrenadorRegistroModelo evaluacion =
                                new EntrenadorRegistroModelo();

                            evaluacion.Cliente = reader[0]?.ToString() ?? "";
                            evaluacion.Fecha = Convert.ToDateTime(reader[1]);

                            evaluacion.Detalle =
                                $"Peso: {reader[2]} kg | " +
                                $"IMC: {reader[3]} | " +
                                $"Grasa: {reader[4]}%";

                            modelo.Evaluaciones.Add(evaluacion);
                        }
                    }
                }
            }

            List<OpcionModelo> rutinasDisponibles =
                new List<OpcionModelo>();

            List<OpcionModelo> ejerciciosDisponibles =
                new List<OpcionModelo>();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sqlRutinasDisponibles = @"
                    SELECT r.id_rutina,
                           CONCAT(
                               c.nombre,
                               ' - ',
                               COALESCE(r.objetivo, '')
                           )
                    FROM Rutina r
                    INNER JOIN Cliente c
                        ON c.id_cliente = r.id_cliente
                    WHERE r.id_entrenador = @Entrenador
                    ORDER BY r.id_rutina DESC";

                using (SqlCommand cmd =
                    new SqlCommand(sqlRutinasDisponibles, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Entrenador",
                        id.Value
                    );

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rutinasDisponibles.Add(new OpcionModelo
                            {
                                Id = Convert.ToInt32(reader[0]),
                                Nombre = reader[1]?.ToString() ?? ""
                            });
                        }
                    }
                }

                string sqlEjerciciosDisponibles = @"
                    SELECT id_ejercicio, nombre
                    FROM Ejercicio
                    ORDER BY nombre";

                using (SqlCommand cmd =
                    new SqlCommand(sqlEjerciciosDisponibles, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ejerciciosDisponibles.Add(new OpcionModelo
                            {
                                Id = Convert.ToInt32(reader[0]),
                                Nombre = reader[1]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            ViewBag.RutinasDisponibles = rutinasDisponibles;
            ViewBag.EjerciciosDisponibles = ejerciciosDisponibles;

            return View("~/Views/Entrenadores/Index.cshtml", modelo);
        }

        private bool EsAlumno(
            int idEntrenador,
            int idCliente,
            SqlConnection con)
        {
            string sql = @"
                SELECT COUNT(*)
                FROM Reserva r
                INNER JOIN Horario h
                    ON h.id_horario = r.id_horario
                WHERE h.id_entrenador = @Entrenador
                AND r.id_cliente = @Cliente
                AND r.estado = 'Confirmada'";

            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue(
                    "@Entrenador",
                    idEntrenador
                );

                cmd.Parameters.AddWithValue(
                    "@Cliente",
                    idCliente
                );

                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        [HttpPost]
        public IActionResult CrearRutina(
            int idCliente,
            string objetivo)
        {
            int? id = EntrenadorActual();

            if (!id.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (idCliente <= 0 ||
                string.IsNullOrWhiteSpace(objetivo) ||
                objetivo.Trim().Length > 150)
            {
                TempData["ErrorEntrenador"] =
                    "Selecciona un alumno y escribe un objetivo de hasta 150 caracteres.";

                return RedirectToAction(nameof(Index));
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                if (!EsAlumno(id.Value, idCliente, con))
                {
                    return StatusCode(403);
                }

                string sql = @"
                    INSERT INTO Rutina
                    (id_cliente, id_entrenador, fecha_inicio, objetivo)
                    VALUES
                    (@Cliente, @Entrenador, @Fecha, @Objetivo)";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Cliente",
                        idCliente
                    );

                    cmd.Parameters.AddWithValue(
                        "@Entrenador",
                        id.Value
                    );

                    cmd.Parameters.AddWithValue(
                        "@Fecha",
                        DateTime.Today
                    );

                    cmd.Parameters.AddWithValue(
                        "@Objetivo",
                        objetivo.Trim()
                    );

                    cmd.ExecuteNonQuery();
                }
            }

            TempData["ExitoEntrenador"] =
                "Rutina registrada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult RegistrarEvaluacion(
            int idCliente,
            decimal peso,
            decimal imc,
            decimal porcentajeGrasa)
        {
            int? id = EntrenadorActual();

            if (!id.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (idCliente <= 0 ||
                peso <= 0 ||
                imc <= 0 ||
                porcentajeGrasa < 0 ||
                porcentajeGrasa > 100)
            {
                TempData["ErrorEntrenador"] =
                    "Verifica el alumno, peso, IMC y porcentaje de grasa.";

                return RedirectToAction(nameof(Index));
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                if (!EsAlumno(id.Value, idCliente, con))
                {
                    return StatusCode(403);
                }

                string sql = @"
                    INSERT INTO Evaluacion
                    (id_cliente, id_entrenador, fecha,
                     peso, imc, porcentaje_grasa)
                    VALUES
                    (@Cliente, @Entrenador, @Fecha,
                     @Peso, @Imc, @Grasa)";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Cliente",
                        idCliente
                    );

                    cmd.Parameters.AddWithValue(
                        "@Entrenador",
                        id.Value
                    );

                    cmd.Parameters.AddWithValue(
                        "@Fecha",
                        DateTime.Today
                    );

                    cmd.Parameters.AddWithValue(
                        "@Peso",
                        peso
                    );

                    cmd.Parameters.AddWithValue(
                        "@Imc",
                        imc
                    );

                    cmd.Parameters.AddWithValue(
                        "@Grasa",
                        porcentajeGrasa
                    );

                    cmd.ExecuteNonQuery();
                }
            }

            TempData["ExitoEntrenador"] =
                "Evaluación registrada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult AgregarEjercicio(
            int idRutina,
            int idEjercicio,
            int series,
            string? repeticiones,
            decimal peso)
        {
            int? idEntrenador = EntrenadorActual();

            if (!idEntrenador.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (idRutina <= 0 ||
                idEjercicio <= 0 ||
                series <= 0 ||
                series > 100 ||
                string.IsNullOrWhiteSpace(repeticiones) ||
                repeticiones.Trim().Length > 20 ||
                peso < 0 ||
                peso > 1000)
            {
                TempData["ErrorEntrenador"] =
                    "Selecciona una rutina y un ejercicio. Verifica las series, repeticiones y peso.";

                return RedirectToAction(nameof(Index));
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql = @"
                    INSERT INTO Rutina_detalle
                        (id_rutina, id_ejercicio,
                         Series, Repeticiones, Peso)
                    SELECT
                        @Rutina,
                        @Ejercicio,
                        @Series,
                        @Repeticiones,
                        @Peso
                    WHERE EXISTS
                    (
                        SELECT 1
                        FROM Rutina
                        WHERE id_rutina = @Rutina
                        AND id_entrenador = @Entrenador
                    )
                    AND EXISTS
                    (
                        SELECT 1
                        FROM Ejercicio
                        WHERE id_ejercicio = @Ejercicio
                    )
                    AND NOT EXISTS
                    (
                        SELECT 1
                        FROM Rutina_detalle
                        WHERE id_rutina = @Rutina
                        AND id_ejercicio = @Ejercicio
                    )";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Rutina",
                        idRutina
                    );

                    cmd.Parameters.AddWithValue(
                        "@Ejercicio",
                        idEjercicio
                    );

                    cmd.Parameters.AddWithValue(
                        "@Entrenador",
                        idEntrenador.Value
                    );

                    cmd.Parameters.AddWithValue(
                        "@Series",
                        series
                    );

                    cmd.Parameters.AddWithValue(
                        "@Repeticiones",
                        repeticiones.Trim()
                    );

                    cmd.Parameters.AddWithValue(
                        "@Peso",
                        peso
                    );

                    try
                    {
                        int filas = cmd.ExecuteNonQuery();

                        if (filas == 0)
                        {
                            TempData["ErrorEntrenador"] =
                                "No se pudo agregar el ejercicio. Comprueba que la rutina sea tuya y que el ejercicio no esté repetido.";

                            return RedirectToAction(nameof(Index));
                        }
                    }
                    catch (SqlException)
                    {
                        TempData["ErrorEntrenador"] =
                            "Ocurrió un problema al guardar el ejercicio. Comprueba los datos.";

                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            TempData["ExitoEntrenador"] =
                "Ejercicio agregado correctamente a la rutina.";

            return RedirectToAction(nameof(Index));
        }
    }
}
