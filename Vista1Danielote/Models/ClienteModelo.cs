
namespace Vista1Danielote.Models;

public class ClienteModelo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Plan { get; set; } = "";
    public string Sucursal { get; set; } = "";
    public DateTime FechaVencimiento { get; set; }
    public string Estado { get; set; } = "";

    public string? RutaImagen { get; set; }
}
