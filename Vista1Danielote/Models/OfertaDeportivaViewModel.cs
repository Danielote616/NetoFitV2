
using System.Collections.Generic;

namespace Vista1Danielote.Models
{
    public class EntrenadorOferta
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Especialidad { get; set; } = "";
        public string Sucursal { get; set; } = "";
        public int ClasesPorSemana { get; set; }
        public string Iniciales { get; set; } = "";
        public string ColorAvatar { get; set; } = "";
    }

    public class ClaseOferta
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Entrenador { get; set; } = "";
        public string Sucursal { get; set; } = "";
        public int DuracionMinutos { get; set; }
    }

    public class HorarioOferta
    {
        public int Id { get; set; }
        public string Clase { get; set; } = "";
        public string Dia { get; set; } = "";
        public string HoraInicio { get; set; } = "";
        public string HoraFin { get; set; } = "";
        public string Sucursal { get; set; } = "";
        public int Cupos { get; set; }
    }

    public class OfertaDeportivaViewModel
    {
        public string Seccion { get; set; } = "Entrenadores";

        public List<EntrenadorOferta> Entrenadores { get; set; }
            = new List<EntrenadorOferta>();

        public List<ClaseOferta> Clases { get; set; }
            = new List<ClaseOferta>();

        public List<HorarioOferta> Horarios { get; set; }
            = new List<HorarioOferta>();
    }

    public class FormularioOfertaViewModel
    {
        public int? Id { get; set; }

        public string Seccion { get; set; } = "Entrenadores";

        public string Nombre { get; set; } = "";

        public string Especialidad { get; set; } = "";

        public string Sucursal { get; set; } = "";

        public int ClasesPorSemana { get; set; }

        public string Tipo { get; set; } = "";

        public string Entrenador { get; set; } = "";

        public int DuracionMinutos { get; set; }

        public string Dia { get; set; } = "";

        public string HoraInicio { get; set; } = "";

        public string HoraFin { get; set; } = "";

        public int Cupos { get; set; }

        public int IdSucursal { get; set; }

        public int IdClase { get; set; }

        public int IdEntrenador { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Today.AddDays(1);

        public string Email { get; set; } = "";

        public string Certificaciones { get; set; } = "";

        public string? Contrasena { get; set; }
    }
}
