using System.Security.Claims;
using ManualIA.Api.Models;
using ManualIA.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManualIA.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = Perfis.Administrador)]
public class UsuariosController(UsuarioService usuarioService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListarUsuarios() => Ok(await usuarioService.ListarUsuarios());

    [HttpGet("perfis")]
    public async Task<IActionResult> ListarPerfis() => Ok(await usuarioService.ListarPerfis());

    [HttpPost]
    public async Task<IActionResult> CadastrarUsuario(CriarUsuarioDto dados)
    {
        var resultado = await usuarioService.CadastrarUsuario(dados, ObterUsuarioId());
        return resultado.Sucesso
            ? Created("", resultado.Dados)
            : StatusCode(resultado.Status, new { mensagem = resultado.Erro });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditarUsuario(int id, EditarUsuarioDto dados)
    {
        var resultado = await usuarioService.EditarUsuario(id, dados);
        return resultado.Sucesso
            ? Ok(resultado.Dados)
            : StatusCode(resultado.Status, new { mensagem = resultado.Erro });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> ExcluirUsuario(int id)
    {
        var resultado = await usuarioService.ExcluirUsuario(id, ObterUsuarioId());
        return resultado.Sucesso
            ? NoContent()
            : StatusCode(resultado.Status, new { mensagem = resultado.Erro });
    }

    private int ObterUsuarioId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
