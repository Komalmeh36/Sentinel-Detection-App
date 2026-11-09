
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Web.Data;
using Sentinel.Web.Models;

namespace Sentinel.Controllers
{
    public class EventItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private static readonly string[] AllowedTypes =
        {
            "Error",
            "Security Threat"
        };

        private static readonly string[] AllowedSeverities =
        {
            "Critical",
            "High",
            "Medium",
            "Low"
        };

        private static readonly string[] AllowedStatuses =
        {
            "Open",
            "In Progress",
            "Resolved"
        };

        public EventItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display, search, filter and sort events belonging to active projects
        public async Task<IActionResult> Index(
            string? searchString,
            string? type,
            string? severity,
            string? status,
            string? sortBy)
        {
            var events = _context.EventItems
                .Include(e => e.Service)
                    .ThenInclude(s => s!.Project)
                .Where(e => e.Service != null &&
                            e.Service.Project != null &&
                            !e.Service.Project.IsArchived);

            // Search by event title or message
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                events = events.Where(e =>
                    e.Title.Contains(searchString) ||
                    e.Message.Contains(searchString));
            }

            // Filter by event type
            if (!string.IsNullOrWhiteSpace(type))
            {
                events = events.Where(e => e.Type == type);
            }

            // Filter by severity
            if (!string.IsNullOrWhiteSpace(severity))
            {
                events = events.Where(e => e.Severity == severity);
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                events = events.Where(e => e.Status == status);
            }

            // Default sorting
            sortBy = string.IsNullOrWhiteSpace(sortBy)
                ? "newest"
                : sortBy;

            // Apply sorting
            events = sortBy switch
            {
                "oldest" => events.OrderBy(e => e.CreatedAt),

                "title" => events.OrderBy(e => e.Title),

                "severity" => events.OrderBy(e =>
                    e.Severity == "Critical" ? 1 :
                    e.Severity == "High" ? 2 :
                    e.Severity == "Medium" ? 3 : 4),

                "status" => events.OrderBy(e =>
                    e.Status == "Open" ? 1 :
                    e.Status == "In Progress" ? 2 : 3),

                _ => events.OrderByDescending(e => e.CreatedAt)
            };

            // Preserve selected search, filter and sorting values
            ViewBag.SearchString = searchString;
            ViewBag.SelectedType = type;
            ViewBag.SelectedSeverity = severity;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedSort = sortBy;

            var result = await events.ToListAsync();

            return View(result);
        }

        // Display event details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var eventItem = await _context.EventItems
                .Include(e => e.Service)
                    .ThenInclude(s => s!.Project)
                .FirstOrDefaultAsync(e =>
                    e.Id == id &&
                    e.Service != null &&
                    e.Service.Project != null &&
                    !e.Service.Project.IsArchived);

            if (eventItem == null)
                return NotFound();

            return View(eventItem);
        }

        // Display create form
        public async Task<IActionResult> Create()
        {
            await LoadServicesAsync();
            return View();
        }

        // Save a new event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Title,Message,Type,Severity,Status,ServiceId")]
            EventItem eventItem)
        {
            ValidateEvent(eventItem);

            var service = await _context.Services
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s =>
                    s.Id == eventItem.ServiceId &&
                    s.Project != null &&
                    !s.Project.IsArchived);

            if (service == null)
            {
                ModelState.AddModelError(
                    "ServiceId",
                    "Please select a service belonging to an active project.");
            }

            if (ModelState.IsValid)
            {
                eventItem.CreatedAt = DateTime.UtcNow;

                _context.EventItems.Add(eventItem);

                AddAuditLog(
                    "Event Created",
                    $"Event '{eventItem.Title}' was created. " +
                    $"Type: {eventItem.Type}, Severity: {eventItem.Severity}, " +
                    $"Status: {eventItem.Status}, Service: '{service!.Name}', " +
                    $"Project: '{service.Project!.Name}'.");

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Event created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadServicesAsync(eventItem.ServiceId);
            return View(eventItem);
        }

        // Display edit form
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var eventItem = await _context.EventItems
                .Include(e => e.Service)
                    .ThenInclude(s => s!.Project)
                .FirstOrDefaultAsync(e =>
                    e.Id == id &&
                    e.Service != null &&
                    e.Service.Project != null &&
                    !e.Service.Project.IsArchived);

            if (eventItem == null)
                return NotFound();

            await LoadServicesAsync(eventItem.ServiceId);
            return View(eventItem);
        }

        // Save event changes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Title,Message,Type,Severity,Status,ServiceId")]
            EventItem eventItem)
        {
            if (id != eventItem.Id)
                return NotFound();

            ValidateEvent(eventItem);

            var existingEvent = await _context.EventItems
                .Include(e => e.Service)
                    .ThenInclude(s => s!.Project)
                .FirstOrDefaultAsync(e =>
                    e.Id == id &&
                    e.Service != null &&
                    e.Service.Project != null &&
                    !e.Service.Project.IsArchived);

            if (existingEvent == null)
                return NotFound();

            var newService = await _context.Services
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s =>
                    s.Id == eventItem.ServiceId &&
                    s.Project != null &&
                    !s.Project.IsArchived);

            if (newService == null)
            {
                ModelState.AddModelError(
                    "ServiceId",
                    "Please select a service belonging to an active project.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var oldTitle = existingEvent.Title;
                    var oldSeverity = existingEvent.Severity;
                    var oldStatus = existingEvent.Status;
                    var oldServiceName = existingEvent.Service?.Name;

                    existingEvent.Title = eventItem.Title;
                    existingEvent.Message = eventItem.Message;
                    existingEvent.Type = eventItem.Type;
                    existingEvent.Severity = eventItem.Severity;
                    existingEvent.Status = eventItem.Status;
                    existingEvent.ServiceId = eventItem.ServiceId;

                    AddAuditLog(
                        "Event Updated",
                        $"Event '{oldTitle}' was updated to '{eventItem.Title}'. " +
                        $"Severity: {oldSeverity} to {eventItem.Severity}. " +
                        $"Status: {oldStatus} to {eventItem.Status}. " +
                        $"Service: '{oldServiceName}' to '{newService!.Name}'.");

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Event updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.EventItems.AnyAsync(e => e.Id == id))
                        return NotFound();

                    throw;
                }
            }

            await LoadServicesAsync(eventItem.ServiceId);
            return View(eventItem);
        }

        // Display delete confirmation
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var eventItem = await _context.EventItems
                .Include(e => e.Service)
                    .ThenInclude(s => s!.Project)
                .FirstOrDefaultAsync(e =>
                    e.Id == id &&
                    e.Service != null &&
                    e.Service.Project != null &&
                    !e.Service.Project.IsArchived);

            if (eventItem == null)
                return NotFound();

            return View(eventItem);
        }

        // Delete an event
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventItem = await _context.EventItems
                .Include(e => e.Service)
                    .ThenInclude(s => s!.Project)
                .FirstOrDefaultAsync(e =>
                    e.Id == id &&
                    e.Service != null &&
                    e.Service.Project != null &&
                    !e.Service.Project.IsArchived);

            if (eventItem == null)
                return NotFound();

            AddAuditLog(
                "Event Deleted",
                $"Event '{eventItem.Title}' was deleted. " +
                $"Type: {eventItem.Type}, Severity: {eventItem.Severity}, " +
                $"Status: {eventItem.Status}, Service: '{eventItem.Service?.Name}', " +
                $"Project: '{eventItem.Service?.Project?.Name}'.");

            _context.EventItems.Remove(eventItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Validate event fields on the server
        private void ValidateEvent(EventItem eventItem)
        {
            if (string.IsNullOrWhiteSpace(eventItem.Title))
            {
                ModelState.AddModelError(
                    "Title", "Event title is required.");
            }
            else if (eventItem.Title.Length > 150)
            {
                ModelState.AddModelError(
                    "Title", "Event title cannot exceed 150 characters.");
            }

            if (string.IsNullOrWhiteSpace(eventItem.Message))
            {
                ModelState.AddModelError(
                    "Message", "Event description is required.");
            }
            else if (eventItem.Message.Length > 2000)
            {
                ModelState.AddModelError(
                    "Message", "Event description cannot exceed 2000 characters.");
            }

            if (!AllowedTypes.Contains(eventItem.Type))
            {
                ModelState.AddModelError(
                    "Type", "Please select a valid event type.");
            }

            if (!AllowedSeverities.Contains(eventItem.Severity))
            {
                ModelState.AddModelError(
                    "Severity", "Please select a valid severity.");
            }

            if (!AllowedStatuses.Contains(eventItem.Status))
            {
                ModelState.AddModelError(
                    "Status", "Please select a valid event status.");
            }

            if (eventItem.ServiceId <= 0)
            {
                ModelState.AddModelError(
                    "ServiceId", "Please select an associated service.");
            }
        }

        // Add audit log entry
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

        // Load services belonging to active projects only
        private async Task LoadServicesAsync(int? selectedServiceId = null)
        {
            var services = await _context.Services
                .Include(s => s.Project)
                .Where(s => s.Project != null && !s.Project.IsArchived)
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewBag.ServiceId = new SelectList(
                services.Select(s => new
                {
                    s.Id,
                    DisplayName = s.Name + " — " + s.Project!.Name
                }),
                "Id",
                "DisplayName",
                selectedServiceId);
        }
    }
}
