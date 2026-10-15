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

    public string Iniciales
    {
        get
        {
            string[] partes = Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string letras = "";
            for (int i = 0; i < partes.Length && i < 2; i++)
            {
                if (partes[i].Length > 0) { letras += char.ToUpperInvariant(partes[i][0]); }
            }
            return letras;
        }
    }
}
