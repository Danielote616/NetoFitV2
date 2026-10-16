
using Microsoft.Data.SqlClient;
using Vista1Danielote.Models;
using Vistasdebeltran.Models;

namespace Vista1Danielote.DAL
{
    public static class PreciosPlanDAL
    {
        public static PagosModelo? Cargar(SqlConnection con, int id)
        {
            PagosModelo? plan;

            string sqlPlan = @"SELECT id_plan, nombre, precio, duracion
                               FROM Planes
                               WHERE id_plan = @Id";

            using (SqlCommand cmd = new SqlCommand(sqlPlan, con))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    plan = new PagosModelo
                    {
                        IdPlan = reader.GetInt32(0),
                        NombrePlan = reader.GetString(1),
                        Total = reader.GetDecimal(2),
                        Subtotal = reader.GetDecimal(2),
                        DetallePlan = reader[3]?.ToString() ?? "",
                        NombrePromo = "Sin promoción",
                        DescuentoPromo = 0
                    };
                }
            }

            string sqlPromocion = @"SELECT TOP 1 nombre, descuento
                                    FROM Promocion
                                    WHERE id_plan = @Id
                                    AND @Hoy BETWEEN CAST(fecha_inicio AS DATE)
                                    AND CAST(fecha_fin AS DATE)
                                    AND descuento > 0
                                    AND descuento <= 100
                                    ORDER BY descuento DESC,
                                             fecha_inicio DESC,
                                             id_promocion DESC";

            using (SqlCommand cmd = new SqlCommand(sqlPromocion, con))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Hoy", DateTime.Today);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        decimal porcentaje = Convert.ToDecimal(reader[1]);

                        plan.NombrePromo = reader[0]?.ToString() ?? "Promoción";

                        plan.DescuentoPromo = Math.Round(
                            plan.Subtotal * porcentaje / 100m,
                            2,
                            MidpointRounding.AwayFromZero
                        );

                        plan.Total = plan.Subtotal - plan.DescuentoPromo;
                    }
                }
            }

            return plan;
        }
    }
}
