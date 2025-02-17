using MagicMirror.Hubs;
using MagicMirror.Services;
using System.IO;
using System.Reflection;

// Compute project directory from build output (assumes three levels up, adjust if needed)
var projectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
DotNetEnv.Env.Load(Path.Combine(projectDirectory, ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5001");

// Add services to the container.
// In your Startup.cs or Program.cs (ConfigureServices method)
builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.MaximumReceiveMessageSize = 100 * 1024 * 1024; // Example: 10MB (adjust as needed)
});
builder.Services.AddControllers(); // If you need controllers as well

// Register OpenAI Services with Dependency Injection
builder.Services.AddScoped<WhisperService>();
builder.Services.AddScoped<LLMService>();
builder.Services.AddScoped<TTSService>();
builder.Services.AddScoped<SimulationBroadcaster>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Comment out HTTPS redirection to allow external HTTP connections
// app.UseHttpsRedirection();

// Add middleware to serve static files from wwwroot
app.UseStaticFiles();

app.UseRouting(); // Make sure routing is enabled
app.UseAuthorization(); // If you need authorization

app.MapHub<ChatHub>("/chathub"); // Map your SignalR Hub
app.MapControllers(); // Map controllers if you have any

app.Run();
