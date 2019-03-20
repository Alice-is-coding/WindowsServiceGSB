using System;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.IO;
using GenericHostSample;

namespace GSBCoreBackgroundService
{
    internal class GSBService
    {
        private static async Task Main(string[] args)
        {
            // Si nous debuggons ou si l'application a été lancée en passant un argument "--console" : on passe isService à faux.
            var isService = !(Debugger.IsAttached || args.Contains("--console"));
            // Création d'une instance de HostBuilder qui nous servira à créer un hôte générique. 
            var builder = new HostBuilder()
                // On Configure un service qui implémente l'interface IHostedService : C'est de cette façon que l'on lancera le code dans l'application.
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<FileWriterService>();
                });
            // S'il s'agit d'un service :
            if (isService)
            {
                // On fait tourner l'app de manière asynchrone.
                await builder.RunAsServiceAsync();
            }else
            {
                // Sinon on lance l'application de façon synchrone comme une application console .Net Core classique (pour tests locaux et debug).
                await builder.RunConsoleAsync();
            }
        }
    }

    /**
     * Cette classe implémente IHostedService et une fois enregistré, sera déclenché et arrêté par le IHost.
     */
    public class FileWriterService : IHostedService, IDisposable
    {
        // Propriétés.
        private const string Path = @"d:\TestApplication.txt";
        private Timer _timer; 

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Création d'un nouveau timer qui se déclenchera chaque minute et exécutera le code de la méthode WriteTimeToFile.
            _timer = new Timer(
                (e) => WriteTimeToFile(),
                null, 
                TimeSpan.Zero,
                TimeSpan.FromMinutes(1)
                );

            return Task.CompletedTask;
        }

        // Ecrit la date actuelle dans un fichier. 
        public void WriteTimeToFile()
        {
            if (!File.Exists(Path))
            {
                using (var sw = File.CreateText(Path))
                {
                    sw.WriteLine(DateTime.UtcNow.ToString("O"));   
                }
            }else
            {
                using (var sw = File.AppendText(Path))
                {
                    sw.WriteLine(DateTime.UtcNow.ToString("O"));
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }
    }
}
