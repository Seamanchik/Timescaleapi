using Microsoft.AspNetCore.Mvc;

namespace TimeScaleAPI.Interface
{
    public interface ITimeScale : IDisposable
    {
        Task<IActionResult> UploadCsv(IFormFile file);
        Task<IActionResult> GetResults(
            [FromQuery] string? fileName,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] double? startExecutionTime,
            [FromQuery] double? endExecutionTime,
            [FromQuery] double? startAverageValue,
            [FromQuery] double? endAverageValue);

        Task<IActionResult> GetResultsByFile([FromQuery] string fileName);
    }
}
