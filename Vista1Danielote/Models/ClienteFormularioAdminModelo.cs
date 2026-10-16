
using System.ComponentModel.DataAnnotations;

namespace Vista1Danielote.Models
{
    public class ClienteFormularioAdminModelo
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ingresa el nombre")]
        [StringLength(100)]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "Ingresa el correo")]
        [EmailAddress(ErrorMessage = "Correo inválido")]
        [StringLength(100)]
        public string Correo { get; set; } = "";

        [Required(ErrorMessage = "Ingresa el teléfono")]
        [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "Ingresa ocho dígitos")]
        public string Telefono { get; set; } = "";

        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Indica fecha de nacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        [DataType(DataType.Password)]
        public string? Contrasena { get; set; }

        [Display(Name = "Foto del cliente (opcional)")]
        [StringLength(255)]
        public string? RutaImagen { get; set; }
    }
}
