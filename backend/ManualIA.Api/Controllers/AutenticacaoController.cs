using ManualIA.Api.Models;
using ManualIA.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ManualIA.Api.Controllers;

[ApiController]
[Route("api/autenticacao")]
public class AutenticacaoController(AutenticacaoService autenticacaoService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> FazerLogin(LoginDto dados)
    {
        var resposta = await autenticacaoService.FazerLogin(dados);
        return resposta is null
            ? Unauthorized(new { mensagem = "E-mail ou senha inválidos." })
            : Ok(resposta);
    }
}
