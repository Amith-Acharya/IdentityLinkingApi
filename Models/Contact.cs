namespace IdentityLinkingApi.Models
{
    public class Contact
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int? LinkedId { get; set; }
        public string LinkPrecedence { get; set; } = "primary"; // "primary" or "secondary"
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
