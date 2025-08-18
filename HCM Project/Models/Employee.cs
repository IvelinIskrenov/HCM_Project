using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HCM_Project.Models
{
    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string JobTitle { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }

        public string Department { get; set; } = null!;
        public string Role { get; set; } = "Employee";

        // NEW: foreign key to User (1:1)
        // Initially nullable to allow safe migration; later you may make it non-nullable
        public int? UserId { get; set; }

        // Navigation property for 1:1 relation
        public User? User { get; set; }
    }
}

