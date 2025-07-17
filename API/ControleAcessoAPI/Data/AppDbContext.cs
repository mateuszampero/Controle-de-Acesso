using Microsoft.EntityFrameworkCore;
using ControleAcessoAPI.Models;

namespace ControleAcessoAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Computador> Computadores { get; set; }
        public DbSet<StatusLog> StatusLogs { get; set; }

    }
}
