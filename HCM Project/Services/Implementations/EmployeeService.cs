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

        // helper: try to get current user's numeric id from claims (NameIdentifier)
        // returns null if cannot find
        private int? GetCurrentUserId(ClaimsPrincipal currentUser)
        {
            var idClaim = currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var id)) return id;
            return null;
        }

        // helper: get username from ClaimsPrincipal; throws if missing
        private string GetCurrentUsername(ClaimsPrincipal currentUser)
        {
            var username = currentUser?.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                throw new UnauthorizedAccessException("No username in identity");
            return username;
        }

        // GET index: HRAdmin -> everyone
        // Manager -> managers first then employees from same department
        // Employee -> only self
        public async Task<IEnumerable<Employee>> GetIndexAsync(ClaimsPrincipal currentUser)
        {
            if (currentUser.IsInRole("HRAdmin"))
            {
                // admin sees all
                return await _context.Employees.AsNoTracking().ToListAsync();
            }

            if (currentUser.IsInRole("Manager"))
            {
                // find current user's user id or username
                var currentUserId = GetCurrentUserId(currentUser);
                User? cu;
                if (currentUserId.HasValue)
                    cu = await _context.Users.FindAsync(currentUserId.Value);
                else
                {
                    var currentUsername = GetCurrentUsername(currentUser);
                    cu = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
                }

                if (cu == null) throw new UnauthorizedAccessException("Current user record not found");

                // find manager employee by UserId
                var mgr = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == cu.Id);
                if (mgr == null) throw new UnauthorizedAccessException("Manager record not found");

                // load everyone in that department
                var deptPeople = await _context.Employees
                    .Where(e => e.Department == mgr.Department)
                    .AsNoTracking()
                    .ToListAsync();

                var managers = deptPeople.Where(e => string.Equals(e.Role, "Manager", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(e => e.LastName).ThenBy(e => e.FirstName);

                var employees = deptPeople.Where(e => string.Equals(e.Role, "Employee", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(e => e.LastName).ThenBy(e => e.FirstName);

                return managers.Concat(employees).ToList();
            }

            // Employee role -> only self
            {
                var currentUserId = GetCurrentUserId(currentUser);
                User? cu;
                if (currentUserId.HasValue)
                    cu = await _context.Users.FindAsync(currentUserId.Value);
                else
                {
                    var currentUsername = GetCurrentUsername(currentUser);
                    cu = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
                }

                if (cu == null) throw new UnauthorizedAccessException("User record not found");

                var self = await _context.Employees.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.UserId == cu.Id);

                if (self == null) throw new UnauthorizedAccessException("Employee record not found");
                return new[] { self };
            }
        }

        // GET details
        public async Task<Employee> GetDetailsAsync(int id, ClaimsPrincipal currentUser)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) throw new KeyNotFoundException("Employee not found");

            if (currentUser.IsInRole("Employee"))
            {
                // only self can view
                var currentUserId = GetCurrentUserId(currentUser);
                User? cu;
                if (currentUserId.HasValue)
                    cu = await _context.Users.FindAsync(currentUserId.Value);
                else
                {
                    var currentUsername = GetCurrentUsername(currentUser);
                    cu = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
                }

                if (cu == null) throw new UnauthorizedAccessException("User record not found");

                var self = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == cu.Id);
                if (self == null || self.Id != employee.Id)
                    throw new UnauthorizedAccessException("Access denied");
            }

            return employee;
        }

        // CREATE: create User first, then Employee with UserId (in a DB transaction)
        public async Task<Employee> CreateAsync(EmployeeCreateViewModel vm, ClaimsPrincipal currentUser)
        {
            // Manager rules: only create Employees in own department and role must be Employee
            if (currentUser.IsInRole("Manager"))
            {
                var currentUserId = GetCurrentUserId(currentUser);
                User? cu;
                if (currentUserId.HasValue)
                    cu = await _context.Users.FindAsync(currentUserId.Value);
                else
                {
                    var currentUsername = GetCurrentUsername(currentUser);
                    cu = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
                }

                if (cu == null) throw new UnauthorizedAccessException("Current user record not found");

                var mgr = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == cu.Id);
                if (mgr == null) throw new UnauthorizedAccessException("Manager record not found");

                vm.Department = mgr.Department;

                if (!string.Equals(vm.Role, "Employee", StringComparison.OrdinalIgnoreCase) || vm.Department != mgr.Department)
                    throw new UnauthorizedAccessException("Manager can only create employees in their department");
            }

            // prepare username
            var proposedUsername = $"{vm.FirstName}_{vm.LastName}";

            // check duplicates
            if (await _context.Users.AnyAsync(u => u.Username == proposedUsername))
                throw new InvalidOperationException("Username already exists.");

            if (await _context.Users.AnyAsync(u => u.Email == vm.Email))
                throw new InvalidOperationException("Email already registered.");

            // create in transaction
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    Username = proposedUsername,
                    Email = vm.Email,
                    Role = vm.Role
                };
                user.PasswordHash = _hasher.HashPassword(user, vm.Password);

                _context.Users.Add(user);
                await _context.SaveChangesAsync(); // user.Id now exists

                var employee = new Employee
                {
                    FirstName = vm.FirstName,
                    LastName = vm.LastName,
                    Email = vm.Email,
                    JobTitle = vm.JobTitle,
                    Salary = vm.Salary,
                    Department = vm.Department,
                    Role = vm.Role,
                    UserId = user.Id
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                return employee;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // UPDATE: update employee and sync user (by UserId)
        public async Task<(Employee Employee, User? User)> UpdateAsync(Employee updatedEmployee, ClaimsPrincipal currentUser)
        {
            var existing = await _context.Employees.FindAsync(updatedEmployee.Id);
            if (existing == null) throw new KeyNotFoundException("Employee not found");

            //Manager restrictions
            if (currentUser.IsInRole("Manager"))
            {
                var currentUserId = GetCurrentUserId(currentUser);
                User? cu;
                if (currentUserId.HasValue)
                    cu = await _context.Users.FindAsync(currentUserId.Value);
                else
                {
                    var currentUsername = GetCurrentUsername(currentUser);
                    cu = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
                }

                if (cu == null) throw new UnauthorizedAccessException("Current user record not found");

                var mgr = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == cu.Id);
                if (mgr == null) throw new UnauthorizedAccessException("Manager record not found");

                // cannot edit managers/HRAdmin
                if (!string.Equals(existing.Role, "Employee", StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedAccessException("Managers cannot edit other managers or admins.");

                // cannot edit employees from other departments
                if (existing.Department != mgr.Department)
                    throw new UnauthorizedAccessException("Managers can only edit employees in their own department.");

                // cannot move to another department
                if (updatedEmployee.Department != mgr.Department)
                    throw new UnauthorizedAccessException("Managers can only update employees within their own department.");
            }

            // do the update
            existing.FirstName = updatedEmployee.FirstName;
            existing.LastName = updatedEmployee.LastName;
            existing.Email = updatedEmployee.Email;
            existing.JobTitle = updatedEmployee.JobTitle;
            existing.Salary = updatedEmployee.Salary;        
            existing.Role = updatedEmployee.Role;
            if (!currentUser.IsInRole("Manager"))
            {
                existing.Department = updatedEmployee.Department;
            }


                _context.Employees.Update(existing);

            // synchronize user if linked by UserId
            User? user = null;
            if (existing.UserId.HasValue)
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Id == existing.UserId.Value);
                if (user != null)
                {
                    // check username uniqueness if name changed
                    var newUsername = $"{existing.FirstName}_{existing.LastName}";
                    if (!string.Equals(user.Username, newUsername, StringComparison.OrdinalIgnoreCase))
                    {
                        if (await _context.Users.AnyAsync(u => u.Username == newUsername && u.Id != user.Id))
                            throw new InvalidOperationException("Another user already has the requested username.");
                        user.Username = newUsername;
                    }

                    user.Role = existing.Role;
                    user.Email = existing.Email;
                    _context.Users.Update(user);
                }
            }

            await _context.SaveChangesAsync();
            return (existing, user);
        }

        // DELETE: remove employee and associated user (policy: remove matching User if linked)
        public async Task DeleteAsync(int id, ClaimsPrincipal currentUser)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) throw new KeyNotFoundException("Employee not found");

            if (currentUser.IsInRole("Manager"))
            {
                var currentUserId = GetCurrentUserId(currentUser);
                User? cu;
                if (currentUserId.HasValue)
                    cu = await _context.Users.FindAsync(currentUserId.Value);
                else
                {
                    var currentUsername = GetCurrentUsername(currentUser);
                    cu = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
                }

                if (cu == null) throw new UnauthorizedAccessException("Current user record not found");

                var mgr = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == cu.Id);
                if (mgr == null || mgr.Department != employee.Department || !string.Equals(employee.Role, "Employee", StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedAccessException("Manager cannot delete this employee");
            }

            // delete transactionally
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Employees.Remove(employee);

                if (employee.UserId.HasValue)
                {
                    var user = await _context.Users.FindAsync(employee.UserId.Value);
                    if (user != null)
                        _context.Users.Remove(user);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
