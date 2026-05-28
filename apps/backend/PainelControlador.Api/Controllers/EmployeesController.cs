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
    private readonly IFichaRegistroService _ficha;

    public EmployeesController(IEmployeeService service, IFichaRegistroService ficha)
    {
        _service = service;
        _ficha = ficha;
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

    [HttpGet("{matricula}/ficha")]
    public async Task<IActionResult> GetFicha(string matricula, CancellationToken ct)
    {
        var ficha = await _ficha.GetAsync(matricula, ct);
        return ficha is null ? NotFound() : Ok(ficha);
    }
}
