using Microsoft.AspNetCore.Mvc;
using Npgsql;
using smile.server.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace smile.server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly string _connectionString = "Host=95.165.153.126;Port=5432;Database=jmix_bd_smile;Username=jmix_bd_smile_administrator;Password=UfI#6_HhOG21hjH+j@JmA_KFFGkDdsaygh_*F9uhgfio^9123";
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(ILogger<MessagesController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] Message message)
        {
            if (message == null || string.IsNullOrEmpty(message.Text) || message.SenderId == 0 || message.RecipientId == 0)
            {
                _logger.LogWarning("Invalid message data received.");
                return BadRequest("Message data is invalid.");
            }

            _logger.LogInformation("Starting message sending process.");

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using (var command = new NpgsqlCommand("INSERT INTO messages (sender_id, recipient_id, text, timestamp) VALUES (@senderId, @recipientId, @text, @timestamp)", connection))
                {
                    command.Parameters.AddWithValue("senderId", message.SenderId);
                    command.Parameters.AddWithValue("recipientId", message.RecipientId);
                    command.Parameters.AddWithValue("text", message.Text);
                    command.Parameters.AddWithValue("timestamp", DateTime.UtcNow);

                    var result = await command.ExecuteNonQueryAsync();
                    _logger.LogInformation("Message sent successfully from user {SenderId} to user {RecipientId}.", message.SenderId, message.RecipientId);

                    if (result == 0)
                    {
                        _logger.LogError("Failed to insert message into database.");
                        return StatusCode(500, "Failed to send message.");
                    }
                }

                return Ok(message);
            }
            catch (PostgresException ex)
            {
                _logger.LogError(ex, "PostgresException occurred while sending the message.");
                return StatusCode(500, "PostgresException: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending the message.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("{contactId}")]
        public async Task<IActionResult> GetMessages(int contactId)
        {
            if (contactId == 0)
            {
                _logger.LogWarning("Invalid contact ID received.");
                return BadRequest("Contact ID is invalid.");
            }

            _logger.LogInformation("Starting fetching messages process for contact {ContactId}.", contactId);

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var messages = new List<Message>();

                await using (var command = new NpgsqlCommand("SELECT sender_id, recipient_id, text, timestamp FROM messages WHERE (sender_id = @contactId OR recipient_id = @contactId) ORDER BY timestamp", connection))
                {
                    command.Parameters.AddWithValue("contactId", contactId);

                    await using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var message = new Message
                        {
                            SenderId = reader.GetInt32(0),
                            RecipientId = reader.GetInt32(1),
                            Text = reader.GetString(2),
                            Timestamp = reader.GetDateTime(3)
                        };
                        messages.Add(message);
                    }
                }

                return Ok(messages);
            }
            catch (PostgresException ex)
            {
                _logger.LogError(ex, "PostgresException occurred while fetching messages.");
                return StatusCode(500, "PostgresException: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching messages.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetChatHistory(int userId)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var users = new List<User>();

                await using (var command = new NpgsqlCommand(@"
                    SELECT DISTINCT u.id_users, u.login_users, u.nickname_users, u.profile_picture_url
                    FROM messages m
                    JOIN users u ON (m.sender_id = u.id_users OR m.recipient_id = u.id_users)
                    WHERE (m.sender_id = @userId OR m.recipient_id = @userId) AND u.id_users != @userId
                ", connection))
                {
                    command.Parameters.AddWithValue("userId", userId);

                    await using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var user = new User
                        {
                            Id_Users = reader.GetInt32(0),
                            Login_Users = reader.GetString(1),
                            Nickname_Users = reader.GetString(2),
                            Profile_Picture_Url = reader.IsDBNull(3) ? null : reader.GetString(3)
                        };
                        users.Add(user);
                    }
                }

                return Ok(users);
            }
            catch (PostgresException ex)
            {
                _logger.LogError(ex, "PostgresException occurred while getting chat history.");
                return StatusCode(500, "PostgresException: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting chat history.");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}