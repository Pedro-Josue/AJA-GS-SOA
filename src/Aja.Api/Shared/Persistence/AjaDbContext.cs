using Aja.Api.Modules.Auth.Infrastructure;
using Aja.Api.Modules.Demandas.Domain;
using Aja.Api.Modules.Demandas.Infrastructure;
using Aja.Api.Modules.Participacoes.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Aja.Api.Shared.Persistence;

public sealed class AjaDbContext(DbContextOptions<AjaDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Demanda> Demandas => Set<Demanda>();
    public DbSet<HistoricoSituacao> HistoricosSituacao => Set<HistoricoSituacao>();
    public DbSet<Apoio> Apoios => Set<Apoio>();
    public DbSet<Comentario> Comentarios => Set<Comentario>();
    public DbSet<CorrespondenciaLegada> CorrespondenciasLegadas => Set<CorrespondenciaLegada>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigureAuth(builder);
        ConfigureDemandas(builder);
        ConfigureParticipacoes(builder);

        var converter = new DateTimeOffsetToBinaryConverter();
        foreach (var entity in builder.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?)))
            property.SetValueConverter(converter);
    }

    private static void ConfigureAuth(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.Alias).HasMaxLength(60).IsRequired();
            entity.HasIndex(x => x.NormalizedEmail).IsUnique().HasDatabaseName("UX_Usuarios_EmailNormalizado");
        });
    }

    private static void ConfigureDemandas(ModelBuilder builder)
    {
        builder.Entity<Demanda>(entity =>
        {
            entity.ToTable("Demandas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Descricao).HasMaxLength(5000).IsRequired();
            entity.Property(x => x.Local).HasMaxLength(500).IsRequired();
            entity.Property(x => x.EvidenciaUrl).HasMaxLength(2048);
            entity.Property(x => x.Cidade).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Bairro).HasMaxLength(100).IsRequired();
            entity.Property(x => x.OrigemLegada).HasMaxLength(50);
            entity.HasIndex(x => new { x.Situacao, x.Categoria, x.Bairro, x.CriadoEm });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AutorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RemovidaPorId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<HistoricoSituacao>(entity =>
        {
            entity.ToTable("HistoricosSituacao");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Justificativa).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.OrigemLegada).HasMaxLength(50);
            entity.HasIndex(x => new { x.DemandaId, x.CriadoEm });
            entity.HasOne<Demanda>().WithMany().HasForeignKey(x => x.DemandaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ResponsavelId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CorrespondenciaLegada>(entity =>
        {
            entity.ToTable("CorrespondenciasLegadas");
            entity.HasKey(x => new { x.OrigemTipo, x.OrigemId, x.DestinoTipo, x.DestinoId });
            entity.Property(x => x.OrigemTipo).HasMaxLength(50);
            entity.Property(x => x.OrigemId).HasMaxLength(100);
            entity.Property(x => x.DestinoTipo).HasMaxLength(50);
            entity.Property(x => x.DestinoId).HasMaxLength(100);
            entity.Property(x => x.Tratamento).HasMaxLength(500).IsRequired();
        });
    }

    private static void ConfigureParticipacoes(ModelBuilder builder)
    {
        builder.Entity<Apoio>(entity =>
        {
            entity.ToTable("DemandaApoios");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrigemLegada).HasMaxLength(50);
            entity.HasIndex(x => new { x.DemandaId, x.UsuarioId }).IsUnique().HasDatabaseName("UX_DemandaApoio_Demanda_Usuario");
            entity.HasOne<Demanda>().WithMany().HasForeignKey(x => x.DemandaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Comentario>(entity =>
        {
            entity.ToTable("Comentarios");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Texto).HasMaxLength(3000).IsRequired();
            entity.Property(x => x.MotivoModeracao).HasMaxLength(2000);
            entity.Property(x => x.TipoLegado).HasMaxLength(50);
            entity.Property(x => x.NaturezaLegada).HasMaxLength(50);
            entity.Property(x => x.OrigemLegada).HasMaxLength(50);
            entity.HasIndex(x => new { x.DemandaId, x.CriadoEm });
            entity.HasOne<Demanda>().WithMany().HasForeignKey(x => x.DemandaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AutorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RemovidoPorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ModeradoPorId).OnDelete(DeleteBehavior.Restrict);
        });
    }

}
