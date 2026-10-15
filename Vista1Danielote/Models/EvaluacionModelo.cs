namespace Vista1Danielote.Models;

public class EvaluacionModelo
{
    public decimal PesoActual { get; set; }
    public decimal ImcActual { get; set; }
    public decimal GrasaActual { get; set; }
    public decimal CambioPeso { get; set; }
    public decimal CambioImc { get; set; }
    public decimal CambioGrasa { get; set; }
    public int Anio { get; set; }
    public List<RegistroEvaluacionModelo> Historial { get; set; } = new List<RegistroEvaluacionModelo>();
}

public class RegistroEvaluacionModelo
{
    public string Mes { get; set; } = string.Empty;
    public decimal Peso { get; set; }
    public decimal Grasa { get; set; }
}
