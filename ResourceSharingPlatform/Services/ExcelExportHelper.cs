using ClosedXML.Excel;

namespace ResourceSharingPlatform.Services
{
    public static class ExcelExportHelper
    {
        public static byte[] BuildWorkbook(string sheetName, IReadOnlyList<string> headers, IEnumerable<object?[]> rows)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(sheetName);

            for (var col = 0; col < headers.Count; col++)
            {
                var cell = sheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            var rowIndex = 2;
            foreach (var row in rows)
            {
                for (var col = 0; col < row.Length; col++)
                {
                    var cell = sheet.Cell(rowIndex, col + 1);
                    switch (row[col])
                    {
                        case null:
                            break;
                        case DateTime dt:
                            cell.Value = dt;
                            cell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
                            break;
                        case int i:
                            cell.Value = i;
                            break;
                        case decimal dec:
                            cell.Value = dec;
                            break;
                        default:
                            cell.Value = row[col]!.ToString();
                            break;
                    }
                }
                rowIndex++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
