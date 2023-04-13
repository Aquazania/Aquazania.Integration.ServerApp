using HTTPServer.Client;
using HTTPServer.MiddlewareException;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();
builder.Services.AddSingleton<Timed_Client>();
builder.Services.AddHttpClient<ITimed_Client, HttpClientWrapper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var client = builder.Services.BuildServiceProvider().GetRequiredService<Timed_Client>();
//client.StartTimer();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
