using Sobe.Api.Dtos;
using Sobe.Api.Infra;
using Sobe.Api.Models;
using Sobe.Api.Repositories;

namespace Sobe.Api.Services;

public class RelatoService(IRelatoRepository relatos, IEquipamentoRepository equipamentos, TimeProvider relogio)
{
    public async Task<List<RelatoResponse>> ListarAsync(int usuarioId, bool admin, StatusRelato? status)
    {
        int? autor = admin ? null : usuarioId;
        return (await relatos.ListarAsync(autor, status)).Select(r => r.ParaResposta(usuarioId)).ToList();
    }

    public async Task<RelatoResponse> BuscarAsync(int id, int usuarioId, bool admin) =>
        (await ObterAsync(id, usuarioId, admin)).ParaResposta(usuarioId);

    public async Task<RelatoResponse> CriarAsync(int usuarioId, NovoRelatoRequest pedido)
    {
        if (await equipamentos.BuscarAsync(pedido.EquipamentoId) is null) throw new NaoEncontrado("Equipamento não encontrado.");

        var agora = relogio.GetUtcNow().UtcDateTime;
        var relato = new Relato
        {
            UsuarioId = usuarioId,
            EquipamentoId = pedido.EquipamentoId,
            Descricao = pedido.Descricao.Trim(),
            CriadoEm = agora,
            AtualizadoEm = agora,
        };
        relatos.Adicionar(relato);
        await relatos.SalvarAsync();
        return (await relatos.BuscarAsync(relato.Id))!.ParaResposta(usuarioId);
    }

    public async Task<RelatoResponse> EditarAsync(int id, int usuarioId, EditarRelatoRequest pedido)
    {
        var relato = await ObterAsync(id, usuarioId, admin: false);
        if (relato.Status != StatusRelato.Aberto) throw new RegraViolada("Só dá para editar um relato enquanto ele está aberto.");

        relato.Descricao = pedido.Descricao.Trim();
        relato.AtualizadoEm = relogio.GetUtcNow().UtcDateTime;
        await relatos.SalvarAsync();
        return relato.ParaResposta(usuarioId);
    }

    public async Task<RelatoResponse> MudarStatusAsync(int id, int usuarioId, StatusRelato status)
    {
        var relato = await ObterAsync(id, usuarioId, admin: true);
        relato.Status = status;
        relato.AtualizadoEm = relogio.GetUtcNow().UtcDateTime;
        await relatos.SalvarAsync();
        return relato.ParaResposta(usuarioId);
    }

    public async Task RemoverAsync(int id, int usuarioId, bool admin)
    {
        var relato = await ObterAsync(id, usuarioId, admin);
        if (!admin && relato.Status != StatusRelato.Aberto) throw new RegraViolada("Só dá para apagar um relato enquanto ele está aberto.");

        relatos.Remover(relato);
        await relatos.SalvarAsync();
    }

    // Quem não é o autor recebe o mesmo 404 de um relato que não existe, para não revelar os ids dos outros.
    private async Task<Relato> ObterAsync(int id, int usuarioId, bool admin)
    {
        var relato = await relatos.BuscarAsync(id);
        if (relato is null || (!admin && relato.UsuarioId != usuarioId)) throw new NaoEncontrado("Relato não encontrado.");
        return relato;
    }
}
