using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelControlador.Api.Services;

namespace PainelControlador.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _service;

    public EmployeesController(IEmployeeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var employees = await _service.ListAsync(ct);
        return Ok(employees);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var employee = await _service.GetByIdAsync(id, ct);
        return employee is null ? NotFound() : Ok(employee);
    }
}
