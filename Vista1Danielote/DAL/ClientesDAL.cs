
using Microsoft.Data.SqlClient;
using Vista1Danielote.Models;

namespace Vista1Danielote.DAL
{
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
            {
                return null;
            }

            return clientes[0];
        }

        private List<ClienteModelo> ConsultarClientes(int? id)
        {
            List<ClienteModelo> clientes =
                new List<ClienteModelo>();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sqlClientes = @"
                    SELECT id_cliente, nombre, email, telefono, ruta_imagen
                    FROM Cliente";

                if (id.HasValue)
                {
                    sqlClientes += " WHERE id_cliente = @Id";
                }

                sqlClientes += " ORDER BY nombre";

                using (SqlCommand cmd =
                    new SqlCommand(sqlClientes, con))
                {
                    if (id.HasValue)
                    {
                        cmd.Parameters.AddWithValue(
                            "@Id", id.Value);
                    }

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ClienteModelo cliente = new ClienteModelo();

                            cliente.Id = reader.GetInt32(0);
                            cliente.Nombre = reader.GetString(1);
                            cliente.Correo = reader.GetString(2);

                            cliente.Telefono = reader[3] == DBNull.Value
                                ? "Sin teléfono"
                                : reader[3]?.ToString() ?? "Sin teléfono";

                            cliente.RutaImagen = reader[4] == DBNull.Value
                                ? null
                                : reader[4].ToString();

                            cliente.Plan = "Sin plan";
                            cliente.Sucursal = "Sin asignar";
                            cliente.Estado = "Sin membresía";
                            cliente.FechaVencimiento = DateTime.MinValue;

                            clientes.Add(cliente);
                        }
                    }
                }

                foreach (ClienteModelo cliente in clientes)
                {
                    string sqlMembresias = @"
                        SELECT m.fecha_inicio,
                               m.fecha_fin,
                               m.estado,
                               p.nombre,
                               s.nombre
                        FROM Membresias m
                        LEFT JOIN Planes p
                            ON p.id_plan = m.id_plan
                        LEFT JOIN Sucursal s
                            ON s.id_sucursal = m.id_sucursal
                        WHERE m.id_cliente = @Cliente
                        ORDER BY m.fecha_fin DESC";

                    using (SqlCommand cmd =
                        new SqlCommand(sqlMembresias, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@Cliente", cliente.Id);

                        int mejorPrioridad = 100;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime inicio =
                                    reader.GetDateTime(0);

                                DateTime fin =
                                    reader.GetDateTime(1);

                                string estadoBD =
                                    reader[2]?.ToString() ?? "";

                                int prioridad = 3;

                                if (estadoBD == "Activa" &&
                                    inicio.Date <= DateTime.Today &&
                                    fin.Date >= DateTime.Today)
                                {
                                    prioridad = 0;
                                }
                                else if (estadoBD == "Pendiente")
                                {
                                    prioridad = 1;
                                }
                                else if (estadoBD == "Activa" &&
                                         inicio.Date > DateTime.Today)
                                {
                                    prioridad = 2;
                                }

                                if (prioridad < mejorPrioridad)
                                {
                                    mejorPrioridad = prioridad;

                                    cliente.FechaVencimiento = fin;

                                    cliente.Plan = reader[3] == DBNull.Value
                                        ? "Sin plan"
                                        : reader[3]?.ToString() ?? "Sin plan";

                                    cliente.Sucursal = reader[4] == DBNull.Value
                                        ? "Sin asignar"
                                        : reader[4]?.ToString() ?? "Sin asignar";

                                    if (estadoBD == "Pendiente")
                                    {
                                        cliente.Estado = "Pendiente";
                                    }
                                    else if (estadoBD == "Activa" &&
                                             inicio.Date > DateTime.Today)
                                    {
                                        cliente.Estado = "Programada";
                                    }
                                    else if (estadoBD == "Activa" &&
                                             fin.Date < DateTime.Today)
                                    {
                                        cliente.Estado = "Vencida";
                                    }
                                    else if (estadoBD == "Activa" &&
                                             fin.Date <= DateTime.Today.AddDays(7))
                                    {
                                        cliente.Estado = "Por vencer";
                                    }
                                    else if (estadoBD == "Activa")
                                    {
                                        cliente.Estado = "Activa";
                                    }
                                    else
                                    {
                                        cliente.Estado = estadoBD;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return clientes;
        }
    }
}
