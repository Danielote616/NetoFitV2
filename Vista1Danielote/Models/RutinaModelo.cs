namespace Vista1Danielote.Models;

public class RutinaModelo
{
    public string Objetivo { get; set; } = string.Empty;
    public string Entrenador { get; set; } = string.Empty;
    public List<EjercicioRutinaModelo> Ejercicios { get; set; } = new List<EjercicioRutinaModelo>();
}

public class EjercicioRutinaModelo
{
    public string Nombre { get; set; } = string.Empty;
    public int Series { get; set; }
    public string Repeticiones { get; set; } = string.Empty;
    public string Peso { get; set; } = string.Empty;
}
