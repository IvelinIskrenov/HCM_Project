# HCM Project

A Human Capital Management (HCM) Web Application built with ASP .NET Core 8, Entity Framework Core, and React/Blazor (Razor views). It simulates essential HR workflows—managing employees, departments, user roles—and demonstrates best practices: clean code, layered architecture, authentication/authorization, OpenAPI docs, and Docker containerization.

---

# Features

- CRUD operations for Employees and Departments  
- Authentication (Cookie-based) & Authorization (HRAdmin, Manager, Employee)  
- Razor UI for web interface (Views + Controllers)  
- REST API endpoints under `/api/employees` with full Swagger/OpenAPI support  
- Entity Framework Core (Code-First) with SQL Server  
- Swagger UI for live API docs and testing  
- Docker support for consistent local/deployment containers  
- Error handling & logging with `ILogger<T>`  
- Clean architecture patterns: DI, Layers (Controllers, Data, Models, ViewModels)

---

# Tech Stack

- .NET 8.0 / C#  
- ASP .NET Core MVC** & Web API 
- Entity Framework Core 9 (SQL Server)  
- Swashbuckle.AspNetCore (Swagger/OpenAPI)  
- Microsoft.AspNetCore.Identity for password hashing & roles  
- Docker / Docker Compose  
- Bootstrap 5 for front-end styling  

---

# Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)  
- [SQL Server Express / LocalDB](https://aka.ms/ssmsfullsetup)  
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for containerization)  
- Optional: Visual Studio 2022 / VS Code  

# Usage Instructions

Start the application - In Visual Studio: press F5 (https)
To enter as HR Admin - go to login and fill the from (username - Ivelin, Password - 123123123)
To enter as some of the managers for example (username - Tom_Krus, Password - TomKrus) - you will see only your department Employees
To enter as Employee use - (username - Orlando_Bloom, Password - OrlandoBloom) you will see only your details
# Create
You can test Create function - Fill out the form (First Name, Last Name, Email, JobTitle, Salary, Department, Role, Password), 
when you create a Employee automated the Username will be like  (FirstName_LastName)
# Delete
You can try to delete some of the Employees:
- HR Admin can delete everyone
- Manager can delete only Employees in his Department
- Employee can't delete
# Edit
In the list, click "Edit" next to an employee.
Modify any fields (including Role for HRAdmin; Managers can only change Employees in their department)
Click "Save" to confirm the edit.

# API Documentation (Swagger)

When app is running, visit:
https://localhost:7070/swagger
- interactive docs for all /api/employees endpoints, including descriptions pulled from the <summary> comments. (Need to add more summery commnets)
