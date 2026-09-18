using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ManualIA.Api.Models.Processing;
using UglyToad.PdfPig;

namespace ManualIA.Api.Services;

public class ProcessadorDocumento
{
    public List<TrechoDocumento> ExtrairTrechos(Stream arquivo, string extensao)
    {
        var textos = extensao == ".pdf" ? LerPdf(arquivo) : LerDocx(arquivo);
        var trechos = new List<TrechoDocumento>();

        foreach (var (texto, pagina) in textos)
            trechos.AddRange(DividirTexto(texto, pagina));

        if (trechos.Count == 0)
            throw new InvalidDataException("O arquivo não possui texto que possa ser extraído.");

        return trechos;
    }

    private static List<(string Texto, int? Pagina)> LerPdf(Stream arquivo)
    {
        if (!TemAssinatura(arquivo, "%PDF"))
            throw new InvalidDataException("O conteúdo não é um PDF válido.");

        arquivo.Position = 0;
        using var pdf = PdfDocument.Open(arquivo);
        return pdf.GetPages().Select(pagina => (pagina.Text, (int?)pagina.Number)).ToList();
    }

    private static List<(string Texto, int? Pagina)> LerDocx(Stream arquivo)
    {
        if (!TemAssinatura(arquivo, "PK"))
            throw new InvalidDataException("O conteúdo não é um DOCX válido.");

        arquivo.Position = 0;
        using var documento = WordprocessingDocument.Open(arquivo, false);
        var paragrafos = documento.MainDocumentPart?.Document?.Body?
            .Descendants<Paragraph>().Select(x => x.InnerText).Where(x => !string.IsNullOrWhiteSpace(x))
            ?? [];
        return paragrafos.Select(x => (x, (int?)null)).ToList();
    }

    private static bool TemAssinatura(Stream arquivo, string assinatura)
    {
        arquivo.Position = 0;
        var bytes = new byte[assinatura.Length];
        return arquivo.Read(bytes, 0, bytes.Length) == bytes.Length
            && System.Text.Encoding.ASCII.GetString(bytes) == assinatura;
    }

    private static IEnumerable<TrechoDocumento> DividirTexto(string texto, int? pagina)
    {
        texto = string.Join(' ', texto.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        const int tamanho = 1200;
        const int sobreposicao = 200;

        for (var inicio = 0; inicio < texto.Length; inicio += tamanho - sobreposicao)
        {
            var quantidade = Math.Min(tamanho, texto.Length - inicio);
            yield return new TrechoDocumento(texto.Substring(inicio, quantidade), pagina);
            if (inicio + quantidade >= texto.Length) yield break;
        }
    }
}
