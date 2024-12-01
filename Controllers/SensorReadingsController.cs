using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SensorDataLogger.Hubs;
using SensorDataLogger.Models;
using SensorDataLogger.Repositories;
using SensorDataLogger.Dtos;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace SensorDataLogger.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensorReadingsController : ControllerBase
    {
        private readonly ISensorReadingRepository _repository;
        private readonly IHubContext<SensorHub> _hubContext;
        private readonly ILogger<SensorReadingsController> _logger;

        public SensorReadingsController(
            ISensorReadingRepository repository,
            IHubContext<SensorHub> hubContext,
            ILogger<SensorReadingsController> logger)
        {
            _repository = repository;
            _hubContext = hubContext;
            _logger = logger;
        }



        [HttpPost]
        public async Task<IActionResult> AddReading([FromBody] SensorReadingDto readingDto)
        {
            // Convert DTO to model
            var reading = new SensorReading
            {
                Temperature = readingDto.Temperature,
                GsrValue = readingDto.GsrValue,
                Timestamp = DateTime.UtcNow
            };

            // Save to database
            await _repository.AddReadingAsync(reading);

            // Broadcast to all clients via SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveSensorReading", readingDto);

            return CreatedAtAction(nameof(GetReading), new { id = reading.Id }, reading);
        }

        [HttpGet("latest")]
        public async Task<ActionResult<IEnumerable<SensorReading>>> GetLatestReadings(int count = 20)
        {
            var readings = await _repository.GetLatestReadingsAsync(count);
            return Ok(readings);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SensorReading>> GetReading(int id)
        {
            var reading = await _repository.GetReadingByIdAsync(id);

            if (reading == null)
            {
                return NotFound();
            }

            return Ok(reading);
        }
    }
}