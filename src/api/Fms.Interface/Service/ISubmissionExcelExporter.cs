using Fms.Model.Entities;

namespace Fms.Interface.Service;

/// <summary>Renders submissions as an Excel workbook (feature 07).</summary>
public interface ISubmissionExcelExporter
{
    byte[] BuildWorkbook(IReadOnlyList<Submission> submissions);
}
