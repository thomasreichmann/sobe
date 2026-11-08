using Sobe.Api.Models;

namespace Sobe.Api.Dtos;

public static class Mapeamentos
{
    public static UsuarioResponse ParaResposta(this Usuario u) => new(u.Id, u.Nome, u.Email, u.Perfil);

    public static LinhaResponse ParaResposta(this Linha l) => new(l.Id, l.Numero, l.Nome, l.Cor, l.Operadora);

    public static EstacaoResumoResponse ParaResumo(this Estacao e, bool favorita) =>
        new(e.Id, e.Nome, e.Ordem, e.Linha.ParaResposta(), e.Equipamentos.Count, e.ForaDeServico, e.Acessivel, favorita);

    public static EquipamentoResponse ParaResposta(this Equipamento q)
    {
        var aberto = q.Alertas.FirstOrDefault(a => a.Aberto);
        var alerta = aberto is null ? null : new AlertaDoEquipamento(aberto.Id, aberto.Motivo, aberto.AbertoEm);
        return new(q.Id, q.EstacaoId, q.Tipo, q.Localizacao, q.Status, q.StatusDesde, alerta);
    }

    public static LocalResponse ParaLocal(this Equipamento q) =>
        new(q.Id, q.Tipo, q.Localizacao, q.EstacaoId, q.Estacao.Nome, q.Estacao.Linha.Numero, q.Estacao.Linha.Nome,
            q.Estacao.Linha.Cor);

    public static AlertaResponse ParaResposta(this Alerta a) =>
        new(a.Id, a.Motivo, a.AbertoEm, a.EncerradoEm, a.Equipamento.ParaLocal());

    public static RelatoResponse ParaResposta(this Relato r, int? usuarioId) =>
        new(r.Id, r.Descricao, r.Status, r.CriadoEm, r.AtualizadoEm, r.Usuario.Nome, r.UsuarioId == usuarioId,
            r.Equipamento.ParaLocal());

    public static SensorResponse ParaResposta(this Sensor s) =>
        new(s.Id, s.Codigo, s.Tipo, s.Limite, s.Unidade, s.Ativo, s.Equipamento.ParaLocal());
}
