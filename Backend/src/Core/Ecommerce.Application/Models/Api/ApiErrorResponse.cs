
namespace Ecommerce.Application.Models.Api
{
    public class ApiErrorResponse
    {
        public int StatusCode { get; set; }
        public string[] Message { get; set; } = Array.Empty<string>();
        public string? Details { get; set; }
        public string? InnerError { get; set; }
        public string? TraceId { get; set; }
        public List<string> InnerErrors { get; set; }
    }
}
