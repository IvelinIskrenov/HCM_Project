namespace HCM_Project.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string JobTitle { get; set; }
        public decimal Salary { get; set; }
        public string Department { get; set; }

        public string Role { get; set; }
    }
}
