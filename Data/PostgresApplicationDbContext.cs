
using Microsoft.EntityFrameworkCore;

namespace Sentinel.Web.Data
{
    public class PostgresApplicationDbContext : ApplicationDbContext
    {
        public PostgresApplicationDbContext(
            DbContextOptions<PostgresApplicationDbContext> options)
            : base(options)
        {
        }
    }
}
