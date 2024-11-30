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

Service.MaxWalkedDistance = builder.Configuration.GetValue<double?>("MaxWalkedDistance") ?? 10000;

var app = builder.Build();
app.UseCors(policyBuilder => policyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<Service>();
    serviceBuilder.AddServiceEndpoint<Service, IService>(new BasicHttpBinding(BasicHttpSecurityMode.None), "/soap");
    serviceBuilder.AddServiceWebEndpoint<Service, IService>("/web", behavior =>
    {
        behavior.HelpEnabled = true;
        behavior.FaultExceptionEnabled = true;
    });
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpsGetEnabled = true;
});

await app.RunAsync();
