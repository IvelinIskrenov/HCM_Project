using HCM_Project.Data;
using HCM_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCM_Project.Controllers
{
    [ApiController]               // marks it as an API controller
    [Route("api/[controller]")]   // routes to /api/employees
    public class EmployeesApiController : ControllerBase
    {
        private readonly HcmContext _context;
        public EmployeesApiController(HcmContext context) => _context = context;

        /// <summary>Get all employees.</summary>
        [HttpGet]
        [Authorize(Roles = "HRAdmin,Manager,Employee")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetAll() =>
            Ok(await _context.Employees.ToListAsync());

        /// <summary>Get one by ID.</summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "HRAdmin,Manager,Employee")]
        public async Task<ActionResult<Employee>> GetById(int id)
        {
            var emp = await _context.Employees.FindAsync(id);
            if (emp == null) return NotFound();
            return Ok(emp);
        }

        /// <summary>Create a new employee.</summary>
        [HttpPost]
        [Authorize(Roles = "HRAdmin,Manager")]
        public async Task<ActionResult<Employee>> Create(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
        }

        /// <summary>Update an existing employee.</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "HRAdmin,Manager")]
        public async Task<IActionResult> Update(int id, Employee employee)
        {
            if (id != employee.Id) return BadRequest();
            _context.Entry(employee).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Employees.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }
            return NoContent();
        }

        /// <summary>Delete an employee.</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "HRAdmin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var emp = await _context.Employees.FindAsync(id);
            if (emp == null) return NotFound();
            _context.Employees.Remove(emp);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
