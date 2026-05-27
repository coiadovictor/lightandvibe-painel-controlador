using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public class InformationTypeService : IInformationTypeService
{
    private readonly ILogger<InformationTypeService> _logger;

    public InformationTypeService(ILogger<InformationTypeService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<InformationTypeDto>> ListAsync(CancellationToken ct = default)
    {
        // TODO: mapear tabela de tipos de informação quando definida no schema do Supabase.
        _logger.LogDebug("InformationTypeService: ainda sem tabela mapeada.");
        return Task.FromResult<IReadOnlyList<InformationTypeDto>>([]);
    }
}
