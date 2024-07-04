using Microsoft.AspNetCore.Mvc;
using Npgsql;
using smile.server.Models;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace smile.server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString = "Host=95.165.153.126;Port=5432;Database=jmix_bd_smile;Username=jmix_bd_smile_administrator;Password=UfI#6_HhOG21hjH+j@JmA_KFFGkDdsaygh_*F9uhgfio^9123";
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Login_Users) || string.IsNullOrEmpty(user.Hash_Password_Users))
            {
                _logger.LogWarning("Invalid login data received.");
                return BadRequest("Login data is invalid.");
            }

            _logger.LogInformation("Starting user login process.");

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if user exists with the given login and password
                await using (var command = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE login_users = @login AND hash_password_users = @password", connection))
                {
                    command.Parameters.AddWithValue("login", user.Login_Users);
                    command.Parameters.AddWithValue("password", user.Hash_Password_Users);
                    var userCount = (long)await command.ExecuteScalarAsync();
                    if (userCount == 0)
                    {
                        _logger.LogWarning("User with login {Login} not found or password is incorrect.", user.Login_Users);
                        return Unauthorized("Invalid login or password.");
                    }
                }

                _logger.LogInformation("User {Login} logged in successfully.", user.Login_Users);
                return Ok(new { Message = "Login successful." });
            }
            catch (PostgresException ex)
            {
                _logger.LogError(ex, "PostgresException occurred while logging in the user.");
                return StatusCode(500, "PostgresException: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while logging in the user.");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}