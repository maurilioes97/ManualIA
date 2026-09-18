using System.ComponentModel.DataAnnotations;

namespace ManualIA.Api.Models.Dtos;

public record SalvarManualDto(
    [Required, MaxLength(150)] string Titulo,
    [Required, MaxLength(1000)] string Descricao);
