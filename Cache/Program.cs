var builder = WebApplication.CreateBuilder();

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

var app = builder.Build();

JcDecauxClient.ApiUrl = builder.Configuration.GetValue<string>("JCDecaux:ApiUrl") ?? JcDecauxClient.ApiUrl;
JcDecauxClient.ApiKey = builder.Configuration.GetValue<string>("JCDecaux:ApiKey");
OrsClient.ApiUrl = builder.Configuration.GetValue<string>("OpenRouteService:ApiUrl") ?? OrsClient.ApiUrl;
OrsClient.ApiKey = builder.Configuration.GetValue<string>("OpenRouteService:ApiKey");

app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<ProxyCacheService>();
    serviceBuilder.AddServiceEndpoint<ProxyCacheService, IProxyCacheService>(new BasicHttpBinding(BasicHttpSecurityMode.None), "/");
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpsGetEnabled = true;
});

await app.RunAsync();
