namespace ManualIA.Api.Models;

public static class Perfis
{
    public const string Administrador = "Administrador";
    public const string Funcionario = "Funcionário";
}

public class Perfil
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
}

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EmailNormalizado { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public int PerfilId { get; set; }
    public Perfil Perfil { get; set; } = null!;
    public int? CadastradoPorId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public bool Ativo { get; set; } = true;
}

public class Manual
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int CadastradoPorId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    public bool Excluido { get; set; }
    public ArquivoManual? Arquivo { get; set; }
    public List<ChunkManual> Chunks { get; set; } = [];
}

public class ArquivoManual
{
    public int Id { get; set; }
    public int ManualId { get; set; }
    public Manual Manual { get; set; } = null!;
    public string NomeOriginal { get; set; } = string.Empty;
    public string CaminhoArquivo { get; set; } = string.Empty;
    public string TipoArquivo { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }
    public DateTime EnviadoEm { get; set; } = DateTime.UtcNow;
    public int QuantidadeChunks { get; set; }
}

public class ChunkManual
{
    public int Id { get; set; }
    public int ManualId { get; set; }
    public Manual Manual { get; set; } = null!;
    public string Texto { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public int? Pagina { get; set; }
}
