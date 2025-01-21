namespace TimeScaleAPI.Models
{
    public class ResultData
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public double DeltaDate { get; set; }
        public DateTime MinDate { get; set; }
        public double ExecutionTime { get; set; }
        public double AverageValue { get; set; }
        public double MedianValue { get; set; }
        public double MaxValue { get; set; }
        public double MinValue { get; set; }
    }
}
