using System.Security.Claims;
using ManualIA.Api.Data;
using ManualIA.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManualIA.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = Perfis.Administrador)]
public class UsuariosController(AppDbContext banco, IPasswordHasher<Usuario> hasher) : ControllerBase
{
    [HttpGet]
    public async Task<List<UsuarioResposta>> ListarUsuarios() => await banco.Usuarios
        .Include(x => x.Perfil).OrderBy(x => x.Nome)
        .Select(x => new UsuarioResposta(x.Id, x.Nome, x.Email, x.PerfilId, x.Perfil.Nome, x.Ativo))
        .ToListAsync();

    [HttpGet("perfis")]
    public async Task<IActionResult> ListarPerfis() => Ok(await banco.Perfis.OrderBy(x => x.Id)
        .Select(x => new { x.Id, x.Nome }).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> CriarUsuario(CriarUsuarioDto dados)
    {
        var erro = await ValidarDados(dados.Email, dados.PerfilId);
        if (erro is not null) return erro;

        var usuario = new Usuario
        {
            Nome = dados.Nome.Trim(),
            Email = dados.Email.Trim(),
            EmailNormalizado = dados.Email.Trim().ToUpperInvariant(),
            PerfilId = dados.PerfilId,
            CadastradoPorId = ObterUsuarioId()
        };
        usuario.SenhaHash = hasher.HashPassword(usuario, dados.Senha);
        banco.Usuarios.Add(usuario);
        await banco.SaveChangesAsync();
        await banco.Entry(usuario).Reference(x => x.Perfil).LoadAsync();

        return Created("", new UsuarioResposta(usuario.Id, usuario.Nome, usuario.Email,
            usuario.PerfilId, usuario.Perfil.Nome, usuario.Ativo));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditarUsuario(int id, EditarUsuarioDto dados)
    {
        var usuario = await banco.Usuarios.Include(x => x.Perfil).SingleOrDefaultAsync(x => x.Id == id);
        if (usuario is null) return NotFound(new { mensagem = "Usuário não encontrado." });

        var erro = await ValidarDados(dados.Email, dados.PerfilId, id);
        if (erro is not null) return erro;

        usuario.Nome = dados.Nome.Trim();
        usuario.Email = dados.Email.Trim();
        usuario.EmailNormalizado = dados.Email.Trim().ToUpperInvariant();
        usuario.PerfilId = dados.PerfilId;
        await banco.SaveChangesAsync();
        banco.Entry(usuario).Reference(x => x.Perfil).IsLoaded = false;
        await banco.Entry(usuario).Reference(x => x.Perfil).LoadAsync();
        return Ok(new UsuarioResposta(usuario.Id, usuario.Nome, usuario.Email,
            usuario.PerfilId, usuario.Perfil.Nome, usuario.Ativo));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> ExcluirUsuario(int id)
    {
        if (id == ObterUsuarioId())
            return Conflict(new { mensagem = "Você não pode excluir a própria conta." });

        var usuario = await banco.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound(new { mensagem = "Usuário não encontrado." });

        usuario.Ativo = false;
        await banco.SaveChangesAsync();
        return NoContent();
    }

    private async Task<IActionResult?> ValidarDados(string email, int perfilId, int? ignorarId = null)
    {
        var normalizado = email.Trim().ToUpperInvariant();
        if (await banco.Usuarios.AnyAsync(x => x.EmailNormalizado == normalizado && x.Id != ignorarId))
            return Conflict(new { mensagem = "Já existe um usuário com este e-mail." });
        if (!await banco.Perfis.AnyAsync(x => x.Id == perfilId))
            return BadRequest(new { mensagem = "Perfil inválido." });
        return null;
    }

    private int ObterUsuarioId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
