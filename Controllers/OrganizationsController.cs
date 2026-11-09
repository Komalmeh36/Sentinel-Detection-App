
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentinel.Web.Data;
using Sentinel.Web.Models;

namespace Sentinel.Controllers
{
    public class OrganizationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrganizationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Automatically create an audit log
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

        // Display all organizations
        public async Task<IActionResult> Index()
        {
            var organizations = await _context.Organizations
                .Include(o => o.Projects)
                .ToListAsync();

            return View(organizations);
        }

        // Display organization details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var organization = await _context.Organizations
                .Include(o => o.Projects)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
                return NotFound();

            return View(organization);
        }

        // Display the create form
        public IActionResult Create()
        {
            return View();
        }

        // Save a new organization
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Organization organization)
        {
            if (ModelState.IsValid)
            {
                _context.Organizations.Add(organization);

                AddAuditLog(
                    "Organization Created",
                    $"Organization '{organization.Name}' was created."
                );

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(organization);
        }

        // Display the edit form
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var organization = await _context.Organizations.FindAsync(id);

            if (organization == null)
                return NotFound();

            return View(organization);
        }

        // Save organization changes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Organization organization)
        {
            if (id != organization.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Entry(organization).State = EntityState.Modified;

                AddAuditLog(
                    "Organization Updated",
                    $"Organization '{organization.Name}' was updated."
                );

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(organization);
        }

        // Display delete confirmation
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var organization = await _context.Organizations
                .Include(o => o.Projects)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
                return NotFound();

            return View(organization);
        }

        // Delete an organization
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var organization = await _context.Organizations
                .Include(o => o.Projects)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
                return NotFound();

            // Prevent deletion when projects are assigned
            if (organization.Projects.Any())
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This organization has projects assigned to it. Reassign or remove those projects before deleting this organization."
                );

                return View("Delete", organization);
            }

            AddAuditLog(
                "Organization Deleted",
                $"Organization '{organization.Name}' was deleted."
            );

            _context.Organizations.Remove(organization);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
