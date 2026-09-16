using System.ComponentModel.DataAnnotations;

namespace ManualIA.Api.Models;

public record LoginDto(
    [Required, EmailAddress] string Email,
    [Required] string Senha);

public record CriarUsuarioDto(
    [Required, MaxLength(100)] string Nome,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Senha,
    [Required] int PerfilId);

public record EditarUsuarioDto(
    [Required, MaxLength(100)] string Nome,
    [Required, EmailAddress] string Email,
    [Required] int PerfilId);

public record UsuarioResposta(int Id, string Nome, string Email, int PerfilId, string Perfil, bool Ativo);

public record SalvarManualDto(
    [Required, MaxLength(150)] string Titulo,
    [Required, MaxLength(1000)] string Descricao);

public record ArquivoResposta(string Nome, string Tipo, long Tamanho, DateTime EnviadoEm, int QuantidadeChunks);

public record ManualResposta(
    int Id,
    string Titulo,
    string Descricao,
    DateTime CriadoEm,
    DateTime AtualizadoEm,
    ArquivoResposta? Arquivo);
