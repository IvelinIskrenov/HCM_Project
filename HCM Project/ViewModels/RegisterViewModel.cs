//WE ARE NOT USING THAT
using System.ComponentModel.DataAnnotations;

namespace HCM_Project.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; }  // HRAdmin, Manager или Employee
    }
}

