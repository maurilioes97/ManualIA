using System.ComponentModel.DataAnnotations;

namespace ManualIA.Api.Models;

public record CriarUsuarioDto(
    [Required, MaxLength(100)] string Nome,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Senha,
    [Required] int PerfilId);
