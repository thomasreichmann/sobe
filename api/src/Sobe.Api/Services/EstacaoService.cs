using Sobe.Api.Dtos;
using Sobe.Api.Infra;
using Sobe.Api.Models;
using Sobe.Api.Repositories;

namespace Sobe.Api.Services;

public class EstacaoService(IEstacaoRepository estacoes, IFavoritoRepository favoritos)
{
    public async Task<List<LinhaResponse>> ListarLinhasAsync() =>
        (await estacoes.ListarLinhasAsync()).Select(l => l.ParaResposta()).ToList();

    public async Task<List<EstacaoResumoResponse>> ListarAsync(int? linhaId, string? busca, bool somenteFavoritas, int? usuarioId)
    {
        IEnumerable<Estacao> lista = await estacoes.ListarAsync(linhaId);
        var favoritas = await FavoritasAsync(usuarioId);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = Texto.SemAcento(busca);
            lista = lista.Where(e => Texto.SemAcento(e.Nome).Contains(termo));
        }
        if (somenteFavoritas) lista = lista.Where(e => favoritas.Contains(e.Id));

        return lista.Select(e => e.ParaResumo(favoritas.Contains(e.Id))).ToList();
    }

    public async Task<EstacaoDetalheResponse> BuscarAsync(int id, int? usuarioId)
    {
        var estacao = await estacoes.BuscarAsync(id) ?? throw new NaoEncontrado("Estação não encontrada.");
        var favoritas = await FavoritasAsync(usuarioId);
        var alternativa = estacao.Acessivel ? null : await AlternativaAsync(estacao);

        return new EstacaoDetalheResponse(
            estacao.Id, estacao.Nome, estacao.Ordem, estacao.Linha.ParaResposta(), estacao.Acessivel,
            favoritas.Contains(estacao.Id), estacao.Equipamentos.Select(q => q.ParaResposta()).ToList(), alternativa);
    }

    public async Task<EstacaoDetalheResponse> CriarAsync(EstacaoRequest pedido)
    {
        var nome = pedido.Nome.Trim();
        await ValidarAsync(pedido.LinhaId, nome, pedido.Ordem, ignorarId: null);

        var estacao = new Estacao { LinhaId = pedido.LinhaId, Nome = nome, Ordem = pedido.Ordem };
        estacoes.Adicionar(estacao);
        await estacoes.SalvarAsync();
        return await BuscarAsync(estacao.Id, usuarioId: null);
    }

    public async Task<EstacaoDetalheResponse> AtualizarAsync(int id, EstacaoRequest pedido)
    {
        var estacao = await estacoes.BuscarAsync(id) ?? throw new NaoEncontrado("Estação não encontrada.");
        var nome = pedido.Nome.Trim();
        await ValidarAsync(pedido.LinhaId, nome, pedido.Ordem, ignorarId: id);

        estacao.LinhaId = pedido.LinhaId;
        estacao.Nome = nome;
        estacao.Ordem = pedido.Ordem;
        await estacoes.SalvarAsync();
        return await BuscarAsync(id, usuarioId: null);
    }

    public async Task RemoverAsync(int id)
    {
        var estacao = await estacoes.BuscarAsync(id) ?? throw new NaoEncontrado("Estação não encontrada.");
        estacoes.Remover(estacao);
        await estacoes.SalvarAsync();
    }

    private async Task ValidarAsync(int linhaId, string nome, int ordem, int? ignorarId)
    {
        if (await estacoes.BuscarLinhaAsync(linhaId) is null) throw new NaoEncontrado("Linha não encontrada.");
        if (await estacoes.NomeEmUsoAsync(linhaId, nome, ignorarId)) throw new Conflito("Esta linha já tem uma estação com este nome.");
        if (await estacoes.OrdemEmUsoAsync(linhaId, ordem, ignorarId)) throw new Conflito("Esta linha já tem uma estação nesta posição.");
    }

    private async Task<HashSet<int>> FavoritasAsync(int? usuarioId)
    {
        if (usuarioId is null) return [];
        return await favoritos.ListarEstacoesAsync(usuarioId.Value);
    }

    private async Task<AlternativaResponse?> AlternativaAsync(Estacao estacao)
    {
        var daLinha = await estacoes.ListarAsync(estacao.LinhaId);
        var maisProxima = daLinha
            .Where(e => e.Id != estacao.Id && e.Acessivel)
            .OrderBy(e => Math.Abs(e.Ordem - estacao.Ordem)).ThenBy(e => e.Ordem)
            .FirstOrDefault();
        if (maisProxima is null) return null;
        return new AlternativaResponse(maisProxima.Id, maisProxima.Nome, Math.Abs(maisProxima.Ordem - estacao.Ordem));
    }
}
