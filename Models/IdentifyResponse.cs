namespace IdentityLinkingApi.Models
{
    public class IdentifyResponse
    {
        public ContactResult Contact { get; set; } = new ContactResult();
    }

    public class ContactResult
    {
        public int PrimaryContactId { get; set; }
        public List<string> Emails { get; set; } = new List<string>();
        public List<string> PhoneNumbers { get; set; } = new List<string>();
        public List<int> SecondaryContactIds { get; set; } = new List<int>();
    }
}
