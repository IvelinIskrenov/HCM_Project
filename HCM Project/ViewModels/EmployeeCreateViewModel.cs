using System.ComponentModel.DataAnnotations;

namespace HCM_Project.ViewModels
{
    public class EmployeeCreateViewModel
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string JobTitle { get; set; }

        [Required]
        public decimal Salary { get; set; }

        [Required]
        public string Department { get; set; }

        [Required]
        public string Role { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
