using System.Data;
using OfficeOpenXml;
using Shop.Data.Application.Export.Contracts;

namespace Shop.Data.Infrastructure.Export;

public sealed class DataTableExcelExportService : IExcelExportService
{
    private const int MaximumRowsPerSheet = 1_048_576;
    public Stream CreateExcel(DataTable table, string sheetName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);
        if (table.Columns.Count == 0)
            throw new ArgumentException("The Excel table must have at least one column.", nameof(table));
        if (table.Columns.Count > 16_384)
            throw new ArgumentOutOfRangeException(nameof(table), "An Excel worksheet supports at most 16,384 columns.");

        using var package = new ExcelPackage();
        int sheetNumber = 1;
        int rowOnSheet = 1;
        ExcelWorksheet worksheet = AddWorksheet(sheetNumber);

        void AddHeader(ExcelWorksheet target)
        {
            for (int column = 0; column < table.Columns.Count; column++)
                target.Cells[1, column + 1].Value = table.Columns[column].ColumnName;

            target.Cells[1, 1, 1, table.Columns.Count].Style.Font.Bold = true;
            target.View.FreezePanes(2, 1);
        }

        ExcelWorksheet AddWorksheet(int number)
        {
            string name = number == 1 ? sheetName : $"{sheetName} {number}";
            ExcelWorksheet target = package.Workbook.Worksheets.Add(name);
            AddHeader(target);
            return target;
        }

        foreach (DataRow dataRow in table.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (rowOnSheet == MaximumRowsPerSheet)
            {
                if (rowOnSheet > 1)
                    worksheet.Cells[1, 1, rowOnSheet, table.Columns.Count].AutoFilter = true;

                worksheet = AddWorksheet(++sheetNumber);
                rowOnSheet = 1;
            }

            rowOnSheet++;
            for (int column = 0; column < table.Columns.Count; column++)
            {
                object value = dataRow[column];
                worksheet.Cells[rowOnSheet, column + 1].Value = value switch
                {
                    DBNull => null,
                    string or bool or byte or short or int or long or decimal or double or float or DateTime => value,
                    _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
                };
            }
        }

        if (rowOnSheet > 1)
            worksheet.Cells[1, 1, rowOnSheet, table.Columns.Count].AutoFilter = true;

        var stream = new MemoryStream();
        try
        {
            package.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }
}
