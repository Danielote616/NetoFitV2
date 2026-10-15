namespace Vista1Danielote.Models
{
    public class SeguimientoFilaModelo
    {
        public int Id { get; set; }
        public int IdRelacionado { get; set; }
        public string Titulo { get; set; } = "";
        public string Subtitulo { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string Detalle { get; set; } = "";
    }
    public class SeguimientoFormularioModelo
    {
        public int Id { get; set; }
        public string Seccion { get; set; } = "Evaluaciones";
        public int IdCliente { get; set; }
        public int IdEntrenador { get; set; }
        public int IdRutina { get; set; }
        public int IdEjercicio { get; set; }
        public int? IdReserva { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Today;
        public decimal Peso { get; set; }
        public decimal Imc { get; set; }
        public decimal PorcentajeGrasa { get; set; }
        public TimeSpan HoraEntrada { get; set; } = new TimeSpan(8, 0, 0);
        public TimeSpan? HoraSalida { get; set; }
        public string Objetivo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Grupo { get; set; } = "";
        public string GrupoMuscular { get; set; } = "";
        public int Series { get; set; }
        public string Repeticiones { get; set; } = "";
    }
}
