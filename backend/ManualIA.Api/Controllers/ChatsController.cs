using System.Security.Claims;
using ManualIA.Api.Models;
using ManualIA.Api.Models.Dtos;
using ManualIA.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManualIA.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = Perfis.Funcionario)]
public class ChatsController(ChatService chatService) : ControllerBase
{
    [HttpPost("manuais/{manualId:int}/chats")]
    public async Task<IActionResult> AbrirChat(int manualId)
    {
        var resultado = await chatService.AbrirSessao(manualId, ObterUsuarioId());
        return resultado.Sucesso
            ? Created($"/api/chats/{resultado.Dados!.Id}", resultado.Dados)
            : StatusCode(resultado.Status, new { mensagem = resultado.Erro });
    }

    [HttpPost("chats/{sessaoId:int}/perguntas")]
    public async Task<IActionResult> FazerPergunta(int sessaoId, PerguntaChatDto dados)
    {
        var resultado = await chatService.EnviarPergunta(
            sessaoId, ObterUsuarioId(), dados.Pergunta, HttpContext.RequestAborted);
        return resultado.Sucesso
            ? Ok(resultado.Dados)
            : StatusCode(resultado.Status, new { mensagem = resultado.Erro });
    }

    private int ObterUsuarioId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
