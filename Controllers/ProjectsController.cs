
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentinel.Web.Data;
using Sentinel.Web.Models;

namespace Sentinel.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper: Save an audit log
        private async Task LogActivityAsync(string action, string details)
        {
            var auditLog = new AuditLog
            {
                Action = action,
                UserId = User.Identity?.Name ?? "System",
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects
                .Include(p => p.Organization)
                .ToListAsync();

            return View(projects);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var project = await _context.Projects
                .Include(p => p.Organization)
                .Include(p => p.Services)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            return View(project);
        }

        // GET: Projects/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Organizations = await _context.Organizations.ToListAsync();
            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project)
        {
            if (ModelState.IsValid)
            {
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                await LogActivityAsync(
                    "Project Created",
                    $"Project '{project.Name}' (ID: {project.Id}) was created."
                );

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Organizations = await _context.Organizations.ToListAsync();
            return View(project);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var project = await _context.Projects.FindAsync(id);

            if (project == null)
                return NotFound();

            ViewBag.Organizations = await _context.Organizations.ToListAsync();
            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project)
        {
            if (id != project.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var existingProject = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (existingProject == null)
                    return NotFound();

                bool wasArchived = existingProject.IsArchived;

                _context.Entry(existingProject)
                    .CurrentValues.SetValues(project);

                string action;

                if (!wasArchived && existingProject.IsArchived)
                {
                    action = "Project Archived";
                }
                else if (wasArchived && !existingProject.IsArchived)
                {
                    action = "Project Reactivated";
                }
                else
                {
                    action = "Project Updated";
                }

                await _context.SaveChangesAsync();

                await LogActivityAsync(
                    action,
                    $"Project '{existingProject.Name}' (ID: {existingProject.Id}) was updated. Activity: {action}."
                );

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Organizations = await _context.Organizations.ToListAsync();
            return View(project);
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var project = await _context.Projects
                .Include(p => p.Organization)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project != null)
            {
                string projectName = project.Name;
                int projectId = project.Id;

                await LogActivityAsync(
                    "Project Deleted",
                    $"Project '{projectName}' (ID: {projectId}) was deleted."
                );

                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
