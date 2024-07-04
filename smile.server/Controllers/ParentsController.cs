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
    public class ParentsController : ControllerBase
    {
        private readonly string _connectionString = "Host=95.165.153.126;Port=5432;Database=jmix_bd_smile;Username=jmix_bd_smile_administrator;Password=UfI#6_HhOG21hjH+j@JmA_KFFGkDdsaygh_*F9uhgfio^9123";
        private readonly ILogger<ParentsController> _logger;

        public ParentsController(ILogger<ParentsController> logger)
        {
            _logger = logger;
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateParentDetails([FromBody] ParentDetails parentDetails)
        {
            if (parentDetails == null || string.IsNullOrEmpty(parentDetails.Email_Parents) || string.IsNullOrEmpty(parentDetails.Telephone_Number_Parents))
            {
                _logger.LogWarning("Invalid parent data received.");
                return BadRequest("Parent data is invalid.");
            }

            _logger.LogInformation("Starting parent details update process.");

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Update parent details
                await using (var command = new NpgsqlCommand("UPDATE parents SET telephone_number_parents = @telephone_number_parents, email_parents = @email_parents WHERE users_id = (SELECT id_users FROM users WHERE login_users = @login_users)", connection))
                {
                    command.Parameters.AddWithValue("telephone_number_parents", parentDetails.Telephone_Number_Parents);
                    command.Parameters.AddWithValue("email_parents", parentDetails.Email_Parents);
                    command.Parameters.AddWithValue("login_users", parentDetails.Login_Users);

                    var result = await command.ExecuteNonQueryAsync();
                    _logger.LogInformation("Parent details updated successfully for user {Login}.", parentDetails.Login_Users);

                    if (result == 0)
                    {
                        _logger.LogError("Failed to update parent details in the database.");
                        return StatusCode(500, "Failed to update parent details.");
                    }
                }

                return Ok(parentDetails);
            }
            catch (PostgresException ex)
            {
                _logger.LogError(ex, "PostgresException occurred while updating parent details.");
                return StatusCode(500, "PostgresException: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating parent details.");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}