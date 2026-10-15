using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers;

public class ReservaController : Controller
{
    private readonly ClasesDAL clases = new ClasesDAL();
    private readonly Conexion conexion = new Conexion();

    [HttpGet]
    public IActionResult Detalle(int id)
    {
        ClaseHorarioModelo? clase = clases.ObtenerPorId(id);
        if (clase == null)
            return NotFound();

        int? idCliente = HttpContext.Session.GetInt32("IdCliente");
        bool yaReservada = false;
        if (idCliente.HasValue && HttpContext.Session.GetInt32("Rol") == 2)
        {
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                string sql = "SELECT id_reserva FROM Reserva WHERE id_cliente = @IdCliente AND id_horario = @IdHorario AND estado = 'Confirmada'";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@IdCliente", idCliente.Value);
                    cmd.Parameters.AddWithValue("@IdHorario", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                        yaReservada = reader.Read();
                }
            }
        }

        ViewBag.ErrorReserva = TempData["ErrorReserva"] as string;
        ViewBag.YaReservada = yaReservada;
        ViewBag.Ocupados = clase.Ocupados;
        ViewBag.CuposDisponibles = clase.CuposDisponibles;
        ViewBag.PorcentajeOcupacion = clase.PorcentajeOcupacion;
        return View(clase);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Confirmar(int id)
    {
        int? idCliente = HttpContext.Session.GetInt32("IdCliente");
        if (HttpContext.Session.GetInt32("Rol") != 2 || !idCliente.HasValue)
            return RedirectToAction("Login", "Account");

        ClaseHorarioModelo? clase = clases.ObtenerPorId(id);
        if (clase == null)
            return NotFound();

        using (SqlConnection con = conexion.GetConnection())
        {
            con.Open();
            string verificar = "SELECT id_reserva FROM Reserva WHERE id_cliente = @IdCliente AND id_horario = @IdHorario AND estado = 'Confirmada'";
            using (SqlCommand cmd = new SqlCommand(verificar, con))
            {
                cmd.Parameters.AddWithValue("@IdCliente", idCliente.Value);
                cmd.Parameters.AddWithValue("@IdHorario", id);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return RedirectToAction("Confirmada", new { id });
                }
            }

            if (clase.CuposDisponibles <= 0 || clase.ProximaFecha.Date < DateTime.Today)
            {
                TempData["ErrorReserva"] = "Esta clase no tiene cupos disponibles o su fecha ya pasó.";
                return RedirectToAction("Detalle", new { id });
            }

            string insertar = "INSERT INTO Reserva (fecha_reserva, estado, id_cliente, id_horario) VALUES (@Fecha, @Estado, @IdCliente, @IdHorario)";
            using (SqlCommand cmd = new SqlCommand(insertar, con))
            {
                cmd.Parameters.AddWithValue("@Fecha", DateTime.Today);
                cmd.Parameters.AddWithValue("@Estado", "Confirmada");
                cmd.Parameters.AddWithValue("@IdCliente", idCliente.Value);
                cmd.Parameters.AddWithValue("@IdHorario", id);
                cmd.ExecuteNonQuery();
            }
        }
        return RedirectToAction("Confirmada", new { id });
    }

    [HttpGet]
    public IActionResult Confirmada(int id)
    {
        int? idCliente = HttpContext.Session.GetInt32("IdCliente");
        if (HttpContext.Session.GetInt32("Rol") != 2 || !idCliente.HasValue)
            return RedirectToAction("Login", "Account");

        ClaseHorarioModelo? clase = clases.ObtenerPorId(id);
        if (clase == null)
            return NotFound();

        bool existe;
        using (SqlConnection con = conexion.GetConnection())
        {
            con.Open();
            string sql = "SELECT id_reserva FROM Reserva WHERE id_cliente = @IdCliente AND id_horario = @IdHorario AND estado = 'Confirmada'";
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@IdCliente", idCliente.Value);
                cmd.Parameters.AddWithValue("@IdHorario", id);
                using (SqlDataReader reader = cmd.ExecuteReader())
                    existe = reader.Read();
            }
        }
        if (!existe)
            return RedirectToAction("Detalle", new { id });

        ReservaModelo reserva = new ReservaModelo
        {
            IdClase = id,
            Fecha = clase.ProximaFecha,
            Clase = clase
        };
        return View(reserva);
    }
}
