
namespace Vista1Danielote.Models
{
    public static class DuracionPlan
    {
        public static DateTime? CalcularFin(DateTime inicio, string? duracion)
        {
            string texto = (duracion ?? "").Trim().ToLower();

            int meses = 0;
            int dias = 0;

            if (texto == "mensual" || texto == "mes")
            {
                meses = 1;
            }
            else if (texto == "bimestral")
            {
                meses = 2;
            }
            else if (texto == "trimestral")
            {
                meses = 3;
            }
            else if (texto == "cuatrimestral")
            {
                meses = 4;
            }
            else if (texto == "semestral")
            {
                meses = 6;
            }
            else if (texto == "anual" || texto == "año" || texto == "ano")
            {
                meses = 12;
            }
            else if (texto == "semanal")
            {
                dias = 7;
            }
            else if (texto == "quincenal")
            {
                dias = 15;
            }
            else
            {
                string[] partes = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (partes.Length != 2)
                {
                    return null;
                }

                int cantidad;

                if (!int.TryParse(partes[0], out cantidad) || cantidad <= 0)
                {
                    return null;
                }

                if (partes[1] == "mes" || partes[1] == "meses")
                {
                    if (cantidad > 120) return null;
                    meses = cantidad;
                }
                else if (partes[1] == "año" || partes[1] == "años" ||
                         partes[1] == "ano" || partes[1] == "anos")
                {
                    if (cantidad > 10) return null;
                    meses = cantidad * 12;
                }
                else if (partes[1] == "semana" || partes[1] == "semanas")
                {
                    if (cantidad > 520) return null;
                    dias = cantidad * 7;
                }
                else if (partes[1] == "día" || partes[1] == "días" ||
                         partes[1] == "dia" || partes[1] == "dias")
                {
                    if (cantidad > 3650) return null;
                    dias = cantidad;
                }
                else
                {
                    return null;
                }
            }

            if (meses > 0)
            {
                return inicio.Date.AddMonths(meses);
            }

            if (dias > 0)
            {
                return inicio.Date.AddDays(dias);
            }

            return null;
        }
    }
}
