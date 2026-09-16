using ManualIA.Api.Data;
using ManualIA.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ManualIA.Api.Services;

public class UsuarioService(AppDbContext banco, IPasswordHasher<Usuario> hasher)
{
    public async Task<List<UsuarioResposta>> ListarUsuarios() => await banco.Usuarios
        .Include(x => x.Perfil).OrderBy(x => x.Nome)
        .Select(x => new UsuarioResposta(x.Id, x.Nome, x.Email, x.PerfilId, x.Perfil.Nome, x.Ativo))
        .ToListAsync();

    public async Task<List<PerfilResposta>> ListarPerfis() => await banco.Perfis
        .OrderBy(x => x.Id).Select(x => new PerfilResposta(x.Id, x.Nome)).ToListAsync();

    public async Task<ResultadoServico<UsuarioResposta>> CadastrarUsuario(
        CriarUsuarioDto dados, int cadastradoPorId)
    {
        var erro = await ValidarDados(dados.Email, dados.PerfilId);
        if (erro is not null) return erro;

        var usuario = new Usuario
        {
            Nome = dados.Nome.Trim(),
            Email = dados.Email.Trim(),
            EmailNormalizado = dados.Email.Trim().ToUpperInvariant(),
            PerfilId = dados.PerfilId,
            CadastradoPorId = cadastradoPorId
        };
        usuario.SenhaHash = hasher.HashPassword(usuario, dados.Senha);
        banco.Usuarios.Add(usuario);
        await banco.SaveChangesAsync();
        await banco.Entry(usuario).Reference(x => x.Perfil).LoadAsync();
        return ResultadoServico<UsuarioResposta>.ComSucesso(Converter(usuario));
    }

    public async Task<ResultadoServico<UsuarioResposta>> EditarUsuario(int id, EditarUsuarioDto dados)
    {
        var usuario = await banco.Usuarios.Include(x => x.Perfil).SingleOrDefaultAsync(x => x.Id == id);
        if (usuario is null)
            return ResultadoServico<UsuarioResposta>.ComErro("Usuário não encontrado.", 404);

        var erro = await ValidarDados(dados.Email, dados.PerfilId, id);
        if (erro is not null) return erro;

        usuario.Nome = dados.Nome.Trim();
        usuario.Email = dados.Email.Trim();
        usuario.EmailNormalizado = dados.Email.Trim().ToUpperInvariant();
        usuario.PerfilId = dados.PerfilId;
        await banco.SaveChangesAsync();
        banco.Entry(usuario).Reference(x => x.Perfil).IsLoaded = false;
        await banco.Entry(usuario).Reference(x => x.Perfil).LoadAsync();
        return ResultadoServico<UsuarioResposta>.ComSucesso(Converter(usuario));
    }

    public async Task<ResultadoServico<bool>> ExcluirUsuario(int id, int usuarioLogadoId)
    {
        if (id == usuarioLogadoId)
            return ResultadoServico<bool>.ComErro("Você não pode excluir a própria conta.", 409);

        var usuario = await banco.Usuarios.FindAsync(id);
        if (usuario is null)
            return ResultadoServico<bool>.ComErro("Usuário não encontrado.", 404);

        usuario.Ativo = false;
        await banco.SaveChangesAsync();
        return ResultadoServico<bool>.ComSucesso(true);
    }

    private async Task<ResultadoServico<UsuarioResposta>?> ValidarDados(
        string email, int perfilId, int? ignorarId = null)
    {
        var normalizado = email.Trim().ToUpperInvariant();
        if (await banco.Usuarios.AnyAsync(x => x.EmailNormalizado == normalizado && x.Id != ignorarId))
            return ResultadoServico<UsuarioResposta>.ComErro("Já existe um usuário com este e-mail.", 409);
        if (!await banco.Perfis.AnyAsync(x => x.Id == perfilId))
            return ResultadoServico<UsuarioResposta>.ComErro("Perfil inválido.", 400);
        return null;
    }

    private static UsuarioResposta Converter(Usuario usuario) => new(
        usuario.Id, usuario.Nome, usuario.Email, usuario.PerfilId, usuario.Perfil.Nome, usuario.Ativo);
}
