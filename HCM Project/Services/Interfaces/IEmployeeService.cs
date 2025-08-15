using HCM_Project.Models;
using HCM_Project.ViewModels;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HCM_Project.Services.Interfaces
{
    public interface IEmployeeService
    {
        Task<IEnumerable<Employee>> GetIndexAsync(ClaimsPrincipal currentUser);
        Task<Employee> GetDetailsAsync(int id, ClaimsPrincipal currentUser);
        /// <summary>
        /// Creates Employee and matching User (password is hashed inside).
        /// Returns created Employee.
        /// </summary>
        Task<Employee> CreateAsync(EmployeeCreateViewModel vm, ClaimsPrincipal currentUser);
        /// <summary>
        /// Updates employee and synchronizes Users table role.
        /// Returns tuple: updated Employee and updated User (or null if user not found).
        /// </summary>
        Task<(Employee Employee, User? User)> UpdateAsync(Employee updatedEmployee, ClaimsPrincipal currentUser);
        Task DeleteAsync(int id, ClaimsPrincipal currentUser);
    }
}
