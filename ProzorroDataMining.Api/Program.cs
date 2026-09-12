using AutoMapper;
using ProzorroDataMining.API.Middlewares;
using ProzorroDataMining.Application;
using ProzorroDataMining.Core;
using ProzorroDataMining.Infrastructure;
using Swashbuckle.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure();
builder.Services.AddApplicationServices();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("PerUser", context =>
    {
        var userIp = context.Connection.RemoteIpAddress?.ToString()
                       ?? "unknown";

        return RateLimitPartition.GetConcurrencyLimiter(
            userIp,
            _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 10,
                QueueLimit = 10,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });
});

//Add Controllers 
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

app.UseExceptionHandlerMiddleware();
//Add Routing
app.UseRouting();
//Add Authentication and Authorization
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
//Add Endpoints
app.MapControllers();
app.Run();
