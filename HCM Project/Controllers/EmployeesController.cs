using HCM_Project.Data;
using HCM_Project.Models;
using HCM_Project.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HCM_Project.Controllers
{
    [Authorize]  // All actions require authenticated users
    public class EmployeesController : Controller
    {
        private readonly HcmContext _context;
        private readonly IPasswordHasher<User> _hasher;
        private readonly ILogger<EmployeesController> _logger;


        public EmployeesController(HcmContext context, IPasswordHasher<User> hasher, ILogger<EmployeesController> logger)
        {
            _context = context;
            _hasher = hasher;
            _logger = logger;
        }

        // GET: Employees
        [Authorize(Roles = "HRAdmin,Manager,Employee")]
        public async Task<IActionResult> Index()
        {
            try
            {
                if (User.IsInRole("HRAdmin"))
                {
                    // HRAdmin sees everyone
                    var allEmployees = await _context.Employees.ToListAsync();
                    return View(allEmployees);
                }
                else if (User.IsInRole("Manager"))
                {
                    var mgr = await (
                        from e in _context.Employees
                        join u in _context.Users on e.Email equals u.Email
                        where u.Username == User.Identity.Name
                        select e
                    ).FirstOrDefaultAsync();
                    if (mgr == null) return Forbid();

                    var mgrDept = mgr.Department;

                    var deptGroup = await (
                            from e in _context.Employees
                            where e.Department == mgr.Department
                            select e
                        ).ToListAsync();

                    var managers = (
                            from e in deptGroup
                            where e.Role == "Manager"
                            orderby e.LastName, e.FirstName
                            select e
                        ).ToList();

                    var employees = (
                            from e in deptGroup
                            where e.Role == "Employee"
                            orderby e.LastName, e.FirstName
                            select e
                        ).ToList();

                    var result = managers.Concat(employees).ToList();
                    return View(result);
                }
                else
                {
                    var self = await (
                        from e in _context.Employees
                        join u in _context.Users on e.Email equals u.Email
                        where u.Username == User.Identity.Name
                        select e
                    ).FirstOrDefaultAsync();

                    if (self == null) return Forbid();
                    return View(new[] { self });
                }
            }
            catch (DbUpdateException dbEx)
            {
                // got a database error here
                _logger.LogError(dbEx, "DB error in Index");
                return StatusCode(500, "Oops, DB problem.");
            }
            catch (System.Exception ex)
            {
                // something unexpected happened
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
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null) return NotFound();

                if (User.IsInRole("Employee"))
                {
                    var self = await (
                        from e in _context.Employees
                        join u in _context.Users on e.Email equals u.Email
                        where u.Username == User.Identity.Name
                        select e
                    ).FirstOrDefaultAsync();
                    if (self == null || self.Id != employee.Id)
                        return Forbid();
                }

                return View(employee);
            }
            catch (System.Exception ex)
            {
                // error loading details
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
            ViewBag.Roles = new[] { "HRAdmin", "Manager", "Employee" };

            if (!ModelState.IsValid)
                return View(vm);
            try
            {
                if (User.IsInRole("Manager"))
                {
                    var mgr = await (
                        from e in _context.Employees
                        join u in _context.Users on e.Email equals u.Email
                        where u.Username == User.Identity.Name
                        select e
                    ).FirstOrDefaultAsync();
                    if (mgr == null) return Forbid();

                    if (vm.Role != "Employee" || vm.Department != mgr.Department)
                    {
                        ModelState.AddModelError("",
                            "Manager can only create Employees in their department.");
                        return View(vm);
                    }
                }

                // 1) Create Employee
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

                // 2) Create matching User
                var user = new User
                {
                    Username = $"{vm.FirstName}_{vm.LastName}",
                    Email = vm.Email,
                    Role = vm.Role
                };
                user.PasswordHash = _hasher.HashPassword(user, vm.Password);
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                // got a DB error when saving
                _logger.LogError(dbEx, "DB error in Create");
                ModelState.AddModelError("", "Oops, can't save right now.");
                return View(vm);
            }
            catch (System.Exception ex)
            {
                // unexpected error in create
                _logger.LogError(ex, "Error in Create");
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
                var emp = await _context.Employees.FindAsync(id);
                if (emp == null) return NotFound();

                ViewBag.Roles = new[] { "HRAdmin", "Manager", "Employee" };
                return View(emp);
            }
            catch (System.Exception ex)
            {
                // error loading edit form
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

            if (User.IsInRole("Manager"))
            {
                var mgr = await (
                    from e in _context.Employees
                    join u in _context.Users on e.Email equals u.Email
                    where u.Username == User.Identity.Name
                    select e
                ).FirstOrDefaultAsync();
                if (mgr == null) return Forbid();

                if (employee.Role != "Employee" || employee.Department != mgr.Department)
                    return Forbid();
            }

            try
            {
                // 1) Update employee
                _context.Update(employee);

                // 2) Update user's role
                var user = await (
                        from u in _context.Users
                        where u.Email == employee.Email
                        select u
                    ).FirstOrDefaultAsync();
                if (user != null)
                {
                    user.Role = employee.Role;
                    _context.Users.Update(user);
                }

                await _context.SaveChangesAsync();
                //  If the edited user is the one currently signed in,
                //  refresh their auth cookie so their role claim updates immediately:
                if (user != null && user.Username == User.Identity.Name)
                {
                    // rebuild their claims
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role)
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(principal);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Employees.Any(e => e.Id == id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/Delete/5
        [Authorize(Roles = "HRAdmin,Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            try
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(m => m.Id == id);
                if (employee == null) return NotFound();
                if (User.IsInRole("Manager"))
                {
                    // find this manager's department -> Users join
                    var mgr = await (
                        from e in _context.Employees
                        join u in _context.Users on e.Email equals u.Email
                        where u.Username == User.Identity.Name
                        select e
                    ).FirstOrDefaultAsync();
                    if (mgr == null || mgr.Department != employee.Department || employee.Role != "Employee")
                        return Forbid();
                }
                return View(employee);
            }
            catch (System.Exception ex)
            {
                // error loading delete confirmation
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
                var employee = await _context.Employees.FindAsync(id);
                if (employee != null)
                {
                    if (User.IsInRole("Manager"))
                    {
                        var mgr = await (
                            from e in _context.Employees
                            join u in _context.Users on e.Email equals u.Email
                            where u.Username == User.Identity.Name
                            select e
                        ).FirstOrDefaultAsync();
                        if (mgr == null || mgr.Department != employee.Department || employee.Role != "Employee")
                            return Forbid();
                    }
                    _context.Employees.Remove(employee);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                // db error in delete
                _logger.LogError(dbEx, "DB error in DeleteConfirmed for {Id}", id);
                return StatusCode(500, "Can't delete right now.");
            }
            catch (System.Exception ex)
            {
                // unexpected error in delete
                _logger.LogError(ex, "Error in DeleteConfirmed for {Id}", id);
                return StatusCode(500, "Unexpected error.");
            }
        }
    }
}


