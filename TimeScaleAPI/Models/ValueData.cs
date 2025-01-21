using CsvHelper.Configuration.Attributes;

namespace TimeScaleAPI.Models
{
    public class ValueData
    {
        [Ignore]
        public int Id { get; set; }
        [Ignore]
        public string FileName { get; set; }
        public DateTime Date { get; set; }
        public double ExecutionTime { get; set; }
        public double Value { get; set; }
    }
}
