using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ManualIA.Api.Data;
using ManualIA.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ManualIA.Api.Controllers;

[ApiController]
[Route("api/autenticacao")]
public class AutenticacaoController(
    AppDbContext banco,
    IPasswordHasher<Usuario> hasher,
    IConfiguration configuracao) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> FazerLogin(LoginDto dados)
    {
        var email = dados.Email.Trim().ToUpperInvariant();
        var usuario = await banco.Usuarios.Include(x => x.Perfil)
            .SingleOrDefaultAsync(x => x.EmailNormalizado == email && x.Ativo);

        if (usuario is null || hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, dados.Senha)
            == PasswordVerificationResult.Failed)
            return Unauthorized(new { mensagem = "E-mail ou senha inválidos." });

        return Ok(new
        {
            token = GerarToken(usuario),
            usuario = new UsuarioResposta(usuario.Id, usuario.Nome, usuario.Email,
                usuario.PerfilId, usuario.Perfil.Nome, usuario.Ativo)
        });
    }

    private string GerarToken(Usuario usuario)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracao["Jwt:Chave"]!));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Perfil.Nome)
        };
        var token = new JwtSecurityToken(
            issuer: configuracao["Jwt:Emissor"],
            audience: configuracao["Jwt:Publico"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
