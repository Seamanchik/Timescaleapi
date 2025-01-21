using TimeScaleAPI.Interface;
using Microsoft.AspNetCore.Mvc;
using TimeScaleAPI.Data;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using TimeScaleAPI.Models;

namespace TimeScaleAPI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeScaleController : ControllerBase, ITimeScale
    {
        private readonly AppDbContext _context;

        public TimeScaleController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetResults")]
        public async Task<IActionResult> GetResults(
            [FromQuery] string? fileName,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] double? startExecutionTime,
            [FromQuery] double? endExecutionTime,
            [FromQuery] double? startAverageValue,
            [FromQuery] double? endAverageValue)
        {
            var query = _context.ResultDatas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(fileName))
                query = query.Where(x => x.FileName == fileName);

            if (startDate.HasValue)
                query = query.Where(x => x.MinDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.MinDate <= endDate.Value);

            if (startExecutionTime.HasValue)
                query = query.Where(x => x.ExecutionTime >= startExecutionTime.Value);

            if (endExecutionTime.HasValue)
                query = query.Where(x => x.ExecutionTime <= endExecutionTime.Value);

            if (startAverageValue.HasValue)
                query = query.Where(x => x.AverageValue >= startAverageValue.Value);

            if (endAverageValue.HasValue)
                query = query.Where(x => x.AverageValue <= endAverageValue.Value);

            var results = await query.ToListAsync();

            return Ok(results);
        }

        [HttpGet("GetResultsByFile")]
        public async Task<IActionResult> GetResultsByFile([FromQuery]string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("File is empty or null.");

            var values = await _context.ValueDatas
                .Where(x => x.FileName == fileName)
                .OrderByDescending(x => x.Date)
                .Take(10)
                .ToListAsync();

            return Ok(values);
        }

        [HttpPost("UploadCsv")]
        public async Task<IActionResult> UploadCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty or null.");

            using var reader = new StreamReader(file.OpenReadStream());

            using var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                MissingFieldFound = null
            });

            var records = csvReader.GetRecords<ValueData>().ToList();

            if (!ValidateData(records, out var message))
                return BadRequest(message);

            var fileName = Path.GetFileName(file.FileName);
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.ValueDatas.RemoveRange(_context.ValueDatas.Where(x => x.FileName == fileName));
                _context.ResultDatas.RemoveRange(_context.ResultDatas.Where(x => x.FileName == fileName));

                foreach (var record in records)
                    record.FileName = fileName;

                await _context.ValueDatas.AddRangeAsync(records);

                var result = CalculateResultData(records, fileName);
                await _context.ResultDatas.AddAsync(result);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok("File uploaded successfully.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private bool ValidateData(List<ValueData> values, out string message)
        {
            message = string.Empty;
            if (values.Count < 1 || values.Count > 10000)
            {
                message = "Number of values must be between 1 and 10000.";
                return false;
            }

            foreach (var value in values)
            {
                if (value.Date < new DateTime(2000,1,1) || value.Date > DateTime.UtcNow)
                {
                    message = "Date must be between 2000-01-01 and now.";
                    return false;
                }

                if (value.ExecutionTime < 0)
                {
                    message = "ExecutionTime must be greater than 0.";
                    return false;
                }

                if (value.Value < 0)
                {
                    message = "AverageValue must be greater than 0.";
                    return false;
                }
            }
            return true;
        }

        private ResultData CalculateResultData(List<ValueData> values, string fileName)
        {
            var minDate = values.Min(x => x.Date);
            var maxDate = values.Max(x => x.Date);
            var delta = (maxDate - minDate).TotalSeconds;

            var executionTime = values.Average(r => r.ExecutionTime);
            var avgValue = values.Average(r => r.Value);
            var medianValue = values.Select(r => r.Value).OrderBy(v => v).Skip(values.Count / 2).FirstOrDefault();

            return new ResultData
            {
                FileName = fileName,
                DeltaDate = delta,
                MinDate = minDate,
                ExecutionTime = executionTime,
                AverageValue = avgValue,
                MedianValue = medianValue,
                MaxValue = values.Max(x => x.Value),
                MinValue = values.Min(x => x.Value)
            };
        }

        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    _context.Dispose();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}