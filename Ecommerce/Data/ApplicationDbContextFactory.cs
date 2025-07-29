using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Ecommerce.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Load configuration from appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            // Read connection string
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var connectionString = config.GetConnectionString("DefaultConnection");

            // Use SQL Server
            optionsBuilder.UseSqlServer(connectionString);

            // Return new DbContext
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
