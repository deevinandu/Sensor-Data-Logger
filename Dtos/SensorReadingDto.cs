using System.ComponentModel.DataAnnotations;

namespace SensorDataLogger.Dtos
{
    public class SensorReadingDto
    {
        [Range(-50, 100, ErrorMessage = "Temperature out of valid range")]
        public double Temperature { get; set; }

        [Range(0, 1023, ErrorMessage = "GSR value out of valid range")]
        public double GsrValue { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
