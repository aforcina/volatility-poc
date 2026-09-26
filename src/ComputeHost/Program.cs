using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ComputeHost.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddControllers(); // for REST stub
builder.Services.AddLogging();
builder.Services.AddSingleton<IQuantEngine, MockQuantEngine>(); // swap with real adapter later

var app = builder.Build();

app.MapGrpcService<ComputeServiceImpl>();
app.MapControllers();

app.MapGet("/", () => "ComputeHost running. gRPC endpoint on /ComputeService");

app.Run("http://127.0.0.1:5001"); // loopback: use http for POC; gRPC over HTTP/2
