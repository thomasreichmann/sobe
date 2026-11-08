using Microsoft.EntityFrameworkCore;
using Sobe.Api.Infra;
using Sobe.Api.Models;

namespace Sobe.Api.Data;

public class SobeDbContext(DbContextOptions<SobeDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Linha> Linhas => Set<Linha>();
    public DbSet<Estacao> Estacoes => Set<Estacao>();
    public DbSet<Equipamento> Equipamentos => Set<Equipamento>();
    public DbSet<Sensor> Sensores => Set<Sensor>();
    public DbSet<Leitura> Leituras => Set<Leitura>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<Favorito> Favoritos => Set<Favorito>();
    public DbSet<Relato> Relatos => Set<Relato>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configuracao)
    {
        configuracao.Properties<DateTime>().HaveConversion<UtcConverter>();
        configuracao.Properties<Perfil>().HaveConversion<EnumTextoConverter<Perfil>>();
        configuracao.Properties<TipoEquipamento>().HaveConversion<EnumTextoConverter<TipoEquipamento>>();
        configuracao.Properties<StatusEquipamento>().HaveConversion<EnumTextoConverter<StatusEquipamento>>();
        configuracao.Properties<TipoSensor>().HaveConversion<EnumTextoConverter<TipoSensor>>();
        configuracao.Properties<StatusRelato>().HaveConversion<EnumTextoConverter<StatusRelato>>();
    }

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Usuario>(e =>
        {
            e.ToTable("TB_USUARIO");
            e.Property(x => x.Id).HasColumnName("ID_USUARIO");
            e.Property(x => x.Nome).HasColumnName("NOME").HasMaxLength(120);
            e.Property(x => x.Email).HasColumnName("EMAIL").HasMaxLength(160);
            e.Property(x => x.SenhaHash).HasColumnName("SENHA_HASH").HasMaxLength(200);
            e.Property(x => x.Perfil).HasColumnName("PERFIL").HasMaxLength(10);
            e.Property(x => x.CriadoEm).HasColumnName("CRIADO_EM");
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelo.Entity<Linha>(e =>
        {
            e.ToTable("TB_LINHA");
            e.Property(x => x.Id).HasColumnName("ID_LINHA");
            e.Property(x => x.Numero).HasColumnName("NUMERO");
            e.Property(x => x.Nome).HasColumnName("NOME").HasMaxLength(40);
            e.Property(x => x.Cor).HasColumnName("COR").HasMaxLength(7);
            e.Property(x => x.Operadora).HasColumnName("OPERADORA").HasMaxLength(60);
            e.HasIndex(x => x.Numero).IsUnique();
        });

        modelo.Entity<Estacao>(e =>
        {
            e.ToTable("TB_ESTACAO");
            e.Property(x => x.Id).HasColumnName("ID_ESTACAO");
            e.Property(x => x.LinhaId).HasColumnName("ID_LINHA");
            e.Property(x => x.Nome).HasColumnName("NOME").HasMaxLength(80);
            e.Property(x => x.Ordem).HasColumnName("ORDEM");
            e.HasOne(x => x.Linha).WithMany(l => l.Estacoes).HasForeignKey(x => x.LinhaId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.LinhaId, x.Nome }).IsUnique();
            e.HasIndex(x => new { x.LinhaId, x.Ordem }).IsUnique();
        });

        modelo.Entity<Equipamento>(e =>
        {
            e.ToTable("TB_EQUIPAMENTO");
            e.Property(x => x.Id).HasColumnName("ID_EQUIPAMENTO");
            e.Property(x => x.EstacaoId).HasColumnName("ID_ESTACAO");
            e.Property(x => x.Tipo).HasColumnName("TIPO").HasMaxLength(20);
            e.Property(x => x.Localizacao).HasColumnName("LOCALIZACAO").HasMaxLength(120);
            e.Property(x => x.Status).HasColumnName("STATUS").HasMaxLength(12);
            e.Property(x => x.StatusDesde).HasColumnName("STATUS_DESDE");
            e.HasOne(x => x.Estacao).WithMany(s => s.Equipamentos).HasForeignKey(x => x.EstacaoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelo.Entity<Sensor>(e =>
        {
            e.ToTable("TB_SENSOR");
            e.Property(x => x.Id).HasColumnName("ID_SENSOR");
            e.Property(x => x.EquipamentoId).HasColumnName("ID_EQUIPAMENTO");
            e.Property(x => x.Tipo).HasColumnName("TIPO").HasMaxLength(12);
            e.Property(x => x.Codigo).HasColumnName("CODIGO").HasMaxLength(40);
            e.Property(x => x.Limite).HasColumnName("LIMITE");
            e.Property(x => x.Unidade).HasColumnName("UNIDADE").HasMaxLength(10);
            e.Property(x => x.Ativo).HasColumnName("ATIVO").HasConversion<int>();
            e.HasOne(x => x.Equipamento).WithMany(q => q.Sensores).HasForeignKey(x => x.EquipamentoId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Codigo).IsUnique();
            e.HasIndex(x => new { x.EquipamentoId, x.Tipo }).IsUnique();
        });

        modelo.Entity<Leitura>(e =>
        {
            e.ToTable("TB_LEITURA");
            e.Property(x => x.Id).HasColumnName("ID_LEITURA");
            e.Property(x => x.SensorId).HasColumnName("ID_SENSOR");
            e.Property(x => x.Valor).HasColumnName("VALOR");
            e.Property(x => x.RegistradaEm).HasColumnName("REGISTRADA_EM");
            e.HasOne(x => x.Sensor).WithMany().HasForeignKey(x => x.SensorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelo.Entity<Alerta>(e =>
        {
            e.ToTable("TB_ALERTA");
            e.Property(x => x.Id).HasColumnName("ID_ALERTA");
            e.Property(x => x.EquipamentoId).HasColumnName("ID_EQUIPAMENTO");
            e.Property(x => x.SensorId).HasColumnName("ID_SENSOR");
            e.Property(x => x.Motivo).HasColumnName("MOTIVO").HasMaxLength(200);
            e.Property(x => x.AbertoEm).HasColumnName("ABERTO_EM");
            e.Property(x => x.EncerradoEm).HasColumnName("ENCERRADO_EM");
            e.HasOne(x => x.Equipamento).WithMany(q => q.Alertas).HasForeignKey(x => x.EquipamentoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Sensor).WithMany().HasForeignKey(x => x.SensorId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelo.Entity<Favorito>(e =>
        {
            e.ToTable("TB_FAVORITO");
            e.HasKey(x => new { x.UsuarioId, x.EstacaoId });
            e.Property(x => x.UsuarioId).HasColumnName("ID_USUARIO");
            e.Property(x => x.EstacaoId).HasColumnName("ID_ESTACAO");
            e.Property(x => x.CriadoEm).HasColumnName("CRIADO_EM");
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Estacao).WithMany().HasForeignKey(x => x.EstacaoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelo.Entity<Relato>(e =>
        {
            e.ToTable("TB_RELATO");
            e.Property(x => x.Id).HasColumnName("ID_RELATO");
            e.Property(x => x.UsuarioId).HasColumnName("ID_USUARIO");
            e.Property(x => x.EquipamentoId).HasColumnName("ID_EQUIPAMENTO");
            e.Property(x => x.Descricao).HasColumnName("DESCRICAO").HasMaxLength(500);
            e.Property(x => x.Status).HasColumnName("STATUS").HasMaxLength(12);
            e.Property(x => x.CriadoEm).HasColumnName("CRIADO_EM");
            e.Property(x => x.AtualizadoEm).HasColumnName("ATUALIZADO_EM");
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Equipamento).WithMany().HasForeignKey(x => x.EquipamentoId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
