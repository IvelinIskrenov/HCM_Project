using HCM_Project.Models;
using HCM_Project.Services.Interfaces;
using HCM_Project.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HCM_Project.Controllers
{
    /// <summary>
    /// Controller for managing employees.
    /// Accessible only to authenticated users.
    /// </summary>
    [Authorize]
    public class EmployeesController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _logger = logger;
        }

        /// <summary>
        /// Sets available roles in the ViewBag depending on the current user.
        /// </summary>
        private void SetRoles(string currentRole = null)
        {
            if (User.IsInRole("Manager"))
            {
                ViewBag.Roles = string.Equals(currentRole, "Employee", StringComparison.OrdinalIgnoreCase)
                    ? new[] { "Manager", "Employee" }
                    : new[] { currentRole ?? "Employee" };
            }
            else
            {
                ViewBag.Roles = new[] { "HRAdmin", "Manager", "Employee" };
            }
        }

        /// <summary>
        /// Displays the employee list. Available to all roles.
        /// </summary>
        [Authorize(Roles = "HRAdmin,Manager,Employee")]
        public async Task<IActionResult> Index()
        {
            try
            {
                return View(await _employeeService.GetIndexAsync(User));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized in Index");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Index");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Displays employee details by Id.
        /// </summary>
        [Authorize(Roles = "HRAdmin,Manager,Employee")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                return View(await _employeeService.GetDetailsAsync(id.Value, User));
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Details for {Id}", id);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Returns the create employee form (HRAdmin/Manager only).
        /// </summary>
        [Authorize(Roles = "HRAdmin,Manager")]
        public IActionResult Create()
        {
            SetRoles();
            return View();
        }

        /// <summary>
        /// Handles employee creation.
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "HRAdmin,Manager")]
        public async Task<IActionResult> Create(EmployeeCreateViewModel vm)
        {
            SetRoles();

            if (!ModelState.IsValid) return View(vm);

            // Managers can only create employees
            if (User.IsInRole("Manager") && vm.Role != "Employee")
            {
                ModelState.AddModelError("Role", "Managers can only create employees.");
                SetRoles("Employee");
                return View(vm);
            }

            try
            {
                await _employeeService.CreateAsync(vm, User);
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException ex)
            {
                ModelState.AddModelError("", ex.Message);
                SetRoles(vm.Role);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                ModelState.AddModelError("", "Unexpected error.");
                SetRoles(vm.Role);
                return View(vm);
            }
        }

        /// <summary>
        /// Returns the edit form for a given employee.
        /// </summary>
        [Authorize(Roles = "HRAdmin,Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var emp = await _employeeService.GetDetailsAsync(id.Value, User);
                if (emp == null) return NotFound();

                SetRoles(emp.Role);
                return View(emp);
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Edit for {Id}", id);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Saves changes to an employee.
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "HRAdmin,Manager")]
        public async Task<IActionResult> Edit(int id, Employee employee)
        {
            if (id != employee.Id) return NotFound();
            if (!ModelState.IsValid) return View(employee);

            try
            {
                var (updated, updatedUser) = await _employeeService.UpdateAsync(employee, User);

                // If the current user is updated, refresh their claims
                if (updatedUser?.Username == User.Identity.Name)
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, updatedUser.Username),
                        new Claim(ClaimTypes.Role, updatedUser.Role)
                    };
                    await HttpContext.SignInAsync(
                        new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme))
                    );
                }

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (DbUpdateConcurrencyException)
            {
                try
                {
                    await _employeeService.GetDetailsAsync(id, User);
                    throw; // still exists -> rethrow
                }
                catch (KeyNotFoundException) { return NotFound(); }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Edit for {Id}", id);
                ModelState.AddModelError("", "Unexpected error.");
                return View(employee);
            }

            return View(employee);
        }

        /// <summary>
        /// Displays delete confirmation page for an employee.
        /// </summary>
        [Authorize(Roles = "HRAdmin,Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                return View(await _employeeService.GetDetailsAsync(id.Value, User));
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Delete for {Id}", id);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Deletes an employee.
        /// </summary>
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "HRAdmin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _employeeService.DeleteAsync(id, User);
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteConfirmed for {Id}", id);
                return StatusCode(500);
            }
        }
    }
}




