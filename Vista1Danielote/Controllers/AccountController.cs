
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Vista1Danielote.DAL;
using Vista1Danielote.Models;

namespace Vista1Danielote.Controllers
{
    public class AccountController : Controller
    {
        private readonly Conexion conexion = new Conexion();

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string correo = model.Email.Trim().ToLowerInvariant();

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sqlAdmin = @"SELECT id_admin, nombre, ruta_imagen
                                    FROM Administrador
                                    WHERE email = @Email
                                    AND [contraseña] = @Password";

                using (SqlCommand cmd = new SqlCommand(sqlAdmin, con))
                {
                    cmd.Parameters.AddWithValue("@Email", correo);
                    cmd.Parameters.AddWithValue("@Password", model.Password);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int idAdmin = Convert.ToInt32(reader["id_admin"]);

                            HttpContext.Session.Clear();
                            HttpContext.Session.SetString("Usuario", correo);
                            HttpContext.Session.SetInt32("Rol", 1);
                            HttpContext.Session.SetInt32("IdAdmin", idAdmin);

                            HttpContext.Session.SetString(
                                "NombreUsuario",
                                reader["nombre"].ToString() ?? "Administrador"
                            );

                            HttpContext.Session.SetString(
                                "RutaImagenAdmin",
                                reader["ruta_imagen"] == DBNull.Value
                                    ? ""
                                    : reader["ruta_imagen"].ToString() ?? ""
                            );

                            return RedirectToAction("Dashboard", "Admin");
                        }
                    }
                }

                string sqlCliente = @"SELECT id_cliente, nombre
                                      FROM Cliente
                                      WHERE email = @Email
                                      AND [contraseña] = @Password";

                using (SqlCommand cmd = new SqlCommand(sqlCliente, con))
                {
                    cmd.Parameters.AddWithValue("@Email", correo);
                    cmd.Parameters.AddWithValue("@Password", model.Password);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int idCliente = Convert.ToInt32(reader["id_cliente"]);

                            HttpContext.Session.Clear();
                            HttpContext.Session.SetString("Usuario", correo);
                            HttpContext.Session.SetInt32("Rol", 2);
                            HttpContext.Session.SetInt32("IdCliente", idCliente);

                            HttpContext.Session.SetString(
                                "NombreUsuario",
                                reader["nombre"].ToString() ?? "Cliente"
                            );

                            return RedirectToAction("Index", "Home");
                        }
                    }
                }

                string sqlEntrenador = @"SELECT id_entrenador, nombre
                                        FROM Entrenador
                                        WHERE email = @Email
                                        AND [contraseña] = @Password";

                using (SqlCommand cmd = new SqlCommand(sqlEntrenador, con))
                {
                    cmd.Parameters.AddWithValue("@Email", correo);
                    cmd.Parameters.AddWithValue("@Password", model.Password);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int idEntrenador =
                                Convert.ToInt32(reader["id_entrenador"]);

                            HttpContext.Session.Clear();
                            HttpContext.Session.SetString("Usuario", correo);
                            HttpContext.Session.SetInt32("Rol", 3);
                            HttpContext.Session.SetInt32(
                                "IdEntrenador",
                                idEntrenador
                            );

                            HttpContext.Session.SetString(
                                "NombreUsuario",
                                reader["nombre"].ToString() ?? "Entrenador"
                            );

                            return RedirectToAction("Index", "Entrenador");
                        }
                    }
                }
            }

            ModelState.AddModelError(
                "",
                "Correo o contraseña incorrectos"
            );

            return View(model);
        }

        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Registro(RegistroViewModel model)
        {
            string correo =
                (model.Email ?? "").Trim().ToLowerInvariant();

            if (model.FechaNacimiento.HasValue)
            {
                DateTime hoy = DateTime.Today;
                DateTime fecha = model.FechaNacimiento.Value;

                if (fecha > hoy)
                {
                    ModelState.AddModelError(
                        nameof(model.FechaNacimiento),
                        "La fecha no puede ser futura"
                    );
                }
                else
                {
                    int edad = hoy.Year - fecha.Year;

                    if (fecha > hoy.AddYears(-edad))
                    {
                        edad--;
                    }

                    if (edad < 14)
                    {
                        ModelState.AddModelError(
                            nameof(model.FechaNacimiento),
                            "Debes tener al menos 14 años"
                        );
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();

                string sqlVerificar = @"
                    SELECT CASE
                        WHEN EXISTS (
                            SELECT 1 FROM Cliente
                            WHERE email = @Email
                        )
                        OR EXISTS (
                            SELECT 1 FROM Administrador
                            WHERE email = @Email
                        )
                        OR EXISTS (
                            SELECT 1 FROM Entrenador
                            WHERE email = @Email
                        )
                        THEN 1
                        ELSE 0
                    END";

                using (SqlCommand cmd = new SqlCommand(sqlVerificar, con))
                {
                    cmd.Parameters.AddWithValue("@Email", correo);

                    bool existe;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        existe = reader.Read() &&
                            Convert.ToInt32(reader[0]) == 1;
                    }

                    if (existe)
                    {
                        ModelState.AddModelError(
                            nameof(model.Email),
                            "Este correo ya está registrado"
                        );

                        return View(model);
                    }
                }

                string sqlRegistro = @"
                    INSERT INTO Cliente
                    (nombre, email, [contraseña], telefono, fecha_nacimiento)
                    VALUES
                    (@Nombre, @Email, @Password, @Telefono, @FechaNacimiento)";

                using (SqlCommand cmd = new SqlCommand(sqlRegistro, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@Nombre",
                        model.Nombre.Trim()
                    );

                    cmd.Parameters.AddWithValue("@Email", correo);

                    cmd.Parameters.AddWithValue(
                        "@Password",
                        model.Password
                    );

                    cmd.Parameters.AddWithValue(
                        "@Telefono",
                        model.Telefono
                    );

                    cmd.Parameters.AddWithValue(
                        "@FechaNacimiento",
                        model.FechaNacimiento.Value.Date
                    );

                    cmd.ExecuteNonQuery();
                }

                string sqlCliente = @"SELECT id_cliente
                                      FROM Cliente
                                      WHERE email = @Email";

                using (SqlCommand cmd = new SqlCommand(sqlCliente, con))
                {
                    cmd.Parameters.AddWithValue("@Email", correo);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            ModelState.AddModelError(
                                "",
                                "No se pudo consultar la cuenta registrada"
                            );

                            return View(model);
                        }

                        int idCliente =
                            Convert.ToInt32(reader["id_cliente"]);

                        HttpContext.Session.Clear();
                        HttpContext.Session.SetString("Usuario", correo);
                        HttpContext.Session.SetInt32("Rol", 2);
                        HttpContext.Session.SetInt32("IdCliente", idCliente);

                        HttpContext.Session.SetString(
                            "NombreUsuario",
                            model.Nombre.Trim()
                        );
                    }
                }
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
