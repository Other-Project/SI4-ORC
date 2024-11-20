using RoutingService.JCDecaux;
using RoutingService.OpenRouteService;

var builder = WebApplication.CreateBuilder();
builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.AllowSynchronousIO = true; // Note only needed now if using Streamed transfer mode, can probably remove
});

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddServiceModelWebServices();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

JcDeacauxClient.ApiUrl = builder.Configuration.GetValue<string>("JCDecaux:ApiUrl") ?? JcDeacauxClient.ApiUrl;
JcDeacauxClient.ApiKey = builder.Configuration.GetValue<string>("JCDecaux:ApiKey");
OrsClient.ApiUrl = builder.Configuration.GetValue<string>("OpenRouteService:ApiUrl") ?? OrsClient.ApiUrl;
OrsClient.ApiKey = builder.Configuration.GetValue<string>("OpenRouteService:ApiKey");

var app = builder.Build();

app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<Service>();
    serviceBuilder.AddServiceEndpoint<Service, IService>(new BasicHttpBinding(BasicHttpSecurityMode.None), "/soap");
    serviceBuilder.AddServiceWebEndpoint<Service, IService>("/web", behavior => { behavior.HelpEnabled = true; });
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpsGetEnabled = true;
});

await app.RunAsync();
