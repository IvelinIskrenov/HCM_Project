using System.IO;
using System.Reflection;
using HCM_Project.Services.Interfaces;
using HCM_Project.Services.Implementations;
using HCM_Project.Data;
using HCM_Project.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add support for MVC (Controllers + Razor Views)
builder.Services.AddControllersWithViews();

// Register the Swagger generator and the OpenAPI endpoint
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "HCM API", Version = "v1" });

    // Optional: include XML comments if the file exists (for richer Swagger descriptions)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// Configure Entity Framework Core to use SQL Server with connection string from configuration
builder.Services.AddDbContext<HcmContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register a password hasher service for the User model
// This is required by EmployeeService which hashes user passwords when creating users.
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Service layer registrations
// Register IEmployeeService so controllers can depend on the service instead of using DbContext directly.
// This moves business logic out of controllers into a testable service layer.
builder.Services.AddScoped<IEmployeeService, EmployeeService>();


// Configure Cookie-based Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Path to redirect when user is not authenticated
        options.LoginPath = "/Account/Login";
        // Path to redirect when user is authenticated but not authorized
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Add authorization services to support role- or policy-based authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Enable middleware to serve generated Swagger as JSON
app.UseSwagger();

// Enable middleware to serve Swagger UI (HTML/CSS/JS) at /swagger
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HCM API V1");
    c.RoutePrefix = "swagger";  // serve the UI at /swagger
});

// Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    // Use custom error handling and enable HSTS in production
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Redirect HTTP requests to HTTPS
app.UseHttpsRedirection();

// Enable serving of static files like CSS, JS, images
app.UseStaticFiles();

app.UseRouting();

// **Important:** Authentication middleware must come before authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Configure default routing for MVC controllers and actions
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


//git add .
//git commit -m "Refactor: add service layer (IEmployeeService + EmployeeService) and thin EmployeesController"
//git push

