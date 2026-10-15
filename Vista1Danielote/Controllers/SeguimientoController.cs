using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers
{
    public class SeguimientoController : Controller
    {
        private readonly Conexion conexion = new Conexion();
        private bool EsAdmin() => HttpContext.Session.GetInt32("Rol") == 1;
        private static string Normalizar(string? s) => s == "Rutinas" || s == "Ejercicios" || s == "Detalles" || s == "Asistencias" ? s : "Evaluaciones";

        [HttpGet]
        public IActionResult Index(string seccion = "Evaluaciones")
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            seccion = Normalizar(seccion);
            var filas = new List<SeguimientoFilaModelo>();
            string sql = seccion == "Evaluaciones" ? @"SELECT e.id_evaluacion, c.nombre, COALESCE(t.nombre,'Sin entrenador'), e.fecha,
                CONCAT('Peso: ',COALESCE(CAST(e.peso AS VARCHAR(20)),'—'),' kg; IMC: ',COALESCE(CAST(e.imc AS VARCHAR(20)),'—'),'; Grasa: ',COALESCE(CAST(e.porcentaje_grasa AS VARCHAR(20)),'—'),'%'), 0
                FROM Evaluacion e LEFT JOIN Cliente c ON c.id_cliente=e.id_cliente LEFT JOIN Entrenador t ON t.id_entrenador=e.id_entrenador ORDER BY e.fecha DESC"
                : seccion == "Rutinas" ? @"SELECT r.id_rutina, c.nombre, COALESCE(t.nombre,'Sin entrenador'), r.fecha_inicio, COALESCE(r.objetivo,''), 0
                FROM Rutina r LEFT JOIN Cliente c ON c.id_cliente=r.id_cliente LEFT JOIN Entrenador t ON t.id_entrenador=r.id_entrenador ORDER BY r.id_rutina DESC"
                : seccion == "Ejercicios" ? @"SELECT id_ejercicio, nombre, COALESCE(grupo,''), NULL, COALESCE(grupo_muscular,''), 0 FROM Ejercicio ORDER BY nombre"
                : seccion == "Detalles" ? @"SELECT d.id_rutina, COALESCE(e.nombre,'Ejercicio'), CONCAT('Rutina #',d.id_rutina), NULL,
                    CONCAT(d.Series,' series; ', d.Repeticiones,' repeticiones; ',d.Peso,' kg'), d.id_ejercicio
                    FROM Rutina_detalle d LEFT JOIN Ejercicio e ON e.id_ejercicio=d.id_ejercicio ORDER BY d.id_rutina DESC"
                : @"SELECT a.id_asistencia, c.nombre, CONCAT('Reserva #',COALESCE(CAST(a.id_reserva AS VARCHAR(20)),'Ninguna')),
                    a.fecha_asistencia, CONCAT('Entrada: ',COALESCE(CONVERT(VARCHAR(5),a.hora_entrada,108),'—'),'; Salida: ',COALESCE(CONVERT(VARCHAR(5),a.hora_salida,108),'—')), 0
                    FROM Asistencia a LEFT JOIN Cliente c ON c.id_cliente=a.id_cliente ORDER BY a.fecha_asistencia DESC";
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(sql, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read()) filas.Add(new SeguimientoFilaModelo
                    {
                        Id = Convert.ToInt32(reader[0]), Titulo = reader[1]?.ToString() ?? "",
                        Subtitulo = reader[2]?.ToString() ?? "",
                        Fecha = reader[3] == DBNull.Value ? "" : Convert.ToDateTime(reader[3]).ToString("dd/MM/yyyy"),
                        Detalle = reader[4]?.ToString() ?? "", IdRelacionado = Convert.ToInt32(reader[5])
                    });
            }
            ViewBag.Seccion = seccion; return View(filas);
        }

        [HttpGet]
        public IActionResult Formulario(string seccion = "Evaluaciones", int? id = null, int? relacionado = null)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            var modelo = new SeguimientoFormularioModelo { Seccion = Normalizar(seccion) };
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                if (id.HasValue)
                {
                    string sql = modelo.Seccion == "Evaluaciones" ? "SELECT id_cliente,id_entrenador,fecha,peso,imc,porcentaje_grasa FROM Evaluacion WHERE id_evaluacion=@Id"
                        : modelo.Seccion == "Rutinas" ? "SELECT id_cliente,id_entrenador,fecha_inicio,objetivo FROM Rutina WHERE id_rutina=@Id"
                        : modelo.Seccion == "Ejercicios" ? "SELECT nombre,grupo,grupo_muscular FROM Ejercicio WHERE id_ejercicio=@Id"
                        : modelo.Seccion == "Asistencias" ? "SELECT id_cliente,id_reserva,fecha_asistencia,hora_entrada,hora_salida FROM Asistencia WHERE id_asistencia=@Id"
                        : "SELECT d.Series,d.Repeticiones,d.Peso FROM Rutina_detalle d WHERE d.id_rutina=@Id AND d.id_ejercicio=@Relacionado";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id.Value);
                        cmd.Parameters.AddWithValue("@Relacionado", relacionado ?? 0);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return NotFound();
                            modelo.Id = id.Value;
                            if (modelo.Seccion == "Evaluaciones")
                            {
                                modelo.IdCliente = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                                modelo.IdEntrenador = reader[1] == DBNull.Value ? 0 : Convert.ToInt32(reader[1]);
                                modelo.Fecha = reader[2] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(reader[2]);
                                modelo.Peso = reader[3] == DBNull.Value ? 0 : Convert.ToDecimal(reader[3]);
                                modelo.Imc = reader[4] == DBNull.Value ? 0 : Convert.ToDecimal(reader[4]);
                                modelo.PorcentajeGrasa = reader[5] == DBNull.Value ? 0 : Convert.ToDecimal(reader[5]);
                            }
                            else if (modelo.Seccion == "Rutinas")
                            {
                                modelo.IdCliente = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                                modelo.IdEntrenador = reader[1] == DBNull.Value ? 0 : Convert.ToInt32(reader[1]);
                                modelo.Fecha = reader[2] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(reader[2]);
                                modelo.Objetivo = reader[3]?.ToString() ?? "";
                            }
                            else if (modelo.Seccion == "Ejercicios")
                            {
                                modelo.Nombre = reader[0]?.ToString() ?? ""; modelo.Grupo = reader[1]?.ToString() ?? "";
                                modelo.GrupoMuscular = reader[2]?.ToString() ?? "";
                            }
                            else if (modelo.Seccion == "Asistencias")
                            {
                                modelo.IdCliente = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                                modelo.IdReserva = reader[1] == DBNull.Value ? null : Convert.ToInt32(reader[1]);
                                modelo.Fecha = reader[2] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(reader[2]);
                                modelo.HoraEntrada = reader[3] == DBNull.Value ? TimeSpan.Zero : (TimeSpan)reader[3];
                                modelo.HoraSalida = reader[4] == DBNull.Value ? null : (TimeSpan)reader[4];
                            }
                            else
                            {
                                modelo.IdRutina = id.Value; modelo.IdEjercicio = relacionado ?? 0;
                                modelo.Series = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                                modelo.Repeticiones = reader[1]?.ToString() ?? "";
                                modelo.Peso = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);
                            }
                        }
                    }
                }
                CargarOpciones(con);
            }
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Formulario(SeguimientoFormularioModelo m)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            m.Seccion = Normalizar(m.Seccion);
            if ((m.Seccion == "Evaluaciones" || m.Seccion == "Rutinas" || m.Seccion == "Asistencias") && m.IdCliente <= 0)
                ModelState.AddModelError(nameof(m.IdCliente), "Selecciona un cliente");
            if (m.Seccion == "Evaluaciones" && (m.Peso <= 0 || m.Imc <= 0 || m.PorcentajeGrasa < 0))
                ModelState.AddModelError("", "Ingresa peso, IMC y grasa válidos");
            if (m.Seccion == "Ejercicios" && string.IsNullOrWhiteSpace(m.Nombre))
                ModelState.AddModelError(nameof(m.Nombre), "Ingresa el ejercicio");
            if (m.Seccion == "Detalles" && (m.IdRutina <= 0 || m.IdEjercicio <= 0 || m.Series <= 0))
                ModelState.AddModelError("", "Selecciona rutina y ejercicio e indica las series");
            if (m.Seccion == "Asistencias" && m.HoraSalida.HasValue && m.HoraSalida.Value <= m.HoraEntrada)
                ModelState.AddModelError("", "La salida debe ser posterior a la entrada");
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                if (!ModelState.IsValid) { CargarOpciones(con); return View(m); }
                string sql = m.Seccion == "Evaluaciones"
                    ? m.Id == 0 ? "INSERT INTO Evaluacion (id_cliente,id_entrenador,fecha,peso,imc,porcentaje_grasa) VALUES (@Cliente,@Entrenador,@Fecha,@Peso,@Imc,@Grasa)"
                    : "UPDATE Evaluacion SET id_cliente=@Cliente,id_entrenador=@Entrenador,fecha=@Fecha,peso=@Peso,imc=@Imc,porcentaje_grasa=@Grasa WHERE id_evaluacion=@Id"
                    : m.Seccion == "Rutinas"
                    ? m.Id == 0 ? "INSERT INTO Rutina (id_cliente,id_entrenador,fecha_inicio,objetivo) VALUES (@Cliente,@Entrenador,@Fecha,@Objetivo)"
                    : "UPDATE Rutina SET id_cliente=@Cliente,id_entrenador=@Entrenador,fecha_inicio=@Fecha,objetivo=@Objetivo WHERE id_rutina=@Id"
                    : m.Seccion == "Ejercicios"
                    ? m.Id == 0 ? "INSERT INTO Ejercicio (nombre,grupo,grupo_muscular) VALUES (@Nombre,@Grupo,@Musculo)"
                    : "UPDATE Ejercicio SET nombre=@Nombre,grupo=@Grupo,grupo_muscular=@Musculo WHERE id_ejercicio=@Id"
                    : m.Seccion == "Asistencias"
                    ? m.Id == 0 ? "INSERT INTO Asistencia (id_cliente,id_reserva,fecha_asistencia,hora_entrada,hora_salida) VALUES (@Cliente,@Reserva,@Fecha,@Entrada,@Salida)"
                    : "UPDATE Asistencia SET id_cliente=@Cliente,id_reserva=@Reserva,fecha_asistencia=@Fecha,hora_entrada=@Entrada,hora_salida=@Salida WHERE id_asistencia=@Id"
                    : m.Id == 0 ? "INSERT INTO Rutina_detalle (id_rutina,id_ejercicio,Series,Repeticiones,Peso) VALUES (@Rutina,@Ejercicio,@Series,@Reps,@Peso)"
                    : "UPDATE Rutina_detalle SET Series=@Series,Repeticiones=@Reps,Peso=@Peso WHERE id_rutina=@Rutina AND id_ejercicio=@Ejercicio";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", m.Id); cmd.Parameters.AddWithValue("@Cliente", m.IdCliente);
                    cmd.Parameters.AddWithValue("@Entrenador", m.IdEntrenador == 0 ? (object)DBNull.Value : m.IdEntrenador);
                    cmd.Parameters.AddWithValue("@Reserva", m.IdReserva.HasValue && m.IdReserva.Value > 0 ? (object)m.IdReserva.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Fecha", m.Fecha.Date); cmd.Parameters.AddWithValue("@Peso", m.Peso);
                    cmd.Parameters.AddWithValue("@Imc", m.Imc); cmd.Parameters.AddWithValue("@Grasa", m.PorcentajeGrasa);
                    cmd.Parameters.AddWithValue("@Objetivo", m.Objetivo ?? ""); cmd.Parameters.AddWithValue("@Nombre", m.Nombre ?? "");
                    cmd.Parameters.AddWithValue("@Grupo", m.Grupo ?? ""); cmd.Parameters.AddWithValue("@Musculo", m.GrupoMuscular ?? "");
                    cmd.Parameters.AddWithValue("@Entrada", m.HoraEntrada); cmd.Parameters.AddWithValue("@Salida", m.HoraSalida.HasValue ? (object)m.HoraSalida.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Rutina", m.IdRutina == 0 ? m.Id : m.IdRutina);
                    cmd.Parameters.AddWithValue("@Ejercicio", m.IdEjercicio); cmd.Parameters.AddWithValue("@Series", m.Series);
                    cmd.Parameters.AddWithValue("@Reps", m.Repeticiones ?? "");
                    try { cmd.ExecuteNonQuery(); }
                    catch (SqlException)
                    {
                        ModelState.AddModelError("", "No se pudo guardar. Comprueba los datos, permisos y registros seleccionados.");
                        CargarOpciones(con); return View(m);
                    }
                }
            }
            return RedirectToAction(nameof(Index), new { seccion = m.Seccion });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(string seccion, int id, int relacionado = 0)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            seccion = Normalizar(seccion);
            string sql = seccion == "Evaluaciones" ? "DELETE FROM Evaluacion WHERE id_evaluacion=@Id"
                : seccion == "Rutinas" ? "DELETE FROM Rutina WHERE id_rutina=@Id"
                : seccion == "Ejercicios" ? "DELETE FROM Ejercicio WHERE id_ejercicio=@Id"
                : seccion == "Asistencias" ? "DELETE FROM Asistencia WHERE id_asistencia=@Id"
                : "DELETE FROM Rutina_detalle WHERE id_rutina=@Id AND id_ejercicio=@Relacionado";
            try
            {
                using (SqlConnection con = conexion.GetConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    { cmd.Parameters.AddWithValue("@Id", id); cmd.Parameters.AddWithValue("@Relacionado", relacionado); cmd.ExecuteNonQuery(); }
                }
            }
            catch (SqlException) { TempData["Error"] = "No se puede eliminar este registro porque tiene elementos relacionados."; }
            return RedirectToAction(nameof(Index), new { seccion });
        }

        private void CargarOpciones(SqlConnection con)
        {
            var opciones = new Dictionary<string, List<(int Id, string Nombre)>>();
            var consultas = new Dictionary<string, string>
            {
                ["Clientes"] = "SELECT id_cliente, nombre FROM Cliente ORDER BY nombre",
                ["Entrenadores"] = "SELECT id_entrenador, nombre FROM Entrenador ORDER BY nombre",
                ["Rutinas"] = "SELECT id_rutina, CONCAT('Rutina #',id_rutina,': ',COALESCE(objetivo,'')) FROM Rutina ORDER BY id_rutina DESC",
                ["Ejercicios"] = "SELECT id_ejercicio, nombre FROM Ejercicio ORDER BY nombre",
                ["Reservas"] = "SELECT id_reserva, CONCAT('Reserva #',id_reserva) FROM Reserva ORDER BY id_reserva DESC"
            };
            foreach (var consulta in consultas)
            {
                var lista = new List<(int Id, string Nombre)>();
                using (SqlCommand cmd = new SqlCommand(consulta.Value, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read()) lista.Add((reader.GetInt32(0), reader[1]?.ToString() ?? ""));
                opciones[consulta.Key] = lista;
            }
            ViewBag.Opciones = opciones;
        }
    }
}
