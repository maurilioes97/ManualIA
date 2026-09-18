using System.ComponentModel.DataAnnotations;

namespace ManualIA.Api.Models.Dtos;

public record LoginDto(
    [Required, EmailAddress] string Email,
    [Required] string Senha);
