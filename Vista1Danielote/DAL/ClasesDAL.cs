using Microsoft.Data.SqlClient;
using Vista1Danielote.Models;

namespace Vista1Danielote.DAL;

public class ClasesDAL
{
    private readonly Conexion conexion = new Conexion();

    public List<ClaseHorarioModelo> ObtenerClases()
    {
        return ConsultarClases(null);
    }

    public ClaseHorarioModelo? ObtenerPorId(int id)
    {
        List<ClaseHorarioModelo> clases = ConsultarClases(id);
        if (clases.Count == 0)
            return null;
        return clases[0];
    }

    private List<ClaseHorarioModelo> ConsultarClases(int? id)
    {
        List<ClaseHorarioModelo> clases = new List<ClaseHorarioModelo>();
        string sql = @"
            SELECT h.id_horario, h.dia_fecha, h.hora_inicio, h.hora_fin,
                   c.nombre, c.tipo, c.capacidad_max, c.duracion,
                   e.nombre AS entrenador_nombre, e.especialidad,
                   s.nombre AS sucursal_nombre,
                   (SELECT COUNT(*) FROM Reserva r
                    WHERE r.id_horario = h.id_horario AND r.estado = 'Confirmada') AS ocupados
            FROM Horario h
            INNER JOIN Clase c ON c.id_clase = h.id_clase
            LEFT JOIN Entrenador e ON e.id_entrenador = h.id_entrenador
            LEFT JOIN Sucursal s ON s.id_sucursal = c.id_sucursal
            WHERE (@IdHorario IS NULL AND h.dia_fecha >= @Hoy OR h.id_horario = @IdHorario)
            ORDER BY h.dia_fecha, h.hora_inicio";

        using (SqlConnection con = conexion.GetConnection())
        {
            con.Open();
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@IdHorario", id.HasValue ? (object)id.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Hoy", DateTime.Today);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["dia_fecha"] == DBNull.Value || reader["hora_inicio"] == DBNull.Value)
                            continue;

                        DateTime fecha = Convert.ToDateTime(reader["dia_fecha"]);
                        TimeSpan inicio = (TimeSpan)reader["hora_inicio"];
                        DateTime hora = fecha.Date.Add(inicio);
                        int duracion = 60;
                        if (reader["hora_fin"] != DBNull.Value)
                        {
                            TimeSpan fin = (TimeSpan)reader["hora_fin"];
                            duracion = (int)(fin - inicio).TotalMinutes;
                            if (duracion <= 0)
                                duracion += 24 * 60;
                        }
                        else if (reader["duracion"] != DBNull.Value)
                        {
                            string texto = reader["duracion"].ToString() ?? "";
                            string numeros = new string(texto.TakeWhile(char.IsDigit).ToArray());
                            if (int.TryParse(numeros, out int minutos) && minutos > 0)
                                duracion = minutos;
                        }

                        string categoria = reader["tipo"] == DBNull.Value ? "General" : reader["tipo"].ToString() ?? "General";
                        string color = "verde";
                        if (categoria.Contains("cardio", StringComparison.OrdinalIgnoreCase)) color = "naranja";
                        else if (categoria.Contains("yoga", StringComparison.OrdinalIgnoreCase) || categoria.Contains("bienestar", StringComparison.OrdinalIgnoreCase)) color = "azul";
                        else if (categoria.Contains("funcional", StringComparison.OrdinalIgnoreCase)) color = "morado";
                        else if (categoria.Contains("ciclismo", StringComparison.OrdinalIgnoreCase)) color = "rosado";

                        clases.Add(new ClaseHorarioModelo
                        {
                            Id = Convert.ToInt32(reader["id_horario"]),
                            Nombre = reader["nombre"].ToString() ?? "Clase",
                            Categoria = categoria,
                            Descripcion = "Clase " + (reader["nombre"].ToString() ?? "de entrenamiento"),
                            DiaSemana = fecha.DayOfWeek,
                            FechaProgramada = fecha,
                            HoraInicio = hora,
                            DuracionMinutos = duracion,
                            Entrenador = reader["entrenador_nombre"] == DBNull.Value ? "Por asignar" : reader["entrenador_nombre"].ToString() ?? "Por asignar",
                            Especialidad = reader["especialidad"] == DBNull.Value ? "" : reader["especialidad"].ToString() ?? "",
                            Sucursal = reader["sucursal_nombre"] == DBNull.Value ? "Sin asignar" : reader["sucursal_nombre"].ToString() ?? "",
                            Capacidad = Convert.ToInt32(reader["capacidad_max"]),
                            Ocupados = Convert.ToInt32(reader["ocupados"]),
                            Color = color
                        });
                    }
                }
            }
        }
        return clases;
    }
}
