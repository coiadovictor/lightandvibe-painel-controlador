using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PainelControlador.Api.Services;

namespace PainelControlador.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/information-types")]
public class InformationTypesController : ControllerBase
{
    private readonly IInformationTypeService _service;

    public InformationTypesController(IInformationTypeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var types = await _service.ListAsync(ct);
        return Ok(types);
    }
}
