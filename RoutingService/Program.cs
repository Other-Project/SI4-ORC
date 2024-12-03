var builder = WebApplication.CreateBuilder();
builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.AllowSynchronousIO = true; // Note only needed now if using Streamed transfer mode, can probably remove
});

builder.Services.AddCors();
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddServiceModelWebServices();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

RoutingService.RoutingService.MaxWalkedDistance = builder.Configuration.GetValue<double?>("MaxWalkedDistance") ?? 10000;
RoutingService.RoutingService.ActiveMqUri = Uri.TryCreate(builder.Configuration.GetValue<string>("ActiveMqUri"), UriKind.Absolute, out var uri) ? uri : null;

var app = builder.Build();
app.UseCors(policyBuilder => policyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<RoutingService.RoutingService>();
    serviceBuilder.AddServiceEndpoint<RoutingService.RoutingService, IRoutingService>(new BasicHttpBinding(BasicHttpSecurityMode.None), "/soap");
    serviceBuilder.AddServiceWebEndpoint<RoutingService.RoutingService, IRoutingService>("/web", behavior =>
    {
        behavior.HelpEnabled = true;
        behavior.FaultExceptionEnabled = true;
    });
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpsGetEnabled = true;
});

await app.RunAsync();
