namespace smile.server.Models
{
    public class Message
    {
        public int SenderId { get; set; }
        public int RecipientId { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
    }
}