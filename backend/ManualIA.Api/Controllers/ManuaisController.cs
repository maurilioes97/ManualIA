using System.Security.Claims;
using ManualIA.Api.Models;
using ManualIA.Api.Models.Dtos;
using ManualIA.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManualIA.Api.Controllers;

[ApiController]
[Route("api/manuais")]
[Authorize]
public class ManuaisController(ManualService manualService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListarManuais([FromQuery] string? termo = null)
    {
        var somenteDisponiveis = !User.IsInRole(Perfis.Administrador);
        return Ok(await manualService.ListarManuais(termo, somenteDisponiveis));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObterManual(int id)
    {
        var somenteDisponiveis = !User.IsInRole(Perfis.Administrador);
        var resultado = await manualService.ObterManual(id, somenteDisponiveis);
        return resultado.Sucesso
            ? Ok(resultado.Dados)
            : StatusCode(resultado.Status, new { mensagem = resultado.Erro });
    }

    [HttpGet("{id:int}/arquivo/visualizacao")]
    public async Task<IActionResult> VisualizarArquivo(int id)
    {
        var somenteDisponiveis = !User.IsInRole(Perfis.Administrador);
        var resultado = await manualService.ObterArquivoParaVisualizacao(id, somenteDisponiveis);
        if (!resultado.Sucesso)
            return StatusCode(resultado.Status, new { mensagem = resultado.Erro });

        var arquivo = resultado.Dados!;
        var stream = new FileStream(
            arquivo.Caminho, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, "application/pdf", enableRangeProcessing: true);
    }

    [HttpPost]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> CadastrarManual(SalvarManualDto dados)
    {
        var manual = await manualService.CadastrarManual(dados, ObterUsuarioId());
        return Created("", manual);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> EditarManual(int id, SalvarManualDto dados)
    {
        var resultado = await manualService.EditarManual(id, dados);
        return resultado.Sucesso
            ? Ok(resultado.Dados)
            : StatusCode(resultado.Status, new { mensagem = resultado.Erro });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> ExcluirManual(int id)
    {
        var resultado = await manualService.ExcluirManual(id);
        return resultado.Sucesso
            ? NoContent()
            : StatusCode(resultado.Status, new { mensagem = resultado.Erro });
    }

    [HttpPost("{id:int}/arquivo")]
    [Authorize(Roles = Perfis.Administrador)]
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
