using System.Globalization;

namespace Vista1Danielote.Models;

public class ClaseHorarioModelo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public DayOfWeek DiaSemana { get; set; }
    public DateTime HoraInicio { get; set; }
    public DateTime? FechaProgramada { get; set; }
    public int DuracionMinutos { get; set; }
    public string Entrenador { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public string Sucursal { get; set; } = "";
    public int Capacidad { get; set; }
    public int Ocupados { get; set; }
    public string Color { get; set; } = "";

    public int CuposDisponibles => Math.Max(0, Capacidad - Ocupados);
    public int PorcentajeOcupacion => Capacidad == 0 ? 0 : (int)Math.Round(100.0 * Ocupados / Capacidad);
    public string HoraTexto => HoraInicio.ToString("HH:mm", CultureInfo.InvariantCulture);
    public string RangoHorario => HoraTexto + "-" + HoraInicio.AddMinutes(DuracionMinutos).ToString("HH:mm");
    public DateTime ProximaFecha
    {
        get
        {
            if (FechaProgramada.HasValue) return FechaProgramada.Value;
            DateTime hoy = DateTime.Today;
            int dias = ((int)DiaSemana - (int)hoy.DayOfWeek + 7) % 7;
            if (dias == 0 && DateTime.Now.TimeOfDay > HoraInicio.TimeOfDay) { dias = 7; }
            return hoy.AddDays(dias);
        }
    }
    public string FechaCorta => ProximaFecha.ToString("ddd dd MMM", CultureInfo.GetCultureInfo("es-SV"));
    public string DiaAbreviado
    {
        get
        {
            if (DiaSemana == DayOfWeek.Monday) return "LUN";
            if (DiaSemana == DayOfWeek.Tuesday) return "MAR";
            if (DiaSemana == DayOfWeek.Wednesday) return "MIÉ";
            if (DiaSemana == DayOfWeek.Thursday) return "JUE";
            if (DiaSemana == DayOfWeek.Friday) return "VIE";
            if (DiaSemana == DayOfWeek.Saturday) return "SÁB";
            return "DOM";
        }
    }
}
