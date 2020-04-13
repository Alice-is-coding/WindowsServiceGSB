/**
 * Script : Application permettant l'exécution du script de gestion de clôture des fiches de frais GSB en arrière plan comme un service Windows. 
 * Author : Alice B.
 * Date : 31/03/2019
 */

using System;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.IO;
using GenericHostSample;
using GSBService;
using MyTools;
using MySql.Data.MySqlClient;

namespace GSBCoreBackgroundService
{
    // Permet d'exécuter le script de clôture des frais GSB en arrière plan de manière asynchrone.
    /// <include file='GSBWindowsServiceDoc.xml' path='doc/members[@name="gsb"]/GSBService/*'/>
    internal class GSBService
    {
        // Gère le mode de lancement de l'application : si debug : lancement en mode console, sinon : lancement en mode service.
        /// <include file='GSBWindowsServiceDoc.xml' path='doc/members[@name="gsb"]/Main/*'/>
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
                // On fait tourner l'app de manière asynchrone en arrière plan.
                await builder.RunAsServiceAsync();
            }
            else
            {
                // Sinon on lance l'application de façon asynchrone comme une application console .Net Core classique (pour tests locaux et debug).
                await builder.RunConsoleAsync();
            }
        }
    }

    /**
     * Cette classe implémente IHostedService et une fois enregistré, sera déclenché et arrêté par le IHost.
     */
    /// <include file='GSBWindowsServiceDoc.xml' path='doc/members[@name="gsb"]/FileWriterService/*'/>
    public class FileWriterService : IHostedService, IDisposable
    {
        // Propriétés.
        /// <include file='GSBWindowsServiceDoc.xml' path='doc/members[@name="gsb"]/PATH/*'/>
        private const string PATH = @"c:\GSBLogs.txt";
        //private Timer _timer;
        /// <include file='GSBWindowsServiceDoc.xml' path='doc/members[@name="gsb"]/actualDate/*'/>
        private static DateTime actualDate = DateTime.Today;
        /// <include file='GSBWindowsServiceDoc.xml' path='doc/members[@name="gsb"]/process/*'/>
        private Process process = new Process();

        // Permet de libérer les ressources.
        /// <include file='GSBWindowsServiceDoc.xml' path='doc/members[@name="gsb"]/Dispose/*'/>
        public void Dispose()
        {
            //_timer?.Dispose();
            process.Dispose();
        }

        // Démarre le script de manière asynchrone.
        /// <include file='GSBWindowsServiceDoc.xml' path='doc/members[@name="gsb"]/StartAsync/*'/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Création d'un nouveau timer qui se déclenchera chaque minute et exécutera le code de la méthode WriteTimeToFile.
            /*_timer = new Timer(
                (e) => ExecuteTask(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromMinutes(3)
                );*/
            // Lancement du script distant.
            ExecuteTask();

            return Task.CompletedTask;
        }

        // Utile pour débug si jamais soucis de lancement distant.
        /// <include file='GSBWindowsServiceDoc.xml' path='doc/members[@name="gsb"]/Execute/*'/>
        public void Execute()
        {
            //Console.WriteLine("L'événement 'chronométrage' a été déclenché le "+ e.SignalTime +"\n");
            MyTools.DirAppend.WriteLog(PATH, "L'événement 'chronométrage' a été déclenché le " + actualDate.ToLongDateString() + "\n");
            Program.CloturerFicheFrais();
            Program.RembourserFicheFrais();
        }

        // Exécute le fichier exe de l'application GestionClotureGSB afin de lancer le service.
        /// <include file='GSBWindowsServiceDoc.xml' path='doc/members[@name="gsb"]/ExecuteTask/*'/>
        public void ExecuteTask()
        {
            process.StartInfo.FileName = @"C:\GestionClotureGSB\win-x64\GestionClotureGSB.exe";
            process.Start();
        }

        // Terminaison de l'application asynchrone en arrière plan.
        /// <include file='GSBWindowsServiceDoc.xml' path='doc/members[@name="gsb"]/StopAsync/*'/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            MyTools.DirAppend.WriteLog("C:\\Windows\\System32\\TestGSBService.txt", "Terminaison de l'application..\n"); // [DEBUG]
            //_timer?.Change(Timeout.Infinite, 0);
            process.Kill();
            process.WaitForExit();
            MyTools.DirAppend.WriteLog("C:\\Windows\\System32\\TestGSBService.txt", "Fin.\n"); // [DEBUG]
            Dispose();
            process.Close();           

            return Task.CompletedTask;
        }
    }
}
