namespace Sentinel.Web.Models
{
    public class EventItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        
        // Event attributes matching your system manual
        public string Type { get; set; } = "Error"; // Error, Warning, Security Threat, etc.
        public string Severity { get; set; } = "Medium"; // Critical, High, Medium, Low
        public string Status { get; set; } = "Open"; // Open, Investigating, Resolved
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key to Service
        public int ServiceId { get; set; }
        public Service? Service { get; set; }
    }
}