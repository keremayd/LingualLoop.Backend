using Hangfire;
using LingualLoop.Hangfire;
using LingualLoop.Hangfire.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Postgres.Extensions;
using Service.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

builder.Services.AddLogging(builder =>
{
    builder.AddConsole();
});

builder.Services.AddIdentity();
builder.Services.AddPostgres(builder.Configuration);
builder.Services.AddHangfire(builder.Configuration);

builder.Logging.AddConsole();

var app = builder.Build();

app.UseRouting();

app.UseHangfireDashboard("/hangfire", new DashboardOptions{});
HangfireJobs.ConfigureJobs();

app.Run();