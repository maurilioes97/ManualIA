using System.ComponentModel.DataAnnotations;

namespace ManualIA.Api.Models.Dtos;

public class PerguntaChatDto
{
    [Required(ErrorMessage = "Informe uma pergunta.")]
    [MaxLength(1000, ErrorMessage = "A pergunta deve ter no máximo 1000 caracteres.")]
    public string Pergunta { get; set; } = string.Empty;
}
