using System.Collections.Generic;

namespace Vistasdebeltran.Models
{
    public class TransaccionItem
    {
        public string Id { get; set; } = "";
        public int IdPago { get; set; }
        public string Estado { get; set; } = "";
        public string Cliente { get; set; }
        public string Membresia { get; set; }
        public string Metodo { get; set; }
        public string Fecha { get; set; }
        public decimal Monto { get; set; }
    }

    public class PromocionItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string PlanAplicable { get; set; }
        public string Porcentaje { get; set; }
        public string RangoFechas { get; set; }
        public string Estado { get; set; }
    }

    public class AdminPagosModelo
    {
        public string NombreAdmin { get; set; }
        public string RolAdmin { get; set; }
        public List<TransaccionItem> PagosRecientes { get; set; }
        public List<PromocionItem> PromocionesActivas { get; set; }
    }
}