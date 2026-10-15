using System.ComponentModel.DataAnnotations;
namespace Vista1Danielote.Models
{
    public class PlanAdminModelo
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Ingresa el nombre")]
        public string Nombre { get; set; } = "";
        [Range(0.01, 999999, ErrorMessage = "Indica un precio válido")]
        public decimal Precio { get; set; }
        public string Duracion { get; set; } = "Mensual";
        public string Beneficios { get; set; } = "";
    }
}
