using Microsoft.AspNetCore.SignalR;

namespace SensorDataLogger.Hubs
{
    public class SensorHub : Hub
    {
        public async Task BroadcastSensorReading(object sensorData)
        {
            await Clients.All.SendAsync("ReceiveSensorReading", sensorData);
        }
    }
}
