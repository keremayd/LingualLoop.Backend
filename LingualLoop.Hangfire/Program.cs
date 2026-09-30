using AwsService.Extensions;
﻿using Hangfire;
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

// 5000 macOS'ta AirPlay alıcısı (ControlCenter) tarafından tutuluyor;
// pano API'nin 5214'ünün yanında 5215'te açılır.
builder.WebHost.UseUrls("http://0.0.0.0:5215");

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.AddLogging(builder =>
{
    builder.AddConsole();
});

builder.Services.AddIdentity();
builder.Services.AddPostgres(builder.Configuration);
// Telaffuz üretimi Polly + S3 kullanıyor; API ile aynı yapılandırma.
builder.Services.AddAwsS3Service(builder.Configuration);
builder.Services.AddHangfire(builder.Configuration);

builder.Logging.AddConsole();

var app = builder.Build();

app.UseRouting();

app.UseHangfireDashboard("/hangfire", new DashboardOptions{});
HangfireJobs.ConfigureJobs();

app.Run();
