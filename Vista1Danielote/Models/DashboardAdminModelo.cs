
namespace Vista1Danielote.Models
{
    public class ReporteSucursalModelo
    {
        public int IdSucursal { get; set; }
        public string Nombre { get; set; } = "";
        public decimal Ingresos { get; set; }
        public int Asistencias { get; set; }
    }

    public class ProximaMembresiaModelo
    {
        public string Cliente { get; set; } = "";
        public string Plan { get; set; } = "";
        public string Sucursal { get; set; } = "";
        public DateTime Fecha { get; set; }
    }

    public class DashboardAdminModelo
    {
        public List<ReporteSucursalModelo> Sucursales
        { get; set; } = new List<ReporteSucursalModelo>();

        public List<ProximaMembresiaModelo> Proximas
        { get; set; } = new List<ProximaMembresiaModelo>();
    }
}
