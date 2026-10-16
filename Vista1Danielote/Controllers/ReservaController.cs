
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers
{
    public class ReservaController : Controller
    {
        private readonly ClasesDAL clases = new ClasesDAL();
        private readonly Conexion conexion = new Conexion();

        [HttpGet]
        public IActionResult Detalle(int id)
        {
            ClaseHorarioModelo? clase = clases.ObtenerPorId(id);

            if (clase == null)
            {
                return NotFound();
            }

            int? idCliente =
                HttpContext.Session.GetInt32("IdCliente");

            bool yaReservada = false;

            if (idCliente.HasValue &&
                HttpContext.Session.GetInt32("Rol") == 2)
            {
                using (SqlConnection con = conexion.GetConnection())
                {
                    con.Open();

                    string sql = @"
                        SELECT id_reserva
                        FROM Reserva
                        WHERE id_cliente = @Cliente
                        AND id_horario = @Horario
                        AND estado = 'Confirmada'";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@Cliente", idCliente.Value);

                        cmd.Parameters.AddWithValue("@Horario", id);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            yaReservada = reader.Read();
                        }
                    }
                }
            }

            ViewBag.YaReservada = yaReservada;
            ViewBag.ErrorReserva = TempData["ErrorReserva"];
            ViewBag.Ocupados = clase.Ocupados;
            ViewBag.CuposDisponibles = clase.CuposDisponibles;
            ViewBag.PorcentajeOcupacion = clase.PorcentajeOcupacion;

            return View(clase);
        }

        [HttpPost]
        
        public IActionResult Confirmar(int id)
        {
            int? idCliente =
                HttpContext.Session.GetInt32("IdCliente");

            if (HttpContext.Session.GetInt32("Rol") != 2 ||
                !idCliente.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                DateTime fechaClase = DateTime.MinValue;
                int capacidad = 0;
                bool existeHorario = false;

                string sqlHorario = @"
                    SELECT h.dia_fecha,
                           h.hora_inicio,
                           c.capacidad_max
                    FROM Horario h
                    INNER JOIN Clase c
                        ON c.id_clase = h.id_clase
                    WHERE h.id_horario = @Horario";

                using (SqlCommand cmd = new SqlCommand(sqlHorario, con))
                {
                    cmd.Parameters.AddWithValue("@Horario", id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            existeHorario = true;

                            DateTime fecha = reader.GetDateTime(0);
                            TimeSpan hora = (TimeSpan)reader[1];

                            fechaClase = fecha.Date.Add(hora);
                            capacidad = Convert.ToInt32(reader[2]);
                        }
                    }
                }

                if (!existeHorario)
                {
                    return NotFound();
                }

                if (fechaClase <= DateTime.Now)
                {
                    TempData["ErrorReserva"] =
                        "Esta clase ya comenzó o su fecha pasó.";

                    return RedirectToAction(
                        "Detalle", new { id });
                }

                string sqlDuplicada = @"
                    SELECT COUNT(*)
                    FROM Reserva
                    WHERE id_cliente = @Cliente
                    AND id_horario = @Horario
                    AND estado = 'Confirmada'";

                using (SqlCommand cmd = new SqlCommand(sqlDuplicada, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Cliente", idCliente.Value);

                    cmd.Parameters.AddWithValue("@Horario", id);

                    int duplicadas = Convert.ToInt32(
                        cmd.ExecuteScalar()
                    );

                    if (duplicadas > 0)
                    {
                        return RedirectToAction(
                            "Confirmada", new { id });
                    }
                }

                string sqlOcupados = @"
                    SELECT COUNT(*)
                    FROM Reserva
                    WHERE id_horario = @Horario
                    AND estado = 'Confirmada'";

                using (SqlCommand cmd = new SqlCommand(sqlOcupados, con))
                {
                    cmd.Parameters.AddWithValue("@Horario", id);

                    int ocupados = Convert.ToInt32(
                        cmd.ExecuteScalar()
                    );

                    if (capacidad <= 0 || ocupados >= capacidad)
                    {
                        TempData["ErrorReserva"] =
                            "No hay cupos disponibles para esta clase.";

                        return RedirectToAction(
                            "Detalle", new { id });
                    }
                }

                string sqlInsertar = @"
                    INSERT INTO Reserva
                    (fecha_reserva, estado, id_cliente, id_horario)
                    VALUES
                    (@Fecha, @Estado, @Cliente, @Horario)";

                using (SqlCommand cmd = new SqlCommand(sqlInsertar, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Fecha", DateTime.Today);

                    cmd.Parameters.AddWithValue(
                        "@Estado", "Confirmada");

                    cmd.Parameters.AddWithValue(
                        "@Cliente", idCliente.Value);

                    cmd.Parameters.AddWithValue("@Horario", id);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException)
                    {
                        TempData["ErrorReserva"] =
                            "No se pudo registrar la reserva.";

                        return RedirectToAction(
                            "Detalle", new { id });
                    }
                }
            }

            return RedirectToAction("Confirmada", new { id });
        }

        [HttpPost]
        
        public IActionResult Cancelar(int id)
        {
            int? idCliente =
                HttpContext.Session.GetInt32("IdCliente");

            if (HttpContext.Session.GetInt32("Rol") != 2 ||
                !idCliente.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                bool existeReserva = false;
                DateTime fechaClase = DateTime.MinValue;

                string sqlConsultar = @"
                    SELECT h.dia_fecha, h.hora_inicio
                    FROM Reserva r
                    INNER JOIN Horario h
                        ON h.id_horario = r.id_horario
                    WHERE r.id_reserva = @Reserva
                    AND r.id_cliente = @Cliente
                    AND r.estado = 'Confirmada'";

                using (SqlCommand cmd = new SqlCommand(sqlConsultar, con))
                {
                    cmd.Parameters.AddWithValue("@Reserva", id);

                    cmd.Parameters.AddWithValue(
                        "@Cliente", idCliente.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            existeReserva = true;

                            DateTime fecha = reader.GetDateTime(0);
                            TimeSpan hora = (TimeSpan)reader[1];

                            fechaClase = fecha.Date.Add(hora);
                        }
                    }
                }

                if (!existeReserva)
                {
                    TempData["ErrorPerfil"] =
                        "La reserva no existe o ya fue cancelada.";

                    return RedirectToAction(
                        "Index",
                        "Perfil",
                        new { seccion = "reservas" }
                    );
                }

                if (fechaClase < DateTime.Now.AddHours(2))
                {
                    TempData["ErrorPerfil"] =
                        "Solo puedes cancelar hasta 2 horas antes de la clase.";

                    return RedirectToAction(
                        "Index",
                        "Perfil",
                        new { seccion = "reservas" }
                    );
                }

                string sqlCancelar = @"
                    UPDATE Reserva
                    SET estado = 'Cancelada'
                    WHERE id_reserva = @Reserva
                    AND id_cliente = @Cliente
                    AND estado = 'Confirmada'";

                using (SqlCommand cmd = new SqlCommand(sqlCancelar, con))
                {
                    cmd.Parameters.AddWithValue("@Reserva", id);

                    cmd.Parameters.AddWithValue(
                        "@Cliente", idCliente.Value);

                    try
                    {
                        int afectados = cmd.ExecuteNonQuery();

                        if (afectados > 0)
                        {
                            TempData["ExitoPerfil"] =
                                "Reserva cancelada correctamente.";
                        }
                        else
                        {
                            TempData["ErrorPerfil"] =
                                "La reserva ya no estaba confirmada.";
                        }
                    }
                    catch (SqlException)
                    {
                        TempData["ErrorPerfil"] =
                            "No se pudo cancelar la reserva.";
                    }
                }
            }

            return RedirectToAction(
                "Index",
                "Perfil",
                new { seccion = "reservas" }
            );
        }

        [HttpGet]
        public IActionResult Confirmada(int id)
        {
            int? idCliente =
                HttpContext.Session.GetInt32("IdCliente");

            if (HttpContext.Session.GetInt32("Rol") != 2 ||
                !idCliente.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            ClaseHorarioModelo? clase = clases.ObtenerPorId(id);

            if (clase == null)
            {
                return NotFound();
            }

            bool existeReserva = false;

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql = @"
                    SELECT id_reserva
                    FROM Reserva
                    WHERE id_cliente = @Cliente
                    AND id_horario = @Horario
                    AND estado = 'Confirmada'";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Cliente", idCliente.Value);

                    cmd.Parameters.AddWithValue("@Horario", id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        existeReserva = reader.Read();
                    }
                }
            }

            if (!existeReserva)
            {
                return RedirectToAction(
                    "Detalle", new { id });
            }

            ReservaModelo reserva = new ReservaModelo();

            reserva.IdClase = id;
            reserva.Fecha = clase.ProximaFecha;
            reserva.Clase = clase;
            reserva.Estado = "Confirmada";

            return View(reserva);
        }
    }
}
