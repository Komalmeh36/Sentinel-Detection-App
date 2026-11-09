
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentinel.Web.Data;

namespace Sentinel.Controllers
{
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AuditLogs
        public async Task<IActionResult> Index()
        {
            var auditLogs = await _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();

            return View(auditLogs);
        }

        // GET: AuditLogs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var auditLog = await _context.AuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(log => log.Id == id);

            if (auditLog == null)
                return NotFound();

            return View(auditLog);
        }
    }
}
