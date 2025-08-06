using HCM_Project.Data;
using HCM_Project.Models;
using HCM_Project.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HCM_Project.Controllers
{
    public class AccountController : Controller
    {
        private readonly HcmContext _context;
        private readonly IPasswordHasher<User> _hasher;

        public AccountController(HcmContext context, IPasswordHasher<User> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        // GET: /Account/Login
        // Displays the login form.
        [HttpGet]
        public IActionResult Login() => View();

        // POST: /Account/Login
        // Authenticates the user. If credentials are valid, signs them in using cookies.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // Try to find user by username
            //var user = _context.Users.SingleOrDefault(u => u.Username == vm.Username);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == vm.Username);

            // Validate password
            if (user == null ||
                _hasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password)
                    != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(vm);
            }

            // Sign the user in
            await SignInUser(user);
            return RedirectToAction("Index", "Employees");
        }

        // Handles the actual sign-in logic by creating a ClaimsPrincipal
        // and issuing a cookie for the user.
        private async Task SignInUser(User user)
        {
            var claims = new List<Claim> {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(principal);
        }

        // GET: /Account/Logout
        // Signs the user out and redirects to Login page.
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        // Shown when a user tries to access a forbidden resource.
        public IActionResult AccessDenied() => View();
    }
}

