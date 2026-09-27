namespace Sentinel.Web.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        // Foreign Key to Project
        public int ProjectId { get; set; }
        public Project? Project { get; set; }
    }
}