using System.CommandLine;
using System.Drawing;
using System.Drawing.Imaging;
using BasaltHexagons.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Svg;

namespace BasaltHexagonsIconsGenerator;



class Program
{
    static async Task<int> Main(string[] args)
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

        hostBuilder
            .ConfigureServices((context, services) =>
            {
                services
                    .AddCommandLineSupport();
            });

        IHost host = hostBuilder.Build();
        RootCommand rootCommand = host.Services.GetRequiredService<RootCommand>();
        return await rootCommand.InvokeAsync(args);

        
    }

    
}

