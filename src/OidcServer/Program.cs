using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OidcServer.Configuration;
using OidcServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddControllers();

builder.Services.Configure<OidcSettings>(builder.Configuration.GetSection("OidcSettings"));

builder.Services.AddSingleton<IConfigureOptions<OidcSettings>>(sp =>
{
  return new ConfigureNamedOptions<OidcSettings>(Options.DefaultName, settings =>
  {
    var jwksPath = Path.Combine(AppContext.BaseDirectory, "data", "jwks.json");
    var jwksJson = File.ReadAllText(jwksPath);
    settings.Jwks = JsonConvert.DeserializeObject<JwksConfig>(jwksJson);

    var discoveryPath = Path.Combine(AppContext.BaseDirectory, "data", "oidc-config.json");
    var discoveryJson = File.ReadAllText(discoveryPath);
    settings.Discovery = JsonConvert.DeserializeObject<OidcDiscoveryConfig>(discoveryJson);
  });
});

builder.Services.AddSingleton<OidcUserStore>();
builder.Services.AddSingleton<AuthCodeStore>();

var app = builder.Build();

app.MapControllers();
app.Run();
