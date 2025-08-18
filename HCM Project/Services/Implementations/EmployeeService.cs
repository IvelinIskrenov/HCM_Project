using HCM_Project.Data;
using HCM_Project.Models;
using HCM_Project.Services.Interfaces;
using HCM_Project.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HCM_Project.Services.Implementations
{
    public class EmployeeService : IEmployeeService
    {
        private readonly HcmContext _context;
        private readonly IPasswordHasher<User> _hasher;

        public EmployeeService(HcmContext context, IPasswordHasher<User> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public async Task<IEnumerable<Employee>> GetIndexAsync(ClaimsPrincipal currentUser)
        {
            if (currentUser.IsInRole("HRAdmin"))
            {
                return await _context.Employees.ToListAsync();
            }

            if (currentUser.IsInRole("Manager"))
            {
                var mgr = await (
                    from e in _context.Employees
                    join u in _context.Users on e.Email equals u.Email
                    where u.Username == currentUser.Identity.Name
                    select e
                ).FirstOrDefaultAsync();

                if (mgr == null) throw new UnauthorizedAccessException("Manager record not found");

                var deptPeople = await _context.Employees
                    .Where(e => e.Department == mgr.Department)
                    .ToListAsync();

                var managers = deptPeople.Where(e => e.Role == "Manager")
                    .OrderBy(e => e.LastName).ThenBy(e => e.FirstName);

                var employees = deptPeople.Where(e => e.Role == "Employee")
                    .OrderBy(e => e.LastName).ThenBy(e => e.FirstName);

                return managers.Concat(employees).ToList();
            }

            // Employee role
            var self = await (
                from e in _context.Employees
                join u in _context.Users on e.Email equals u.Email
                where u.Username == currentUser.Identity.Name
                select e
            ).FirstOrDefaultAsync();

            if (self == null) throw new UnauthorizedAccessException("Employee record not found");

            return new[] { self };
        }

        public async Task<Employee> GetDetailsAsync(int id, ClaimsPrincipal currentUser)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) throw new KeyNotFoundException("Employee not found");

            if (currentUser.IsInRole("Employee"))
            {
                var self = await (
                    from e in _context.Employees
                    join u in _context.Users on e.Email equals u.Email
                    where u.Username == currentUser.Identity.Name
                    select e
                ).FirstOrDefaultAsync();

                if (self == null || self.Id != employee.Id)
                    throw new UnauthorizedAccessException("Access denied");
            }

            return employee;
        }

        public async Task<Employee> CreateAsync(EmployeeCreateViewModel vm, ClaimsPrincipal currentUser)
        {
            // Manager rules enforced here as well
            if (currentUser.IsInRole("Manager"))
            {
                var mgr = await (
                    from e in _context.Employees
                    join u in _context.Users on e.Email equals u.Email
                    where u.Username == currentUser.Identity.Name
                    select e
                ).FirstOrDefaultAsync();

                if (mgr == null) throw new UnauthorizedAccessException("Manager record not found");

                if (vm.Role != "Employee" || vm.Department != mgr.Department)
                    throw new UnauthorizedAccessException("Manager can only create employees in their department");
            }

            var employee = new Employee
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Email = vm.Email,
                JobTitle = vm.JobTitle,
                Salary = vm.Salary,
                Department = vm.Department,
                Role = vm.Role
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // create matching User and hash password
            var user = new User
            {
                Username = $"{vm.FirstName}_{vm.LastName}",
                Email = vm.Email,
                Role = vm.Role
            };
            user.PasswordHash = _hasher.HashPassword(user, vm.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return employee;
        }

        public async Task<(Employee Employee, User? User)> UpdateAsync(Employee updatedEmployee, ClaimsPrincipal currentUser)
        {
            var existing = await _context.Employees.FindAsync(updatedEmployee.Id);
            if (existing == null) throw new KeyNotFoundException("Employee not found");

            // Manager restrictions: 
            //  - cannot edit records that are not Employees (i.e. managers or HRAdmin)
            //  - cannot edit employees outside their department
            //  - cannot change role to something other than "Employee"
            if (currentUser.IsInRole("Manager"))
            {
                var mgr = await (
                    from e in _context.Employees
                    join u in _context.Users on e.Email equals u.Email
                    where u.Username == currentUser.Identity.Name
                    select e
                ).FirstOrDefaultAsync();

                if (mgr == null) throw new UnauthorizedAccessException("Manager record not found");

                // Cannot edit managers/HRAdmin
                if (existing.Role != "Employee")
                    throw new UnauthorizedAccessException("Managers cannot edit other managers or admins.");

                // Cannot edit employees from other departments
                if (existing.Department != mgr.Department)
                    throw new UnauthorizedAccessException("Managers can only edit employees in their own department.");

                // Cannot change role to non-Employee
                if (updatedEmployee.Role != "Employee")
                    throw new UnauthorizedAccessException("Managers can only assign role 'Employee'.");

                // Cannot move employee to another department
                if (updatedEmployee.Department != mgr.Department)
                    throw new UnauthorizedAccessException("Managers can only update employees within their own department.");
            }

            // HRAdmin: allowed to update everything (no checks here)

            // update fields
            existing.FirstName = updatedEmployee.FirstName;
            existing.LastName = updatedEmployee.LastName;
            existing.Email = updatedEmployee.Email;
            existing.JobTitle = updatedEmployee.JobTitle;
            existing.Salary = updatedEmployee.Salary;
            existing.Department = updatedEmployee.Department;
            existing.Role = updatedEmployee.Role;

            _context.Employees.Update(existing);

            // synchronize user role & username if needed
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == existing.Email);
            if (user != null)
            {
                user.Role = existing.Role;
                user.Username = $"{existing.FirstName}_{existing.LastName}";
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();
            return (existing, user);
        }

        public async Task DeleteAsync(int id, ClaimsPrincipal currentUser)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) throw new KeyNotFoundException("Employee not found");

            if (currentUser.IsInRole("Manager"))
            {
                var mgr = await (
                    from e in _context.Employees
                    join u in _context.Users on e.Email equals u.Email
                    where u.Username == currentUser.Identity.Name
                    select e
                ).FirstOrDefaultAsync();

                if (mgr == null || mgr.Department != employee.Department || employee.Role != "Employee")
                    throw new UnauthorizedAccessException("Manager cannot delete this employee");
            }

            _context.Employees.Remove(employee);

            // optional: also remove matching user record (choose what fits your requirements)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == employee.Email);
            if (user != null)
                _context.Users.Remove(user);

            await _context.SaveChangesAsync();
        }
    }
}

