
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sentinel.Web.Data
{
    public class PostgresApplicationDbContextFactory
        : IDesignTimeDbContextFactory<PostgresApplicationDbContext>
    {
        public PostgresApplicationDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable(
                    "ConnectionStrings__DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string is missing.");
            }

            var optionsBuilder =
                new DbContextOptionsBuilder<PostgresApplicationDbContext>();

            optionsBuilder.UseNpgsql(connectionString);

            return new PostgresApplicationDbContext(optionsBuilder.Options);
        }
    }
}
