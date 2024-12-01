namespace SensorDataLogger.Models
{
    public class SensorReading
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; }
        public double GsrValue { get; set; }
    }
}
