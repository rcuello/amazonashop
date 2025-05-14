
namespace Ecommerce.Application.Models.Email
{
    public class EmailSettings
    {                
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string BaseUrlClient { get; set; } = string.Empty;
    }
}
