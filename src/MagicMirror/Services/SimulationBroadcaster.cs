using Microsoft.AspNetCore.SignalR;
using System;
using System.IO;
using System.Threading.Tasks;
using MagicMirror.Hubs;
using Microsoft.Extensions.Configuration;

namespace MagicMirror.Services
{
    public class SimulationBroadcaster
    {
        private static bool _simulationStarted = false;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly string _simulatedFile;

        public SimulationBroadcaster(IHubContext<ChatHub> hubContext, IConfiguration configuration)
        {
            _hubContext = hubContext;
            var fileBasePath = Directory.GetCurrentDirectory();
            _simulatedFile = Path.Combine(fileBasePath, configuration["Audio:SimulatedFile"] ?? "simulated_response.mp3");
            if (!File.Exists(_simulatedFile))
            {
                File.Create(_simulatedFile).Close();
            }
        }

        public void Start()
        {
            if (_simulationStarted)
                return;
            _simulationStarted = true;
            Task.Run(async () =>
            {
                byte[] simulatedAudio = await File.ReadAllBytesAsync(_simulatedFile);
                string simulatedBase64 = Convert.ToBase64String(simulatedAudio);
                while (true)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveMessage", "listening");
                    await _hubContext.Clients.All.SendAsync("ReceiveMessage", "transcribing");
                    // await _hubContext.Clients.All.SendAsync("ReceiveAudio", simulatedBase64);
                    await Task.Delay(10000);
                }
            });
        }
    }
}
