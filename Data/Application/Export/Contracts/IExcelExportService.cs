using System.Data;

namespace Shop.Data.Application.Export.Contracts;

public interface IExcelExportService
{
    Stream CreateExcel(DataTable table, string sheetName, CancellationToken cancellationToken = default);
}
