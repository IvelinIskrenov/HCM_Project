using Microsoft.EntityFrameworkCore;
using HCM_Project.Models;

namespace HCM_Project.Data
{
    public class HcmContext : DbContext
    {
        public HcmContext(DbContextOptions<HcmContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // precision for Salary
            modelBuilder.Entity<Employee>()
                .Property(e => e.Salary)
                .HasPrecision(18, 2);

            // unique index on Username
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // 1:1 mapping: User <-> Employee via Employee.UserId
            // A User has one Employee, an Employee has one User, FK is Employee.UserId
            modelBuilder.Entity<User>()
                .HasOne(u => u.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<Employee>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade); // change behavior if you prefer SetNull
        }
    }
}


