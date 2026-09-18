using ManualIA.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ManualIA.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> opcoes) : DbContext(opcoes)
{
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Manual> Manuais => Set<Manual>();
    public DbSet<ArquivoManual> ArquivosManuais => Set<ArquivoManual>();
    public DbSet<ChunkManual> ChunksManuais => Set<ChunkManual>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Perfil>().HasIndex(x => x.Nome).IsUnique();
        modelo.Entity<Usuario>().HasIndex(x => x.EmailNormalizado).IsUnique();
        modelo.Entity<Usuario>().HasOne(x => x.Perfil).WithMany().HasForeignKey(x => x.PerfilId)
            .OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<Manual>().HasOne<Usuario>().WithMany().HasForeignKey(x => x.CadastradoPorId)
            .OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<Manual>().HasOne(x => x.Arquivo).WithOne(x => x.Manual)
            .HasForeignKey<ArquivoManual>(x => x.ManualId).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<Manual>().HasMany(x => x.Chunks).WithOne(x => x.Manual)
            .HasForeignKey(x => x.ManualId).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<ChunkManual>().HasIndex(x => new { x.ManualId, x.Ordem }).IsUnique();
    }
}
