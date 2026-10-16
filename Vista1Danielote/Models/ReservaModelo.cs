
namespace Vista1Danielote.Models
{
    public class ReservaModelo
    {
        public int IdClase { get; set; }

        public int IdReserva { get; set; }

        public DateTime Fecha { get; set; }

        public string Estado { get; set; } = "Confirmada";

        public ClaseHorarioModelo Clase { get; set; } = new ClaseHorarioModelo();
    }
}
