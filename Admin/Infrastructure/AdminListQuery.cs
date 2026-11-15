using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace Shop.Admin.Infrastructure;

/// <summary>Shared bounds and response helpers for operational Admin lists and Excel exports.</summary>
public static class AdminListQuery
{
    public const int DefaultPageSize = 50;
    public const int MaximumPageSize = 100;
    public const int MaximumPage = 100_000;
    public const int MaximumSearchLength = 100;
    public const int ExportBatchSize = 1_000;

    public static bool TryCreate(int page, int? pageSize, string? search, out AdminPageRequest request)
    {
        int size = pageSize ?? DefaultPageSize;
        string normalizedSearch = NormalizeSearch(search);
        long offset = ((long)page - 1) * size;

        if (page is < 1 or > MaximumPage
            || size is < 1 or > MaximumPageSize
            || offset > int.MaxValue
            || normalizedSearch.Length > MaximumSearchLength
            || normalizedSearch.Any(char.IsControl))
        {
            request = default;
            return false;
        }

        request = new AdminPageRequest(page, size, (int)offset, normalizedSearch);
        return true;
    }

    public static string NormalizeSearch(string? search) => search?.Trim() ?? string.Empty;

    public static bool HasNext(int page, int pageSize, long total) => (long)page * pageSize < total;

    public static AdminPageResult<T> CreateResult<T>(IReadOnlyCollection<T> items, int total, AdminPageRequest request) =>
        new(items, total, request.Page, request.PageSize, HasNext(request.Page, request.PageSize, total));

    public static AdminPageResult<T> CreateLookaheadResult<T>(IReadOnlyCollection<T> items, bool hasNext,
        AdminPageRequest request) => new(items, null, request.Page, request.PageSize, hasNext);

}

public readonly record struct AdminPageRequest(int Page, int PageSize, int Offset, string Search);

public sealed record AdminPageResult<T>(IReadOnlyCollection<T> Items, int? Total, int Page, int PageSize, bool HasNext);

public sealed class AdminExcelFileResult(string fileName, string sheetName,
    Func<AdminExcelWorksheetWriter, CancellationToken, Task> writeRows) : Microsoft.AspNetCore.Mvc.IActionResult
{
    private const string SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PackageRelationshipNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";

    public async Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context)
    {
        HttpResponse response = context.HttpContext.Response;
        response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        response.Headers.ContentDisposition = $"attachment; filename*=UTF-8''{Uri.EscapeDataString(fileName)}";

        using var archive = new ZipArchive(response.Body, ZipArchiveMode.Create, leaveOpen: true);
        using var worksheet = new AdminExcelWorksheetWriter(archive, sheetName, SpreadsheetNamespace);
        await writeRows(worksheet, context.HttpContext.RequestAborted);
        worksheet.Complete();

        AddXmlEntry(archive, "[Content_Types].xml", new XElement("Types",
            new XAttribute("xmlns", "http://schemas.openxmlformats.org/package/2006/content-types"),
            new XElement("Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
            new XElement("Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
            new XElement("Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
            Enumerable.Range(1, worksheet.SheetCount).Select(index => new XElement("Override",
                new XAttribute("PartName", $"/xl/worksheets/sheet{index}.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")))));
        AddXmlEntry(archive, "_rels/.rels", new XElement(XName.Get("Relationships", PackageRelationshipNamespace),
            new XElement(XName.Get("Relationship", PackageRelationshipNamespace), new XAttribute("Id", "rId1"),
                new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"), new XAttribute("Target", "xl/workbook.xml"))));
        AddXmlEntry(archive, "xl/workbook.xml", new XElement(XName.Get("workbook", SpreadsheetNamespace),
            new XAttribute(XNamespace.Xmlns + "r", RelationshipNamespace),
            new XElement(XName.Get("sheets", SpreadsheetNamespace), Enumerable.Range(1, worksheet.SheetCount).Select(index =>
                new XElement(XName.Get("sheet", SpreadsheetNamespace), new XAttribute("name", worksheet.SheetCount == 1 ? sheetName : $"{sheetName} {index}"),
                    new XAttribute("sheetId", index), new XAttribute(XName.Get("id", RelationshipNamespace), $"rId{index}"))))));
        AddXmlEntry(archive, "xl/_rels/workbook.xml.rels", new XElement(XName.Get("Relationships", PackageRelationshipNamespace),
            Enumerable.Range(1, worksheet.SheetCount).Select(index => new XElement(XName.Get("Relationship", PackageRelationshipNamespace),
                new XAttribute("Id", $"rId{index}"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                new XAttribute("Target", $"worksheets/sheet{index}.xml")))));
        await response.Body.FlushAsync(context.HttpContext.RequestAborted);
    }

    private static void AddXmlEntry(ZipArchive archive, string name, XElement document)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using Stream stream = entry.Open();
        using XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new System.Text.UTF8Encoding(false) });
        document.Save(writer);
    }
}

public sealed class AdminExcelWorksheetWriter : IDisposable
{
    private const int MaximumRowsPerSheet = 1_048_576;
    private readonly ZipArchive _archive;
    private readonly string _sheetName;
    private readonly string _spreadsheetNamespace;
    private XmlWriter? _writer;
    private object?[]? _headerValues;
    private int _rowsOnSheet;
    private bool _completed;

    internal AdminExcelWorksheetWriter(ZipArchive archive, string sheetName, string spreadsheetNamespace)
    {
        _archive = archive;
        _sheetName = sheetName;
        _spreadsheetNamespace = spreadsheetNamespace;
        StartSheet();
    }

    public int SheetCount { get; private set; }

    public Task WriteRowAsync(CancellationToken cancellationToken, params object?[] values)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_completed) throw new InvalidOperationException("The Excel worksheet is already complete.");
        if (values.Length > 16_384) throw new ArgumentOutOfRangeException(nameof(values));
        if (_rowsOnSheet == MaximumRowsPerSheet)
        {
            EndSheet();
            StartSheet();
        }

        if (SheetCount == 1 && _rowsOnSheet == 0)
            _headerValues = values.ToArray();
        WriteValues(values);
        return Task.CompletedTask;
    }

    private void WriteValues(IReadOnlyList<object?> values)
    {
        _rowsOnSheet++;
        _writer!.WriteStartElement("row", _spreadsheetNamespace);
        _writer.WriteAttributeString("r", _rowsOnSheet.ToString(System.Globalization.CultureInfo.InvariantCulture));
        for (int column = 0; column < values.Count; column++)
        {
            object? value = values[column];
            _writer.WriteStartElement("c", _spreadsheetNamespace);
            _writer.WriteAttributeString("r", $"{ColumnName(column + 1)}{_rowsOnSheet}");
            if (value is decimal or int or long or double or float)
            {
                _writer.WriteStartElement("v", _spreadsheetNamespace);
                _writer.WriteString(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                _writer.WriteEndElement();
            }
            else if (value is bool boolean)
            {
                _writer.WriteAttributeString("t", "b");
                _writer.WriteElementString("v", _spreadsheetNamespace, boolean ? "1" : "0");
            }
            else
            {
                _writer.WriteAttributeString("t", "inlineStr");
                _writer.WriteStartElement("is", _spreadsheetNamespace);
                _writer.WriteStartElement("t", _spreadsheetNamespace);
                _writer.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
                _writer.WriteString(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
                _writer.WriteEndElement();
                _writer.WriteEndElement();
            }
            _writer.WriteEndElement();
        }
        _writer.WriteEndElement();
    }

    public void Complete()
    {
        if (_completed) return;
        EndSheet();
        _completed = true;
    }

    public void Dispose() => Complete();

    private void StartSheet()
    {
        SheetCount++;
        _rowsOnSheet = 0;
        ZipArchiveEntry entry = _archive.CreateEntry($"xl/worksheets/sheet{SheetCount}.xml", CompressionLevel.Fastest);
        _writer = XmlWriter.Create(entry.Open(), new XmlWriterSettings { Encoding = new System.Text.UTF8Encoding(false) });
        _writer.WriteStartDocument(true);
        _writer.WriteStartElement("worksheet", _spreadsheetNamespace);
        _writer.WriteStartElement("sheetData", _spreadsheetNamespace);
        if (_headerValues is not null)
            WriteValues(_headerValues);
    }

    private void EndSheet()
    {
        if (_writer is null) return;
        _writer.WriteEndElement();
        _writer.WriteEndElement();
        _writer.WriteEndDocument();
        _writer.Dispose();
        _writer = null;
    }

    private static string ColumnName(int column)
    {
        Span<char> buffer = stackalloc char[3];
        int index = buffer.Length;
        while (column > 0)
        {
            column--;
            buffer[--index] = (char)('A' + column % 26);
            column /= 26;
        }
        return new string(buffer[index..]);
    }
}
