using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Peep.API.Persistence
{
    public class PeepApiContextFactory : IDesignTimeDbContextFactory<PeepApiContext>
    {
        public PeepApiContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PeepApiContext>();
            optionsBuilder.UseNpgsql("Host=db;Database=peep;Username=postgres;Password=password");
            return new PeepApiContext(optionsBuilder.Options);
        }
    }
}