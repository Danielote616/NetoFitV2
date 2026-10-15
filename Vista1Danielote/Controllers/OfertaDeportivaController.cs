using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers
{
    public class OfertaDeportivaController : Controller
    {
        private readonly Conexion conexion = new Conexion();
        private bool EsAdmin() => HttpContext.Session.GetInt32("Rol") == 1;

        [HttpGet]
        public IActionResult Index(string seccion = "Entrenadores")
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            var datos = new OfertaDeportivaViewModel { Seccion = Normalizar(seccion) };
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(@"SELECT e.id_entrenador, e.nombre, COALESCE(e.especialidad, ''), COALESCE(s.nombre, 'Sin sucursal'),
                    (SELECT COUNT(*) FROM Horario h WHERE h.id_entrenador = e.id_entrenador) FROM Entrenador e
                    LEFT JOIN Sucursal s ON s.id_sucursal=e.id_sucursal ORDER BY e.nombre", con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        string nombre = reader.GetString(1);
                        string[] partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        string iniciales = string.Concat(partes.Take(2).Select(x => x[0].ToString().ToUpperInvariant()));
                        datos.Entrenadores.Add(new EntrenadorOferta
                        {
                            Id = reader.GetInt32(0), Nombre = nombre, Especialidad = reader.GetString(2), Sucursal = reader.GetString(3),
                            ClasesPorSemana = reader.GetInt32(4), Iniciales = iniciales, ColorAvatar = "avatar-uno"
                        });
                    }
                using (SqlCommand cmd = new SqlCommand(@"SELECT c.id_clase, c.nombre, COALESCE(c.tipo, ''),
                    COALESCE((SELECT TOP 1 e.nombre FROM Horario h INNER JOIN Entrenador e ON h.id_entrenador=e.id_entrenador WHERE h.id_clase=c.id_clase ORDER BY h.dia_fecha DESC),'Sin asignar'),
                    COALESCE(s.nombre, 'Sin sucursal'), COALESCE(c.duracion, '')
                    FROM Clase c LEFT JOIN Sucursal s ON s.id_sucursal=c.id_sucursal ORDER BY c.nombre", con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        string duracion = reader.GetString(5).Replace(" min", "");
                        int.TryParse(duracion, out int minutos);
                        datos.Clases.Add(new ClaseOferta
                        {
                            Id = reader.GetInt32(0), Nombre = reader.GetString(1), Tipo = reader.GetString(2),
                            Entrenador = reader.GetString(3), Sucursal = reader.GetString(4), DuracionMinutos = minutos
                        });
                    }
                using (SqlCommand cmd = new SqlCommand(@"SELECT h.id_horario, c.nombre, h.dia_fecha, h.hora_inicio, h.hora_fin,
                    COALESCE(s.nombre,'Sin sucursal'), c.capacidad_max
                    FROM Horario h INNER JOIN Clase c ON c.id_clase=h.id_clase
                    LEFT JOIN Sucursal s ON s.id_sucursal=c.id_sucursal ORDER BY h.dia_fecha, h.hora_inicio", con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read()) datos.Horarios.Add(new HorarioOferta
                    {
                        Id = reader.GetInt32(0), Clase = reader.GetString(1), Dia = reader[2] == DBNull.Value ? "Sin fecha" : Convert.ToDateTime(reader[2]).ToString("dd/MM/yyyy"),
                        HoraInicio = reader[3] == DBNull.Value ? "" : ((TimeSpan)reader[3]).ToString(@"hh\:mm"),
                        HoraFin = reader[4] == DBNull.Value ? "" : ((TimeSpan)reader[4]).ToString(@"hh\:mm"),
                        Sucursal = reader.GetString(5), Cupos = reader.GetInt32(6)
                    });
            }
            return View(datos);
        }

        [HttpGet]
        public IActionResult Formulario(string seccion = "Entrenadores", int? id = null)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            var modelo = new FormularioOfertaViewModel { Seccion = Normalizar(seccion) };
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                if (id.HasValue)
                {
                    string sql = modelo.Seccion == "Entrenadores"
                        ? "SELECT id_entrenador, nombre, especialidad, email, certificaciones, id_sucursal FROM Entrenador WHERE id_entrenador=@Id"
                        : modelo.Seccion == "Clases"
                        ? "SELECT id_clase, nombre, tipo, duracion, capacidad_max, id_sucursal FROM Clase WHERE id_clase=@Id"
                        : "SELECT id_horario, id_clase, id_entrenador, dia_fecha, hora_inicio, hora_fin FROM Horario WHERE id_horario=@Id";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return NotFound();
                            modelo.Id = id.Value;
                            if (modelo.Seccion == "Entrenadores")
                            {
                                modelo.Nombre = reader[1]?.ToString() ?? ""; modelo.Especialidad = reader[2]?.ToString() ?? "";
                                modelo.Email = reader[3]?.ToString() ?? ""; modelo.Certificaciones = reader[4]?.ToString() ?? "";
                                modelo.IdSucursal = reader[5] == DBNull.Value ? 0 : Convert.ToInt32(reader[5]);
                            }
                            else if (modelo.Seccion == "Clases")
                            {
                                modelo.Nombre = reader[1]?.ToString() ?? ""; modelo.Tipo = reader[2]?.ToString() ?? "";
                                int.TryParse((reader[3]?.ToString() ?? "").Replace(" min", ""), out int duracion);
                                modelo.DuracionMinutos = duracion; modelo.Cupos = reader[4] == DBNull.Value ? 0 : Convert.ToInt32(reader[4]);
                                modelo.IdSucursal = reader[5] == DBNull.Value ? 0 : Convert.ToInt32(reader[5]);
                            }
                            else
                            {
                                modelo.IdClase = reader[1] == DBNull.Value ? 0 : Convert.ToInt32(reader[1]);
                                modelo.IdEntrenador = reader[2] == DBNull.Value ? 0 : Convert.ToInt32(reader[2]);
                                modelo.Fecha = reader[3] == DBNull.Value ? DateTime.Today.AddDays(1) : Convert.ToDateTime(reader[3]);
                                modelo.HoraInicio = reader[4] == DBNull.Value ? "" : ((TimeSpan)reader[4]).ToString(@"hh\:mm");
                                modelo.HoraFin = reader[5] == DBNull.Value ? "" : ((TimeSpan)reader[5]).ToString(@"hh\:mm");
                            }
                        }
                    }
                }
                CargarCombos(con);
            }
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Formulario(FormularioOfertaViewModel modelo)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            modelo.Seccion = Normalizar(modelo.Seccion);
            if (modelo.Seccion != "Horarios" && string.IsNullOrWhiteSpace(modelo.Nombre)) ModelState.AddModelError(nameof(modelo.Nombre), "El nombre es obligatorio");
            if (modelo.Seccion == "Entrenadores" && modelo.IdSucursal <= 0) ModelState.AddModelError(nameof(modelo.IdSucursal), "Selecciona una sucursal");
            if (modelo.Seccion == "Clases" && (modelo.IdSucursal <= 0 || modelo.Cupos <= 0 || modelo.DuracionMinutos <= 0))
                ModelState.AddModelError("", "Indica sucursal, capacidad y duración válidas");
            if (modelo.Seccion == "Horarios")
            {
                if (modelo.IdClase <= 0 || modelo.IdEntrenador <= 0) ModelState.AddModelError("", "Selecciona clase y entrenador");
                if (modelo.Fecha.Date < DateTime.Today) ModelState.AddModelError(nameof(modelo.Fecha), "La fecha no puede ser pasada");
                if (!TimeSpan.TryParse(modelo.HoraInicio, out TimeSpan inicio) || !TimeSpan.TryParse(modelo.HoraFin, out TimeSpan fin) || fin <= inicio)
                    ModelState.AddModelError("", "La hora de fin debe ser posterior a la de inicio");
            }
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                if (!ModelState.IsValid) { CargarCombos(con); return View(modelo); }
                string sql;
                if (modelo.Seccion == "Entrenadores")
                    sql = modelo.Id.HasValue
                        ? "UPDATE Entrenador SET nombre=@Nombre, especialidad=@Especialidad, email=@Email, certificaciones=@Certificaciones, id_sucursal=@Sucursal WHERE id_entrenador=@Id"
                        : "INSERT INTO Entrenador (nombre, especialidad, email, certificaciones, id_sucursal) VALUES (@Nombre, @Especialidad, @Email, @Certificaciones, @Sucursal)";
                else if (modelo.Seccion == "Clases")
                    sql = modelo.Id.HasValue
                        ? "UPDATE Clase SET nombre=@Nombre, tipo=@Tipo, duracion=@Duracion, capacidad_max=@Cupos, id_sucursal=@Sucursal WHERE id_clase=@Id"
                        : "INSERT INTO Clase (nombre, tipo, duracion, capacidad_max, id_sucursal) VALUES (@Nombre, @Tipo, @Duracion, @Cupos, @Sucursal)";
                else
                    sql = modelo.Id.HasValue
                        ? "UPDATE Horario SET id_clase=@Clase, id_entrenador=@Entrenador, dia_fecha=@Fecha, hora_inicio=@Inicio, hora_fin=@Fin WHERE id_horario=@Id"
                        : "INSERT INTO Horario (id_clase, id_entrenador, dia_fecha, hora_inicio, hora_fin) VALUES (@Clase, @Entrenador, @Fecha, @Inicio, @Fin)";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", modelo.Id ?? 0);
                    cmd.Parameters.AddWithValue("@Nombre", modelo.Nombre.Trim());
                    cmd.Parameters.AddWithValue("@Especialidad", modelo.Especialidad.Trim());
                    cmd.Parameters.AddWithValue("@Email", modelo.Email.Trim());
                    cmd.Parameters.AddWithValue("@Certificaciones", modelo.Certificaciones.Trim());
                    cmd.Parameters.AddWithValue("@Sucursal", modelo.IdSucursal);
                    cmd.Parameters.AddWithValue("@Tipo", modelo.Tipo.Trim());
                    cmd.Parameters.AddWithValue("@Duracion", modelo.DuracionMinutos.ToString());
                    cmd.Parameters.AddWithValue("@Cupos", modelo.Cupos);
                    cmd.Parameters.AddWithValue("@Clase", modelo.IdClase);
                    cmd.Parameters.AddWithValue("@Entrenador", modelo.IdEntrenador);
                    cmd.Parameters.AddWithValue("@Fecha", modelo.Fecha.Date);
                    cmd.Parameters.AddWithValue("@Inicio", TimeSpan.TryParse(modelo.HoraInicio, out TimeSpan a) ? a : TimeSpan.Zero);
                    cmd.Parameters.AddWithValue("@Fin", TimeSpan.TryParse(modelo.HoraFin, out TimeSpan b) ? b : TimeSpan.Zero);
                    try { cmd.ExecuteNonQuery(); }
                    catch (SqlException)
                    {
                        ModelState.AddModelError("", "No se pudieron guardar los datos. Comprueba los registros seleccionados.");
                        CargarCombos(con); return View(modelo);
                    }
                }
            }
            return RedirectToAction(nameof(Index), new { seccion = modelo.Seccion });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(string seccion, int id)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            seccion = Normalizar(seccion);
            string sql = seccion == "Entrenadores" ? "DELETE FROM Entrenador WHERE id_entrenador=@Id"
                : seccion == "Clases" ? "DELETE FROM Clase WHERE id_clase=@Id" : "DELETE FROM Horario WHERE id_horario=@Id";
            try
            {
                using (SqlConnection con = conexion.GetConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    { cmd.Parameters.AddWithValue("@Id", id); cmd.ExecuteNonQuery(); }
                }
            }
            catch (SqlException) { TempData["Error"] = "No puedes eliminar un registro que tiene datos relacionados."; }
            return RedirectToAction(nameof(Index), new { seccion });
        }

        private void CargarCombos(SqlConnection con)
        {
            var sucursales = new List<(int Id, string Nombre)>();
            var clases = new List<(int Id, string Nombre)>();
            var entrenadores = new List<(int Id, string Nombre)>();
            using (SqlCommand cmd = new SqlCommand("SELECT id_sucursal, nombre FROM Sucursal ORDER BY nombre", con))
            using (SqlDataReader reader = cmd.ExecuteReader())
                while (reader.Read()) sucursales.Add((reader.GetInt32(0), reader.GetString(1)));
            using (SqlCommand cmd = new SqlCommand("SELECT id_clase, nombre FROM Clase ORDER BY nombre", con))
            using (SqlDataReader reader = cmd.ExecuteReader())
                while (reader.Read()) clases.Add((reader.GetInt32(0), reader.GetString(1)));
            using (SqlCommand cmd = new SqlCommand("SELECT id_entrenador, nombre FROM Entrenador ORDER BY nombre", con))
            using (SqlDataReader reader = cmd.ExecuteReader())
                while (reader.Read()) entrenadores.Add((reader.GetInt32(0), reader.GetString(1)));
            ViewBag.Sucursales = sucursales; ViewBag.Clases = clases; ViewBag.Entrenadores = entrenadores;
        }
        private static string Normalizar(string? s) => s == "Clases" || s == "Horarios" ? s : "Entrenadores";
    }
}
