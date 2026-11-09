
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentinel.Web.Data;
using Sentinel.Web.Models;

namespace Sentinel.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var activeEvents = _context.EventItems
                .Where(e => e.Service != null &&
                            e.Service.Project != null &&
                            !e.Service.Project.IsArchived);

            // Define the last 7 days using UTC dates.
            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-6);
            var endDate = today.AddDays(1);

            // Retrieve actual event records for the trend chart.
            var trendEvents = await activeEvents
                .Where(e => e.CreatedAt >= startDate &&
                            e.CreatedAt < endDate)
                .Select(e => new
                {
                    e.CreatedAt,
                    e.Type
                })
                .ToListAsync();

            // Create one entry for each of the last 7 days.
            var eventTrends = Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var date = startDate.AddDays(i);

                    return new EventTrendPoint
                    {
                        Day = date.ToString("dd MMM"),
                        Errors = trendEvents.Count(e =>
                            e.CreatedAt.Date == date &&
                            e.Type == "Error"),

                        SecurityThreats = trendEvents.Count(e =>
                            e.CreatedAt.Date == date &&
                            e.Type == "Security Threat")
                    };
                })
                .ToList();

            var dashboard = new DashboardViewModel
            {
                TotalProjects = await _context.Projects
                    .CountAsync(p => !p.IsArchived),

                TotalServices = await _context.Services
                    .CountAsync(s => s.Project != null &&
                                     !s.Project.IsArchived),

                TotalErrors = await activeEvents
                    .CountAsync(e => e.Type == "Error"),

                TotalSecurityThreats = await activeEvents
                    .CountAsync(e => e.Type == "Security Threat"),

                SeverityCounts = new Dictionary<string, int>
                {
                    ["Critical"] = await activeEvents.CountAsync(e => e.Severity == "Critical"),
                    ["High"] = await activeEvents.CountAsync(e => e.Severity == "High"),
                    ["Medium"] = await activeEvents.CountAsync(e => e.Severity == "Medium"),
                    ["Low"] = await activeEvents.CountAsync(e => e.Severity == "Low")
                },

                StatusCounts = new Dictionary<string, int>
                {
                    ["Open"] = await activeEvents.CountAsync(e => e.Status == "Open"),
                    ["In Progress"] = await activeEvents.CountAsync(e => e.Status == "In Progress"),
                    ["Resolved"] = await activeEvents.CountAsync(e => e.Status == "Resolved")
                },

                RecentActivities = await _context.AuditLogs
                    .OrderByDescending(a => a.Timestamp)
                    .Take(5)
                    .ToListAsync(),

                EventTrends = eventTrends
            };

            return View(dashboard);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new Sentinel.Models.ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
