using Microsoft.AspNetCore.Mvc;
using Npgsql;
using smile.server.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace smile.server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly string _connectionString = "Host=95.165.153.126;Port=5432;Database=jmix_bd_smile;Username=jmix_bd_smile_administrator;Password=UfI#6_HhOG21hjH+j@JmA_KFFGkDdsaygh_*F9uhgfio^9123";
        private readonly ILogger<UsersController> _logger;

        public UsersController(ILogger<UsersController> logger)
        {
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Login_Users))
            {
                _logger.LogWarning("Invalid user data received.");
                return BadRequest("User data is invalid.");
            }

            _logger.LogInformation("Starting user registration process.");

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if user with the same login already exists
                await using (var checkUserCommand = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE login_users = @login", connection))
                {
                    checkUserCommand.Parameters.AddWithValue("login", user.Login_Users);
                    var userCount = (long)await checkUserCommand.ExecuteScalarAsync();
                    if (userCount > 0)
                    {
                        _logger.LogWarning("User with login {Login} already exists.", user.Login_Users);
                        return BadRequest("User with this login already exists.");
                    }
                }

                user.Nickname_Users = user.Login_Users;
                user.Last_Data_Visit_Users = DateTime.UtcNow;
                user.Status_Profile_Users = true;

                // Insert user and parent
                await using (var command = new NpgsqlCommand("SELECT insert_user_with_parent(@login_users, @password_users, @nickname_users, @telephone_number_users, @last_data_visit::timestamp with time zone, @status_profile_users)", connection))
                {
                    command.Parameters.AddWithValue("login_users", NpgsqlTypes.NpgsqlDbType.Varchar, user.Login_Users);
                    command.Parameters.AddWithValue("password_users", NpgsqlTypes.NpgsqlDbType.Varchar, user.Hash_Password_Users);
                    command.Parameters.AddWithValue("nickname_users", NpgsqlTypes.NpgsqlDbType.Varchar, user.Nickname_Users);
                    command.Parameters.AddWithValue("telephone_number_users", NpgsqlTypes.NpgsqlDbType.Varchar, (object)user.Telephone_Number_Users ?? DBNull.Value);
                    command.Parameters.AddWithValue("last_data_visit", NpgsqlTypes.NpgsqlDbType.TimestampTz, user.Last_Data_Visit_Users.ToUniversalTime());
                    command.Parameters.AddWithValue("status_profile_users", NpgsqlTypes.NpgsqlDbType.Boolean, user.Status_Profile_Users);

                    var result = await command.ExecuteNonQueryAsync();
                    _logger.LogInformation("User {Login} and their parent registered successfully.", user.Login_Users);

                    if (result == 0)
                    {
                        _logger.LogError("Failed to insert user and parent into database.");
                        return StatusCode(500, "Failed to register user and parent.");
                    }
                }

                return Ok(user);
            }
            catch (PostgresException ex)
            {
                _logger.LogError(ex, "PostgresException occurred while registering the user.");
                return StatusCode(500, "PostgresException: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while registering the user.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] User user, [FromForm] IFormFile Profile_Picture)
        {
            if (user == null || string.IsNullOrEmpty(user.Login_Users))
            {
                _logger.LogWarning("Invalid user data received.");
                return BadRequest("User data is invalid.");
            }

            _logger.LogInformation("Starting profile update process for user {Login}.", user.Login_Users);

            string profilePictureUrl = user.Profile_Picture_Url;

            if (Profile_Picture != null)
            {
                var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                var filePath = Path.Combine(imagesPath, Profile_Picture.FileName);
                _logger.LogInformation("Saving profile picture to {FilePath}", filePath);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Profile_Picture.CopyToAsync(stream);
                }

                profilePictureUrl = $"/images/{Profile_Picture.FileName}";
            }

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Update user profile
                await using (var command = new NpgsqlCommand("UPDATE users SET nickname_users = @nickname, profile_picture_url = @profile_picture WHERE login_users = @login", connection))
                {
                    command.Parameters.AddWithValue("nickname", user.Nickname_Users);
                    command.Parameters.AddWithValue("profile_picture", profilePictureUrl);
                    command.Parameters.AddWithValue("login", user.Login_Users);

                    var result = await command.ExecuteNonQueryAsync();
                    _logger.LogInformation("Profile updated successfully for user {Login}.", user.Login_Users);

                    if (result == 0)
                    {
                        _logger.LogError("Failed to update user profile in the database.");
                        return StatusCode(500, "Failed to update profile.");
                    }
                }

                user.Profile_Picture_Url = profilePictureUrl;
                return Ok(user);
            }
            catch (PostgresException ex)
            {
                _logger.LogError(ex, "PostgresException occurred while updating the profile.");
                return StatusCode(500, "PostgresException: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the profile.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                _logger.LogWarning("Search query is empty.");
                return BadRequest("Search query is empty.");
            }

            _logger.LogInformation("Starting search for users with query: {Query}", query);

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var users = new List<User>();

                await using (var command = new NpgsqlCommand("SELECT id_users, login_users, nickname_users, telephone_number_users, profile_picture_url FROM users WHERE nickname_users ILIKE @query OR telephone_number_users ILIKE @query", connection))
                {
                    command.Parameters.AddWithValue("query", $"%{query}%");
                    await using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var user = new User
                        {
                            Id_Users = reader.GetInt32(0),
                            Login_Users = reader.GetString(1),
                            Nickname_Users = reader.GetString(2),
                            Telephone_Number_Users = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Profile_Picture_Url = reader.IsDBNull(4) ? null : reader.GetString(4)
                        };
                        users.Add(user);
                    }
                }

                return Ok(users);
            }
            catch (PostgresException ex)
            {
                _logger.LogError(ex, "PostgresException occurred while searching for users.");
                return StatusCode(500, "PostgresException: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while searching for users.");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}