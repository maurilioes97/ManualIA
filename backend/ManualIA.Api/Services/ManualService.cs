using ManualIA.Api.Data;
using ManualIA.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ManualIA.Api.Services;

public class ManualService(
    AppDbContext banco,
    ProcessadorDocumento processador,
    IWebHostEnvironment ambiente)
{
    public const long TamanhoMaximo = 10 * 1024 * 1024;

    public async Task<List<ManualResposta>> ListarManuais() => await banco.Manuais
        .Where(x => !x.Excluido).OrderBy(x => x.Titulo)
        .Select(x => new ManualResposta(x.Id, x.Titulo, x.Descricao, x.CriadoEm, x.AtualizadoEm,
            x.Arquivo == null ? null : new ArquivoResposta(x.Arquivo.NomeOriginal, x.Arquivo.TipoArquivo,
                x.Arquivo.TamanhoBytes, x.Arquivo.EnviadoEm, x.Arquivo.QuantidadeChunks)))
        .ToListAsync();

    public async Task<ManualResposta> CadastrarManual(SalvarManualDto dados, int cadastradoPorId)
    {
        var manual = new Manual
        {
            Titulo = dados.Titulo.Trim(),
            Descricao = dados.Descricao.Trim(),
            CadastradoPorId = cadastradoPorId
        };
        banco.Manuais.Add(manual);
        await banco.SaveChangesAsync();
        return Converter(manual);
    }

    public async Task<ResultadoServico<ManualResposta>> EditarManual(int id, SalvarManualDto dados)
    {
        var manual = await banco.Manuais.Include(x => x.Arquivo)
            .SingleOrDefaultAsync(x => x.Id == id && !x.Excluido);
        if (manual is null)
            return ResultadoServico<ManualResposta>.ComErro("Manual não encontrado.", 404);

        manual.Titulo = dados.Titulo.Trim();
        manual.Descricao = dados.Descricao.Trim();
        manual.AtualizadoEm = DateTime.UtcNow;
        await banco.SaveChangesAsync();
        return ResultadoServico<ManualResposta>.ComSucesso(Converter(manual));
    }

    public async Task<ResultadoServico<bool>> ExcluirManual(int id)
    {
        var manual = await banco.Manuais.SingleOrDefaultAsync(x => x.Id == id && !x.Excluido);
        if (manual is null)
            return ResultadoServico<bool>.ComErro("Manual não encontrado.", 404);

        manual.Excluido = true;
        manual.AtualizadoEm = DateTime.UtcNow;
        await banco.SaveChangesAsync();
        return ResultadoServico<bool>.ComSucesso(true);
    }

    public async Task<ResultadoServico<ArquivoResposta>> EnviarArquivo(
        int id, IFormFile arquivo, bool substituir)
    {
        var manual = await banco.Manuais.Include(x => x.Arquivo).Include(x => x.Chunks)
            .SingleOrDefaultAsync(x => x.Id == id && !x.Excluido);
        if (manual is null)
            return ResultadoServico<ArquivoResposta>.ComErro("Manual não encontrado.", 404);
        if (arquivo is null || arquivo.Length == 0)
            return ResultadoServico<ArquivoResposta>.ComErro("Selecione um arquivo.", 400);
        if (arquivo.Length > TamanhoMaximo)
            return ResultadoServico<ArquivoResposta>.ComErro("O arquivo deve ter no máximo 10 MB.", 413);

        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (extensao is not ".pdf" and not ".docx")
            return ResultadoServico<ArquivoResposta>.ComErro("Envie um arquivo PDF ou DOCX.", 415);
        if (manual.Arquivo is not null && !substituir)
            return ResultadoServico<ArquivoResposta>.ComErro(
                "Este manual já possui arquivo. Confirme a substituição.", 409);

        List<TrechoDocumento> trechos;
        try
        {
            await using var leitura = arquivo.OpenReadStream();
            trechos = processador.ExtrairTrechos(leitura, extensao);
        }
        catch
        {
            return ResultadoServico<ArquivoResposta>.ComErro(
                "Não foi possível ler o arquivo. Verifique se ele é válido.", 415);
        }

        var pasta = Path.Combine(ambiente.ContentRootPath, "storage", "manuais");
        Directory.CreateDirectory(pasta);
        var novoCaminho = Path.Combine(pasta, $"{Guid.NewGuid()}{extensao}");
        await using (var destino = File.Create(novoCaminho))
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
            File.Delete(novoCaminho);
            throw;
        }

        if (caminhoAntigo is not null && File.Exists(caminhoAntigo))
            File.Delete(caminhoAntigo);

        return ResultadoServico<ArquivoResposta>.ComSucesso(new ArquivoResposta(
            manual.Arquivo.NomeOriginal,
            manual.Arquivo.TipoArquivo,
            manual.Arquivo.TamanhoBytes,
            manual.Arquivo.EnviadoEm,
            manual.Arquivo.QuantidadeChunks));
    }

    private static ManualResposta Converter(Manual manual) => new(
        manual.Id,
        manual.Titulo,
        manual.Descricao,
        manual.CriadoEm,
        manual.AtualizadoEm,
        manual.Arquivo is null ? null : new ArquivoResposta(
            manual.Arquivo.NomeOriginal,
            manual.Arquivo.TipoArquivo,
            manual.Arquivo.TamanhoBytes,
            manual.Arquivo.EnviadoEm,
            manual.Arquivo.QuantidadeChunks));
}
