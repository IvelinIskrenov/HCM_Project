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
    [Authorize]  // All actions require authenticated users
    public class EmployeesController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _logger = logger;
        }

        // GET: Employees
        [Authorize(Roles = "HRAdmin,Manager,Employee")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var list = await _employeeService.GetIndexAsync(User);
                return View(list);
            }
            catch (UnauthorizedAccessException uex)
            {
                _logger.LogWarning(uex, "Unauthorized in Index");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Index");
                return StatusCode(500, "Something went wrong.");
            }
        }

        // GET: Employees/Details/5
        [Authorize(Roles = "HRAdmin,Manager,Employee")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            try
            {
                var emp = await _employeeService.GetDetailsAsync(id.Value, User);
                return View(emp);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Details for {Id}", id);
                return StatusCode(500, "Error loading details.");
            }
        }

        // GET: Employees/Create
        [Authorize(Roles = "HRAdmin,Manager")]
        public IActionResult Create()
        {
            ViewBag.Roles = new[] { "HRAdmin", "Manager", "Employee" };
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "HRAdmin,Manager")]
        public async Task<IActionResult> Create(EmployeeCreateViewModel vm)
        {
            // If current user is Manager, show only Employee role in the UI (when re-rendering)
            if (User.IsInRole("Manager"))
            {
                ViewBag.Roles = new[] { "Employee" };
            }
            else
            {
                ViewBag.Roles = new[] { "HRAdmin", "Manager", "Employee" };
            }

            if (!ModelState.IsValid)
                return View(vm);

            // Extra server-side UX validation: Managers must create only Employees.
            if (User.IsInRole("Manager"))
            {
                // Guard against null/empty role or manipulated form values
                if (string.IsNullOrEmpty(vm.Role) || vm.Role != "Employee")
                {
                    ModelState.AddModelError("Role", "Managers can only create users with role 'Employee'.");
                    return View(vm); // ViewBag.Roles already set to ["Employee"]
                }
            }

            try
            {
                // service will create Employee + User (with hashed password)
                var created = await _employeeService.CreateAsync(vm, User);
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException uex)
            {
                // The service enforces department-level rules and will throw UnauthorizedAccessException
                // (e.g. manager tried to create employee in other department). Show the message to user.
                ModelState.AddModelError("", uex.Message);

                // Ensure the roles list is correct when re-rendering the form after service error
                if (User.IsInRole("Manager"))
                    ViewBag.Roles = new[] { "Employee" };
                else
                    ViewBag.Roles = new[] { "HRAdmin", "Manager", "Employee" };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");

                // Ensure roles are present when re-rendering
                if (User.IsInRole("Manager"))
                    ViewBag.Roles = new[] { "Employee" };
                else
                    ViewBag.Roles = new[] { "HRAdmin", "Manager", "Employee" };

                ModelState.AddModelError("", "Unexpected error.");
                return View(vm);
            }
        }


        // GET: Employees/Edit/5
        [Authorize(Roles = "HRAdmin,Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            try
            {
                var emp = await _employeeService.GetDetailsAsync(id.Value, User);
                ViewBag.Roles = new[] { "HRAdmin", "Manager", "Employee" };
                return View(emp);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Edit for {Id}", id);
                return StatusCode(500, "Error loading edit form.");
            }
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "HRAdmin,Manager")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Email,JobTitle,Salary,Department,Role")] Employee employee)
        {
            if (id != employee.Id) return NotFound();
            ViewBag.Roles = new[] { "HRAdmin", "Manager", "Employee" };

            if (!ModelState.IsValid)
                return View(employee);

            try
            {
                var (updated, updatedUser) = await _employeeService.UpdateAsync(employee, User);

                // If the edited user is the one currently signed in,
                // refresh their auth cookie so their role claim updates immediately:
                if (updatedUser != null && updatedUser.Username == User.Identity.Name)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, updatedUser.Username),
                        new Claim(ClaimTypes.Role, updatedUser.Role)
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(principal);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (DbUpdateConcurrencyException)
            {
                // if concurrency problem: check existence
                try
                {
                    // refetch to see if exists
                    await _employeeService.GetDetailsAsync(id, User);
                    // if exists, rethrow to bubble up
                    throw;
                }
                catch (KeyNotFoundException)
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Edit for {Id}", id);
                ModelState.AddModelError("", "Unexpected error.");
                return View(employee);
            }

            // unreachable
        }

        // GET: Employees/Delete/5
        [Authorize(Roles = "HRAdmin,Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            try
            {
                var emp = await _employeeService.GetDetailsAsync(id.Value, User);
                return View(emp);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Delete for {Id}", id);
                return StatusCode(500, "Error loading delete.");
            }
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "HRAdmin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _employeeService.DeleteAsync(id, User);
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteConfirmed for {Id}", id);
                return StatusCode(500, "Can't delete right now.");
            }
        }
    }
}



