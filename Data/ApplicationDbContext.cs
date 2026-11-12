
using Microsoft.EntityFrameworkCore;
using Sentinel.Web.Models;

namespace Sentinel.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected ApplicationDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<EventItem> EventItems { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
    }
}
