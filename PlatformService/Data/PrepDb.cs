using Microsoft.EntityFrameworkCore;
using PlatformService.Models;

namespace PlatformService.Data;

public static class PrepDb
{
    public static void PrepPopulation(IApplicationBuilder app, bool isProd)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        SeedData(serviceScope.ServiceProvider.GetService<AppDbContext>(), isProd);
    }
    
    private static void SeedData(AppDbContext context, bool isProd)
    {
        if(isProd)
        {
            Console.WriteLine("--- > attempting to apply migration");
            try
            {
                context.Database.Migrate();
            }
            catch(Exception ex)
            {
                Console.WriteLine($" ---> could not run migration: {ex.Message}");
            }
        }

        if (!context.Platforms.Any())
        {
            Console.WriteLine(" -------> Seeding data");
            
            context.Platforms.AddRange(
              new Platform() { Name = "Dot Net", Publisher = "Microsoft", Cost = "Free" },
              new Platform { Name = "SQL Server", Publisher = "Microsoft", Cost = "Free" },
              new Platform() { Name = "Kubernetes", Publisher = "Cloud Native Computing", Cost = "Free" }
            );
            
            context.SaveChanges();
        }
        else Console.WriteLine(" -------> Already have data");
    }
}