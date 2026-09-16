using System.Security.Claims;
using ManualIA.Api.Data;
using ManualIA.Api.Models;
using ManualIA.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManualIA.Api.Controllers;

[ApiController]
[Route("api/manuais")]
[Authorize(Roles = Perfis.Administrador)]
public class ManuaisController(AppDbContext banco, ProcessadorDocumento processador, IWebHostEnvironment ambiente)
    : ControllerBase
{
    private const long TamanhoMaximo = 10 * 1024 * 1024;

    [HttpGet]
    public async Task<List<ManualResposta>> ListarManuais() => await banco.Manuais
        .Where(x => !x.Excluido).OrderBy(x => x.Titulo)
        .Select(x => new ManualResposta(x.Id, x.Titulo, x.Descricao, x.CriadoEm, x.AtualizadoEm,
            x.Arquivo == null ? null : new ArquivoResposta(x.Arquivo.NomeOriginal, x.Arquivo.TipoArquivo,
                x.Arquivo.TamanhoBytes, x.Arquivo.EnviadoEm, x.Arquivo.QuantidadeChunks)))
        .ToListAsync();

    [HttpPost]
    public async Task<IActionResult> CriarManual(SalvarManualDto dados)
    {
        var manual = new Manual
        {
            Titulo = dados.Titulo.Trim(),
            Descricao = dados.Descricao.Trim(),
            CadastradoPorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        };
        banco.Manuais.Add(manual);
        await banco.SaveChangesAsync();
        return Created("", Converter(manual));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditarManual(int id, SalvarManualDto dados)
    {
        var manual = await banco.Manuais.Include(x => x.Arquivo)
            .SingleOrDefaultAsync(x => x.Id == id && !x.Excluido);
        if (manual is null) return NotFound(new { mensagem = "Manual não encontrado." });

        manual.Titulo = dados.Titulo.Trim();
        manual.Descricao = dados.Descricao.Trim();
        manual.AtualizadoEm = DateTime.UtcNow;
        await banco.SaveChangesAsync();
        return Ok(Converter(manual));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> ExcluirManual(int id)
    {
        var manual = await banco.Manuais.SingleOrDefaultAsync(x => x.Id == id && !x.Excluido);
        if (manual is null) return NotFound(new { mensagem = "Manual não encontrado." });
        manual.Excluido = true;
        manual.AtualizadoEm = DateTime.UtcNow;
        await banco.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/arquivo")]
    [RequestSizeLimit(TamanhoMaximo)]
    public async Task<IActionResult> EnviarArquivo(int id, IFormFile arquivo, [FromQuery] bool substituir = false)
    {
        var manual = await banco.Manuais.Include(x => x.Arquivo).Include(x => x.Chunks)
            .SingleOrDefaultAsync(x => x.Id == id && !x.Excluido);
        if (manual is null) return NotFound(new { mensagem = "Manual não encontrado." });
        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(new { mensagem = "Selecione um arquivo." });
        if (arquivo.Length > TamanhoMaximo)
            return StatusCode(413, new { mensagem = "O arquivo deve ter no máximo 10 MB." });

        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (extensao is not ".pdf" and not ".docx")
            return StatusCode(415, new { mensagem = "Envie um arquivo PDF ou DOCX." });
        if (manual.Arquivo is not null && !substituir)
            return Conflict(new { mensagem = "Este manual já possui arquivo. Confirme a substituição." });

        List<TrechoDocumento> trechos;
        try
        {
            await using var leitura = arquivo.OpenReadStream();
            trechos = processador.ExtrairTrechos(leitura, extensao);
        }
        catch
        {
            return StatusCode(415, new { mensagem = "Não foi possível ler o arquivo. Verifique se ele é válido." });
        }

        var pasta = Path.Combine(ambiente.ContentRootPath, "storage", "manuais");
        Directory.CreateDirectory(pasta);
        var novoCaminho = Path.Combine(pasta, $"{Guid.NewGuid()}{extensao}");
        await using (var destino = System.IO.File.Create(novoCaminho))
            await arquivo.CopyToAsync(destino);

        var caminhoAntigo = manual.Arquivo?.CaminhoArquivo;
        try
        {
            banco.ChunksManuais.RemoveRange(manual.Chunks);
            manual.Arquivo ??= new ArquivoManual();
            manual.Arquivo.NomeOriginal = Path.GetFileName(arquivo.FileName);
            manual.Arquivo.CaminhoArquivo = novoCaminho;
            manual.Arquivo.TipoArquivo = extensao.TrimStart('.').ToUpperInvariant();
            manual.Arquivo.TamanhoBytes = arquivo.Length;
            manual.Arquivo.EnviadoEm = DateTime.UtcNow;
            manual.Arquivo.QuantidadeChunks = trechos.Count;
            manual.Chunks = trechos.Select((x, indice) => new ChunkManual
            {
                Texto = x.Texto,
                Pagina = x.Pagina,
                Ordem = indice + 1
            }).ToList();
            manual.AtualizadoEm = DateTime.UtcNow;
            await banco.SaveChangesAsync();
        }
        catch
        {
            System.IO.File.Delete(novoCaminho);
            throw;
        }

        if (caminhoAntigo is not null && System.IO.File.Exists(caminhoAntigo))
            System.IO.File.Delete(caminhoAntigo);

        return Ok(new ArquivoResposta(manual.Arquivo.NomeOriginal, manual.Arquivo.TipoArquivo,
            manual.Arquivo.TamanhoBytes, manual.Arquivo.EnviadoEm, manual.Arquivo.QuantidadeChunks));
    }

    private static ManualResposta Converter(Manual x) => new(x.Id, x.Titulo, x.Descricao, x.CriadoEm,
        x.AtualizadoEm, x.Arquivo is null ? null : new ArquivoResposta(x.Arquivo.NomeOriginal,
            x.Arquivo.TipoArquivo, x.Arquivo.TamanhoBytes, x.Arquivo.EnviadoEm, x.Arquivo.QuantidadeChunks));
}
