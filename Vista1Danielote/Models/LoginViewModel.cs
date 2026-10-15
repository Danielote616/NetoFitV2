using System.ComponentModel.DataAnnotations;

namespace Vista1Danielote.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        [RegularExpression(@"^[A-Za-z0-9._-]+@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)+$", ErrorMessage = "Correo no válido. Solo letras sin acento (a-z), números, punto (.), guion bajo (_) y guion medio (-)")]
        [StringLength(100, ErrorMessage = "El correo no puede superar 100 caracteres")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "La contraseña debe tener entre 4 y 50 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;
    }
}