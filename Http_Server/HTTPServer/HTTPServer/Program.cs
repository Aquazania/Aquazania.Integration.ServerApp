using Aquazania.Integration.ServerApp.Client.Interfaces;
using Aquazania.Integration.ServerApp.Factory;
using HTTPServer.Client;
using HTTPServer.MiddlewareException;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
try
{
    builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();
    builder.Services.AddSingleton<Timed_Client>();
    builder.Services.AddHttpClient<ITimed_Client, HttpClientWrapper>();

}
catch (Exception e)
{
    EventLogger.logerror(e.Message);
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
try
{
    var client = builder.Services.BuildServiceProvider().GetRequiredService<Timed_Client>();
    client.StartTimer();
}
catch (Exception e)
{ EventLogger.logerror(e.Message); }

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
