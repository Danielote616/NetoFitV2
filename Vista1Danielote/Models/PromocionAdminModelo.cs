using System.ComponentModel.DataAnnotations;
namespace Vistasdebeltran.Models
{
    public class PromocionAdminModelo
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Indica un nombre")]
        public string Nombre { get; set; } = "";
        [Range(1, 100, ErrorMessage = "El descuento debe estar entre 1 y 100")]
        public decimal Descuento { get; set; }
        [DataType(DataType.Date)]
        public DateTime FechaInicio { get; set; } = DateTime.Today;
        [DataType(DataType.Date)]
        public DateTime FechaFin { get; set; } = DateTime.Today.AddMonths(1);
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un plan")]
        public int IdPlan { get; set; }
    }
}
