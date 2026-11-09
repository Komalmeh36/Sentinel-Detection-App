
namespace Sentinel.Web.Models
{
    public class DashboardViewModel
    {
        public int TotalProjects { get; set; }
        public int TotalServices { get; set; }
        public int TotalErrors { get; set; }
        public int TotalSecurityThreats { get; set; }

        public Dictionary<string, int> SeverityCounts { get; set; } = new();
        public Dictionary<string, int> StatusCounts { get; set; } = new();

        public List<AuditLog> RecentActivities { get; set; } = new();

        public List<EventTrendPoint> EventTrends { get; set; } = new();
    }

    public class EventTrendPoint
    {
        public string Day { get; set; } = string.Empty;
        public int Errors { get; set; }
        public int SecurityThreats { get; set; }
    }
}
