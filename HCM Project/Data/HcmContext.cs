using Microsoft.EntityFrameworkCore;
using HCM_Project.Models;

namespace HCM_Project.Data  
{
    // Application's database context: connects models to the database
    public class HcmContext : DbContext
    {
        // Constructor: configures the context with options 
        public HcmContext(DbContextOptions<HcmContext> options)
            : base(options)
        {
        }

        // DbSet for Employees table in the database
        public DbSet<Employee> Employees { get; set; }

        // DbSet for Users table (used for authentication and roles)
        public DbSet<User> Users { get; set; }
    }
}


