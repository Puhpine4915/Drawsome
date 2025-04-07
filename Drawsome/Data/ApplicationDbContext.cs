using Microsoft.EntityFrameworkCore;
using Drawsome.Models;

namespace Drawsome.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public virtual DbSet<User> Users { get; set; }
    }
}