using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Peep.API.Persistence
{
    public class PeepApiContextFactory : IDesignTimeDbContextFactory<PeepApiContext>
    {
        public PeepApiContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PeepApiContext>();
            optionsBuilder.UseMySql("server=db;user=root;password=password;database=peep");
            return new PeepApiContext(optionsBuilder.Options);
        }
    }
}