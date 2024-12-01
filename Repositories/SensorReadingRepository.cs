using Microsoft.EntityFrameworkCore;
using SensorDataLogger.Data;
using SensorDataLogger.Models;

namespace SensorDataLogger.Repositories
{

    public class SensorReadingRepository : ISensorReadingRepository
    {
        private readonly 
            Context _context;

        public SensorReadingRepository(SensorDbContext context)
        {
            _context = context;
        }

        public async Task AddReadingAsync(SensorReading reading)
        {
            reading.Timestamp = DateTime.UtcNow;
            _context.SensorReadings.Add(reading);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SensorReading>> GetLatestReadingsAsync(int count)
        {
            return await _context.SensorReadings
                .OrderByDescending(r => r.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<SensorReading?> GetReadingByIdAsync(int id)
        {
            return await _context.SensorReadings.FindAsync(id);
        }

        public async Task UpdateReadingAsync(SensorReading reading)
        {
            _context.SensorReadings.Update(reading);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteReadingAsync(int id)
        {
            var reading = await _context.SensorReadings.FindAsync(id);
            if (reading != null)
            {
                _context.SensorReadings.Remove(reading);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<SensorReading>> GetReadingsByDateRangeAsync(DateTime start, DateTime end)
        {
            return await _context.SensorReadings
                .Where(r => r.Timestamp >= start && r.Timestamp <= end)
                .OrderBy(r => r.Timestamp)
                .ToListAsync();
        }
    }
}
