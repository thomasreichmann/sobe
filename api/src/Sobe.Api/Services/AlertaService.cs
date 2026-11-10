using Sobe.Api.Dtos;
using Sobe.Api.Repositories;

namespace Sobe.Api.Services;

public class AlertaService(IAlertaRepository alertas)
{
    public async Task<List<AlertaResponse>> ListarAsync(bool somenteAbertos, bool somenteFavoritas, int usuarioId)
    {
        int? favoritasDe = somenteFavoritas ? usuarioId : null;
        return (await alertas.ListarAsync(somenteAbertos, favoritasDe)).Select(a => a.ParaResposta()).ToList();
    }
}
