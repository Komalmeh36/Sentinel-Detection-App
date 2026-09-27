namespace Sentinel.Web.Models
{
    public class Project
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsArchived { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key to Organization
        public int OrganizationId { get; set; }

        public Organization? Organization { get; set; }

        // Projects can have multiple services
        public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}