using ManualIA.Api.Models;
using ManualIA.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ManualIA.Api.Data;

public static class BancoInicializador
{
    public static async Task CriarDadosIniciaisAsync(IServiceProvider servicos)
    {
        var banco = servicos.GetRequiredService<AppDbContext>();
        var hasher = servicos.GetRequiredService<IPasswordHasher<Usuario>>();
        var configuracao = servicos.GetRequiredService<IConfiguration>();

        if (!await banco.Perfis.AnyAsync())
        {
            banco.Perfis.AddRange(
                new Perfil { Nome = Perfis.Administrador },
                new Perfil { Nome = Perfis.Funcionario });
            await banco.SaveChangesAsync();
        }

        if (!await banco.Usuarios.AnyAsync())
        {
            var perfil = await banco.Perfis.SingleAsync(x => x.Nome == Perfis.Administrador);
            var usuario = new Usuario
            {
                Nome = "Administrador",
                Email = configuracao["Administrador:Email"] ?? "admin@manualia.com",
                PerfilId = perfil.Id
            };
            usuario.EmailNormalizado = usuario.Email.Trim().ToUpperInvariant();
            usuario.SenhaHash = hasher.HashPassword(usuario,
                configuracao["Administrador:Senha"] ?? "Admin123!");
            banco.Usuarios.Add(usuario);
            await banco.SaveChangesAsync();
        }
    }
}
