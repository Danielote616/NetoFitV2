
using System;
using System.Collections.Generic;

namespace Vista1Danielote.Models
{
    public class EntrenadorPanelModelo
    {
        public string Nombre { get; set; } = "";
        public string Especialidad { get; set; } = "";
        public string Certificaciones { get; set; } = "";
        public string Sucursal { get; set; } = "";

        public List<EntrenadorClaseModelo> Clases { get; set; } = new();
        public List<EntrenadorAlumnoModelo> Alumnos { get; set; } = new();
        public List<EntrenadorRegistroModelo> Rutinas { get; set; } = new();
        public List<EntrenadorRegistroModelo> Evaluaciones { get; set; } = new();
    }

    public class EntrenadorClaseModelo
    {
        public string Nombre { get; set; } = "";
        public DateTime Fecha { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public string Sucursal { get; set; } = "";
        public int Reservas { get; set; }
        public int Capacidad { get; set; }
    }

    public class EntrenadorAlumnoModelo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Correo { get; set; } = "";
    }

    public class EntrenadorRegistroModelo
    {
        public string Cliente { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string Detalle { get; set; } = "";
    }
}
