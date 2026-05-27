using PainelControlador.Api.Dtos;

namespace PainelControlador.Api.Services;

public interface IInformationTypeService
{
    Task<IReadOnlyList<InformationTypeDto>> ListAsync(CancellationToken ct = default);
}
