using Microsoft.Data.SqlClient;
using Vista1Danielote.Models;

namespace Vista1Danielote.DAL;

public class ClientesDAL
{
    private readonly Conexion conexion = new Conexion();

    public List<ClienteModelo> ObtenerClientes()
    {
        return ConsultarClientes(null);
    }

    public ClienteModelo? ObtenerPorId(int id)
    {
        List<ClienteModelo> clientes = ConsultarClientes(id);
        if (clientes.Count == 0)
            return null;
        return clientes[0];
    }

    private List<ClienteModelo> ConsultarClientes(int? id)
    {
        List<ClienteModelo> clientes = new List<ClienteModelo>();
        string sql = @"
            SELECT c.id_cliente, c.nombre, c.email, c.telefono,
                   m.fecha_fin, m.estado,
                   p.nombre AS plan_nombre, s.nombre AS sucursal_nombre
            FROM Cliente c
            OUTER APPLY (
                SELECT TOP 1 mm.fecha_fin, mm.estado, mm.id_plan, mm.id_sucursal
                FROM Membresias mm
                WHERE mm.id_cliente = c.id_cliente
                ORDER BY mm.fecha_fin DESC, mm.id_membresia DESC
            ) m
            LEFT JOIN Planes p ON m.id_plan = p.id_plan
            LEFT JOIN Sucursal s ON m.id_sucursal = s.id_sucursal
            WHERE (@IdCliente IS NULL OR c.id_cliente = @IdCliente)
            ORDER BY c.nombre";

        using (SqlConnection con = conexion.GetConnection())
        {
            con.Open();
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@IdCliente", id.HasValue ? (object)id.Value : DBNull.Value);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bool tieneMembresia = reader["fecha_fin"] != DBNull.Value;
                        DateTime vencimiento = tieneMembresia ? Convert.ToDateTime(reader["fecha_fin"]) : DateTime.MinValue;
                        string estado = "Sin membresía";
                        if (tieneMembresia)
                        {
                            if (vencimiento.Date < DateTime.Today)
                                estado = "Vencida";
                            else if (vencimiento.Date <= DateTime.Today.AddDays(7))
                                estado = "Por vencer";
                            else
                                estado = "Activa";
                        }

                        clientes.Add(new ClienteModelo
                        {
                            Id = Convert.ToInt32(reader["id_cliente"]),
                            Nombre = reader["nombre"].ToString() ?? "",
                            Correo = reader["email"].ToString() ?? "",
                            Telefono = reader["telefono"] == DBNull.Value ? "Sin teléfono" : reader["telefono"].ToString() ?? "",
                            Plan = reader["plan_nombre"] == DBNull.Value ? "Sin plan" : reader["plan_nombre"].ToString() ?? "",
                            Sucursal = reader["sucursal_nombre"] == DBNull.Value ? "Sin asignar" : reader["sucursal_nombre"].ToString() ?? "",
                            FechaVencimiento = vencimiento,
                            Estado = estado
                        });
                    }
                }
            }
        }
        return clientes;
    }
}
