using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ReqResClient.Core.Options;
using ReqResClient.Core.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<ReqResClientOptions>(opts =>
        {
            opts.BaseUrl = "https://reqres.in/api/";
            opts.CacheDurationSeconds = 30;
        });

        services.AddMemoryCache();

        services.AddHttpClient<IExternalUserService, ExternalUserService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ReqResClientOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });
    })
    .Build();

var service = host.Services.GetRequiredService<IExternalUserService>();
var user = await service.GetUserByIdAsync(2);
Console.WriteLine($"{user?.FirstName} {user?.LastName}");
