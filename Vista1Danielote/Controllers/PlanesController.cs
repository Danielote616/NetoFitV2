using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers
{
    public class PlanesController : Controller
    {
        private readonly Conexion conexion = new Conexion();
        private bool EsAdmin() => HttpContext.Session.GetInt32("Rol") == 1;

        [HttpGet]
        public IActionResult Index()
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            var planes = new List<PlanAdminModelo>();
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT id_plan, nombre, precio, duracion, beneficios FROM Planes ORDER BY id_plan", con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read()) planes.Add(new PlanAdminModelo
                    {
                        Id = reader.GetInt32(0), Nombre = reader.GetString(1), Precio = reader.GetDecimal(2),
                        Duracion = reader[3]?.ToString() ?? "", Beneficios = reader[4]?.ToString() ?? ""
                    });
            }
            return View(planes);
        }

        [HttpGet]
        public IActionResult Formulario(int? id)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            var modelo = new PlanAdminModelo();
            if (id.HasValue)
            {
                using (SqlConnection con = conexion.GetConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT id_plan, nombre, precio, duracion, beneficios FROM Planes WHERE id_plan=@Id", con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return NotFound();
                            modelo.Id = reader.GetInt32(0); modelo.Nombre = reader.GetString(1);
                            modelo.Precio = reader.GetDecimal(2); modelo.Duracion = reader[3]?.ToString() ?? "";
                            modelo.Beneficios = reader[4]?.ToString() ?? "";
                        }
                    }
                }
            }
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Formulario(PlanAdminModelo modelo)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(modelo);
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                string sql = modelo.Id == 0
                    ? "INSERT INTO Planes (nombre, precio, duracion, beneficios) VALUES (@Nombre, @Precio, @Duracion, @Beneficios)"
                    : "UPDATE Planes SET nombre=@Nombre, precio=@Precio, duracion=@Duracion, beneficios=@Beneficios WHERE id_plan=@Id";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", modelo.Id); cmd.Parameters.AddWithValue("@Nombre", modelo.Nombre.Trim());
                    cmd.Parameters.AddWithValue("@Precio", modelo.Precio); cmd.Parameters.AddWithValue("@Duracion", modelo.Duracion.Trim());
                    cmd.Parameters.AddWithValue("@Beneficios", modelo.Beneficios.Trim()); cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");
            try
            {
                using (SqlConnection con = conexion.GetConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Planes WHERE id_plan=@Id", con))
                    { cmd.Parameters.AddWithValue("@Id", id); cmd.ExecuteNonQuery(); }
                }
            }
            catch (SqlException) { TempData["Error"] = "Este plan tiene membresías o promociones asociadas y no se puede eliminar."; }
            return RedirectToAction(nameof(Index));
        }
    }
}
