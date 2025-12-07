using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataAccess
{
    // This is used ONLY at design time by EF (for migrations)
    public class RestaurantDbContextFactory : IDesignTimeDbContextFactory<RestaurantDbContext>
    {
        public RestaurantDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RestaurantDbContext>();

            // SAME connection string as in appsettings.json
            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=RestaurantMvcDb;Trusted_Connection=True;MultipleActiveResultSets=true");

            return new RestaurantDbContext(optionsBuilder.Options);
        }
    }
}
