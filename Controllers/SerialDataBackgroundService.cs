using System;
using System.IO.Ports;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using SensorDataLogger.Models;
using SensorDataLogger.Dtos;
using SensorDataLogger.Hubs;
using SensorDataLogger.Repositories;

namespace SensorDataLogger.Controllers
{
    public class SerialDataBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SerialDataBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private SerialPort? _serialPort;
        private bool _isFirstConnect = true;

        public SerialDataBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<SerialDataBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessSerialDataAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in serial data processing loop");
                    await Task.Delay(5000, stoppingToken); // Wait before retrying
                }
            }
        }

        private async Task ProcessSerialDataAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<ISensorReadingRepository>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<SensorHub>>();

                try
                {
                    await ConnectToSerialPortAsync();

                    while (!stoppingToken.IsCancellationRequested && _serialPort?.IsOpen == true)
                    {
                        string? line = await ReadLineAsync(stoppingToken);
                        if (string.IsNullOrEmpty(line)) continue;

                        _logger.LogDebug($"Received serial data: {line}");

                        var reading = ParseReading(line);
                        if (reading != null)
                        {
                            await SaveAndBroadcastReadingAsync(reading, repository, hubContext, stoppingToken);
                        }
                    }
                }
                finally
                {
                    CloseSerialPort();
                }
            }
        }

        private async Task ConnectToSerialPortAsync()
        {
            if (_serialPort?.IsOpen == true) return;

            var portName = _configuration.GetValue<string>("SerialPort:Name") ?? "COM13";
            var baudRate = _configuration.GetValue<int>("SerialPort:BaudRate", 115200);

            _serialPort = new SerialPort(portName, baudRate)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            try
            {
                _serialPort.Open();
                if (_isFirstConnect)
                {
                    _logger.LogInformation($"Connected to {portName} at {baudRate} baud");
                    _isFirstConnect = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to open serial port {portName}");
                throw;
            }
        }

        private async Task<string?> ReadLineAsync(CancellationToken stoppingToken)
        {
            try
            {
                return await Task.Run(() => 
                {
                    try
                    {
                        return _serialPort?.ReadLine()?.Trim();
                    }
                    catch (TimeoutException)
                    {
                        return null;
                    }
                }, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from serial port");
                throw;
            }
        }

        private SensorReading? ParseReading(string line)
        {
            try
            {
                // Assuming format: "Temperature: X.XX °C, GSR Average: YYY"
                var tempMatch = Regex.Match(line, @"Temperature: ([-+]?[0-9]*\.?[0-9]+) °C");
                var gsrMatch = Regex.Match(line, @"GSR Average: (\d+)");

                if (!tempMatch.Success || !gsrMatch.Success)
                {
                    _logger.LogWarning($"Failed to parse line: {line}");
                    return null;
                }

                var temperature = double.Parse(tempMatch.Groups[1].Value);
                var gsrValue = double.Parse(gsrMatch.Groups[1].Value);

                // Validate ranges
                if (temperature < -50 || temperature > 100 || gsrValue < 0 || gsrValue > 1023)
                {
                    _logger.LogWarning($"Values out of range: Temp={temperature}, GSR={gsrValue}");
                    return null;
                }

                return new SensorReading
                {
                    Temperature = temperature,
                    GsrValue = gsrValue,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing line: {line}");
                return null;
            }
        }

        private async Task SaveAndBroadcastReadingAsync(
            SensorReading reading,
            ISensorReadingRepository repository,
            IHubContext<SensorHub> hubContext,
            CancellationToken stoppingToken)
        {
            try
            {
                // Save to database
                await repository.AddReadingAsync(reading);

                // Broadcast via SignalR
                var readingDto = new SensorReadingDto
                {
                    Temperature = reading.Temperature,
                    GsrValue = reading.GsrValue,
                    Timestamp = reading.Timestamp
                };

                await hubContext.Clients.All.SendAsync("ReceiveSensorReading", readingDto, stoppingToken);
                
                _logger.LogInformation($"Processed reading: Temp={reading.Temperature:F2}°C, GSR={reading.GsrValue}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving or broadcasting reading");
                throw;
            }
        }

        private void CloseSerialPort()
        {
            try
            {
                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.Close();
                    _logger.LogInformation("Serial port closed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing serial port");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            CloseSerialPort();
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            CloseSerialPort();
            _serialPort?.Dispose();
            base.Dispose();
        }
    }
}