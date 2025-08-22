using System.ComponentModel.DataAnnotations;

namespace HCM_Project.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; } = null!; //First_Last

        [Required, MaxLength(200)]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required, MaxLength(50)]
        public string Role { get; set; } = "Employee";

        //nav 1:1 -> the employee record for this user
        public Employee? Employee { get; set; }
    }
}
