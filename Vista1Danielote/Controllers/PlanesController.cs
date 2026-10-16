
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers
{
    public class PlanesController : Controller
    {
        private readonly Conexion conexion = new Conexion();

        private bool EsAdmin()
        {
            return HttpContext.Session.GetInt32("Rol") == 1;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            List<PlanAdminModelo> planes =
                new List<PlanAdminModelo>();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql = @"
                    SELECT id_plan, nombre, precio,
                           duracion, beneficios
                    FROM Planes
                    ORDER BY id_plan";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            PlanAdminModelo plan = new PlanAdminModelo();

                            plan.Id = reader.GetInt32(0);
                            plan.Nombre = reader.GetString(1);
                            plan.Precio = reader.GetDecimal(2);

                            plan.Duracion = reader[3]?.ToString() ?? "";
                            plan.Beneficios = reader[4]?.ToString() ?? "";

                            planes.Add(plan);
                        }
                    }
                }
            }

            return View(planes);
        }

        [HttpGet]
        public IActionResult Formulario(int? id)
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            PlanAdminModelo modelo = new PlanAdminModelo();

            if (id.HasValue)
            {
                using (SqlConnection con = conexion.GetConnection())
                {
                    con.Open();

                    string sql = @"
                        SELECT id_plan, nombre, precio,
                               duracion, beneficios
                        FROM Planes
                        WHERE id_plan = @Id";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id.Value);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return NotFound();
                            }

                            modelo.Id = reader.GetInt32(0);
                            modelo.Nombre = reader.GetString(1);
                            modelo.Precio = reader.GetDecimal(2);

                            modelo.Duracion =
                                reader[3]?.ToString() ?? "";

                            modelo.Beneficios =
                                reader[4]?.ToString() ?? "";
                        }
                    }
                }
            }

            return View(modelo);
        }

        [HttpPost]
        
        public IActionResult Formulario(PlanAdminModelo modelo)
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(modelo.Duracion))
            {
                ModelState.AddModelError(
                    "Duracion",
                    "Debes indicar una duración."
                );
            }
            else
            {
                DateTime? vencimiento = DuracionPlan.CalcularFin(
                    DateTime.Today,
                    modelo.Duracion
                );

                if (!vencimiento.HasValue)
                {
                    ModelState.AddModelError(
                        "Duracion",
                        "Duración no válida. Usa Mensual, Trimestral, Semestral, Anual o una cantidad como 3 meses."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sql;

                if (modelo.Id == 0)
                {
                    sql = @"
                        INSERT INTO Planes
                        (nombre, precio, duracion, beneficios)
                        VALUES
                        (@Nombre, @Precio, @Duracion, @Beneficios)";
                }
                else
                {
                    sql = @"
                        UPDATE Planes
                        SET nombre = @Nombre,
                            precio = @Precio,
                            duracion = @Duracion,
                            beneficios = @Beneficios
                        WHERE id_plan = @Id";
                }

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", modelo.Id);

                    cmd.Parameters.AddWithValue(
                        "@Nombre", modelo.Nombre.Trim());

                    cmd.Parameters.AddWithValue(
                        "@Precio", modelo.Precio);

                    cmd.Parameters.AddWithValue(
                        "@Duracion", modelo.Duracion.Trim());

                    cmd.Parameters.AddWithValue(
                        "@Beneficios", modelo.Beneficios.Trim());

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        
        public IActionResult Eliminar(int id)
        {
            if (!EsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                using (SqlConnection con = conexion.GetConnection())
                {
                    con.Open();

                    string sql = @"
                        DELETE FROM Planes
                        WHERE id_plan = @Id";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException)
            {
                TempData["Error"] =
                    "No se puede eliminar un plan que tiene membresías o promociones asociadas.";
            }

            return RedirectToAction("Index");
        }
    }
}
