using System.Security.Claims;
using ManualIA.Api.Models;
using ManualIA.Api.Models.Dtos;
using ManualIA.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManualIA.Api.Controllers;

[ApiController]
[Route("api/manuais")]
[Authorize(Roles = Perfis.Administrador)]
public class ManuaisController(ManualService manualService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListarManuais() => Ok(await manualService.ListarManuais());

    [HttpPost]
    public async Task<IActionResult> CadastrarManual(SalvarManualDto dados)
    {
        var manual = await manualService.CadastrarManual(dados, ObterUsuarioId());
        return Created("", manual);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditarManual(int id, SalvarManualDto dados)
    {
        var resultado = await manualService.EditarManual(id, dados);
        return resultado.Sucesso
            ? Ok(resultado.Dados)
            : StatusCode(resultado.Status, new { mensagem = resultado.Erro });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> ExcluirManual(int id)
    {
        var resultado = await manualService.ExcluirManual(id);
        return resultado.Sucesso
            ? NoContent()
            : StatusCode(resultado.Status, new { mensagem = resultado.Erro });
    }

    [HttpPost("{id:int}/arquivo")]
    [RequestSizeLimit(ManualService.TamanhoMaximo)]
    public async Task<IActionResult> EnviarArquivo(
        int id, IFormFile arquivo, [FromQuery] bool substituir = false)
    {
        var resultado = await manualService.EnviarArquivo(id, arquivo, substituir);
        return resultado.Sucesso
            ? Ok(resultado.Dados)
            : StatusCode(resultado.Status, new { mensagem = resultado.Erro });
    }

    private int ObterUsuarioId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
