using IdentityLinkingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityLinkingApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Contact> Contacts { get; set; }
    }
}
