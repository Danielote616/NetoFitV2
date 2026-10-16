
using System.ComponentModel.DataAnnotations;

namespace Vistasdebeltran.Models
{
    public class PagosModelo
    {
        public int IdPlan { get; set; }

        public string TipoOperacion { get; set; } = "nuevo";

        [Range(1, int.MaxValue, ErrorMessage = "Selecciona una sucursal")]
        public int IdSucursal { get; set; }

        public string Nombre { get; set; } = "";

        public string Correo { get; set; } = "";

        public string NombrePlan { get; set; } = "";

        public string DetallePlan { get; set; } = "";

        public decimal Subtotal { get; set; }

        public string NombrePromo { get; set; } = "";

        public decimal DescuentoPromo { get; set; }

        public decimal Total { get; set; }

        public string Estado { get; set; } = "Pendiente";

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }
    }
}
