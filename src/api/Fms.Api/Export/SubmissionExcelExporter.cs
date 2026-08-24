using ClosedXML.Excel;
using Fms.Api.Data.Entities;

namespace Fms.Api.Export;

/// <summary>
/// Renders a list of submissions as an Excel workbook (feature 07). Each
/// submission's data is flattened to columns via <see cref="SubmissionFlattener"/>;
/// the header row is the sorted union of every flattened column so heterogeneous
/// submissions share a table, with missing cells left blank. Values are written as
/// text.
/// </summary>
public sealed class SubmissionExcelExporter
{
    /// <summary>Returns the workbook bytes for <paramref name="submissions"/>.</summary>
    public byte[] BuildWorkbook(IReadOnlyList<Submission> submissions)
    {
        var rows = submissions.Select(s => SubmissionFlattener.Flatten(s.Data)).ToList();
        var columns = rows
            .SelectMany(r => r.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Submissions");

        for (var c = 0; c < columns.Count; c++)
        {
            sheet.Cell(1, c + 1).Value = columns[c];
        }

        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < columns.Count; c++)
            {
                if (rows[r].TryGetValue(columns[c], out var value))
                {
                    sheet.Cell(r + 2, c + 1).Value = value;
                }
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
