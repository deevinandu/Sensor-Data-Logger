using Microsoft.AspNetCore.Http.HttpResults;
using SensorDataLogger.Models;

namespace SensorDataLogger.Repositories
{
    public interface ISensorReadingRepository
    {
        // Create
        Task AddReadingAsync(SensorReading reading);

        // Read
        Task<IEnumerable<SensorReading>> GetLatestReadingsAsync(int count);
        Task<SensorReading?> GetReadingByIdAsync(int id);

        // Update
        Task UpdateReadingAsync(SensorReading reading);

        // Delete
        Task DeleteReadingAsync(int id);

        // Additional query methods
        Task<IEnumerable<SensorReading>> GetReadingsByDateRangeAsync(DateTime start, DateTime end);
    }
}
