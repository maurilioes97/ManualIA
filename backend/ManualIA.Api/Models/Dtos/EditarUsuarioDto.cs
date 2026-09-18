using System.ComponentModel.DataAnnotations;

namespace ManualIA.Api.Models.Dtos;

public record EditarUsuarioDto(
    [Required, MaxLength(100)] string Nome,
    [Required, EmailAddress] string Email,
    [Required] int PerfilId);
