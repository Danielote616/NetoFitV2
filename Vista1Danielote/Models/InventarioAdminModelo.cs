using System.ComponentModel.DataAnnotations;

namespace Vista1Danielote.Models
{
    public class SucursalAdminModelo
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Ingresa el nombre de la sucursal")]
        [StringLength(100)]
        public string Nombre { get; set; } = "";
        [Required(ErrorMessage = "Ingresa la ciudad")]
        [StringLength(100)]
        public string Ciudad { get; set; } = "";
        [Required(ErrorMessage = "Ingresa la dirección")]
        [StringLength(150)]
        public string Direccion { get; set; } = "";
        [Required(ErrorMessage = "Ingresa el teléfono")]
        [StringLength(20)]
        public string Telefono { get; set; } = "";
        [Required(ErrorMessage = "Indica la apertura")]
        [DataType(DataType.Time)]
        public TimeSpan Apertura { get; set; } = new TimeSpan(6, 0, 0);
        [Required(ErrorMessage = "Indica el cierre")]
        [DataType(DataType.Time)]
        public TimeSpan Cierre { get; set; } = new TimeSpan(22, 0, 0);
        public int Miembros { get; set; }
    }

    public class EquipoAdminModelo
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Ingresa el nombre")]
        [StringLength(100)]
        public string Nombre { get; set; } = "";
        [Required(ErrorMessage = "Ingresa el tipo")]
        [StringLength(100)]
        public string Tipo { get; set; } = "";
        [Required(ErrorMessage = "Selecciona el estado")]
        public string Estado { get; set; } = "Operativo";
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona una sucursal")]
        public int IdSucursal { get; set; }
        public string Sucursal { get; set; } = "";
    }
}
