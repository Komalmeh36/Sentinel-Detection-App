
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentinel.Web.Data;
using Sentinel.Web.Models;

namespace Sentinel.Controllers
{
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Services
        // Show services only for active projects
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services
                .Include(s => s.Project)
                .Where(s => s.Project != null && !s.Project.IsArchived)
                .ToListAsync();

            return View(services);
        }

        // GET: Services/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var service = await _context.Services
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.Project != null &&
                    !s.Project.IsArchived);

            if (service == null)
                return NotFound();

            return View(service);
        }

        // GET: Services/Create
        public async Task<IActionResult> Create()
        {
            await LoadActiveProjectsAsync();
            return View();
        }

        // POST: Services/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service)
        {
            bool projectIsActive = await _context.Projects
                .AnyAsync(p => p.Id == service.ProjectId && !p.IsArchived);

            if (!projectIsActive)
            {
                ModelState.AddModelError(
                    "ProjectId",
                    "Please select an active project.");
            }

            if (ModelState.IsValid)
            {
                var projectName = await _context.Projects
                    .Where(p => p.Id == service.ProjectId)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync();

                _context.Services.Add(service);

                AddAuditLog(
                    "Service Created",
                    $"Service '{service.Name}' was created for project '{projectName}'.");

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadActiveProjectsAsync(service.ProjectId);
            return View(service);
        }

        // GET: Services/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var service = await _context.Services
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.Project != null &&
                    !s.Project.IsArchived);

            if (service == null)
                return NotFound();

            await LoadActiveProjectsAsync(service.ProjectId);
            return View(service);
        }

        // POST: Services/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Service service)
        {
            if (id != service.Id)
                return NotFound();

            var existingService = await _context.Services
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.Project != null &&
                    !s.Project.IsArchived);

            if (existingService == null)
                return NotFound();

            bool projectIsActive = await _context.Projects
                .AnyAsync(p => p.Id == service.ProjectId && !p.IsArchived);

            if (!projectIsActive)
            {
                ModelState.AddModelError(
                    "ProjectId",
                    "Please select an active project.");
            }

            if (ModelState.IsValid)
            {
                var oldName = existingService.Name;
                var oldProjectName = existingService.Project?.Name;

                var newProjectName = await _context.Projects
                    .Where(p => p.Id == service.ProjectId)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync();

                existingService.Name = service.Name;
                existingService.ProjectId = service.ProjectId;

                AddAuditLog(
                    "Service Updated",
                    $"Service '{oldName}' was updated to '{service.Name}'. " +
                    $"Project: '{oldProjectName}' to '{newProjectName}'.");

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadActiveProjectsAsync(service.ProjectId);
            return View(service);
        }

        // GET: Services/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var service = await _context.Services
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.Project != null &&
                    !s.Project.IsArchived);

            if (service == null)
                return NotFound();

            return View(service);
        }

        // POST: Services/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.Services
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.Project != null &&
                    !s.Project.IsArchived);

            if (service != null)
            {
                AddAuditLog(
                    "Service Deleted",
                    $"Service '{service.Name}' was deleted from project '{service.Project?.Name}'.");

                _context.Services.Remove(service);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Add an audit log entry
        private void AddAuditLog(string action, string details)
        {
            var auditLog = new AuditLog
            {
                Action = action,
                UserId = User.Identity?.Name ?? "System",
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
        }

        // Load active projects for Create and Edit dropdowns
        private async Task LoadActiveProjectsAsync(int? selectedProjectId = null)
        {
            ViewBag.Projects = await _context.Projects
                .Where(p => !p.IsArchived)
                .OrderBy(p => p.Name)
                .ToListAsync();

            ViewBag.SelectedProjectId = selectedProjectId;
        }
    }
}
