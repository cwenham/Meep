using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Reflection;
using System.Xml.Serialization;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NLog;
using Mono.Options;
using Newtonsoft.Json;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;
using System.Diagnostics;
using System.Net;

namespace Meep
{
    class Program
    {
        static Logger logger = LogManager.LoadConfiguration("nlog.config").GetCurrentClassLogger();

        static CancellationTokenSource stoppingTokenSource;

        static Bootstrapper Bootstrapper { get; set; }

        static LaunchOptions options = null;

        static void Main(string[] args)
        {
            try
            {
                options = new LaunchOptions(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `meep --help' for more information.");
                return;
            }

            if (Environment.UserInteractive)
                InteractiveMain(options);
            else
                CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
                        Host.CreateDefaultBuilder(args)
                        .ConfigureServices(services =>
                        {
                            services.AddSingleton(typeof(Bootstrapper), Bootstrapper);
                            services.AddSingleton(typeof(LaunchOptions), options);
                            services.AddHostedService<PipelineService>();
                        });

        /// <summary>
        /// Main() for User Interactive mode (command line)
        /// </summary>
        /// <param name="args"></param>
        public static void InteractiveMain(LaunchOptions options)
        {
            if (options.Help)
                ShowHelp();

            if (options.ListBooks)
                ShowLibrary();

            LoadBasePlugins();

            var proxy = new HostProxy();

            if (String.IsNullOrWhiteSpace(options.GitRepository))
                if (File.Exists(options.PipelineURI))
                    Bootstrapper = new Bootstrapper(options.PipelineURI);
                else
                {
                    Console.WriteLine("Couldn't find a pipeline definition at {0}", options.PipelineURI);
                    Console.WriteLine("Either create one at the default location (Pipelines/MasterPipeline.meep) or specify it with -p path/to/pipeline.meep");
                    Console.WriteLine("Try `meep --help' for more information.");
                    return;
                }
            else
                Bootstrapper = new Bootstrapper(new Uri(options.GitRepository), options.PipelineURI, options.RecheckInterval);

            try
            {
                stoppingTokenSource = new CancellationTokenSource();
                Bootstrapper.PipelineRefreshed += Bootstrapper_PipelineRefreshed;

                Bootstrapper.Start(stoppingTokenSource.Token);

                System.Threading.ManualResetEvent resetEvent = new System.Threading.ManualResetEvent(false);
                resetEvent.WaitOne();

                stoppingTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (System.Environment.UserInteractive)
                    Console.ReadKey();
                throw;
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("E.G.: meep -p evilplan.meep");
        }

        static void ShowLibrary()
        {
            Console.WriteLine("Selections available for <Enumerate Selection=\"...\">");
            foreach (var book in MeepLib.Sources.Enumerate.GetSelections())
                Console.WriteLine(book.Key);
            Console.WriteLine("\n\nFeed the output of <Timer> or <Random> to <Enumerate>, or set the Paragraph attribute to emit specific paragraphs or items from the selection.");
            Console.WriteLine("E.G.: <Enumerate Selection=\"Countries\" Paragraph=\"{msg.Value}\">");
        }

        static void Bootstrapper_PipelineRefreshed(object sender, PipelineRefreshEventArgs e)
        {
            Console.WriteLine("{0}tarting pipeline", stoppingTokenSource == null ? "S" : "Re");

            stoppingTokenSource?.Cancel();
            stoppingTokenSource = new CancellationTokenSource();

            try
            {
                IConnectableObservable<Message> observable = Bootstrapper.PipelineRoot.Pipeline.Publish();
                observable.Connect();

                observable.SubscribeOn(TaskPoolScheduler.Default).Subscribe<Message>(
                    msg => EOLMessage(msg),
                    ex => LogError(ex),
                    () => Console.WriteLine("Pipeline completed"),
                    stoppingTokenSource.Token
                );
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{0} thrown when running pipeline: {1}", ex.GetType().Name, ex.Message);
            }
        }

        /// <summary>
        /// End-Of-Life a message
        /// </summary>
        /// <param name="msg"></param>
        /// <remarks>Writes the message out according to gutter serialisation rules, then calls Dispose().</remarks>
        private static void EOLMessage(Message msg)
        {
            switch (options.GutterSerialisation)
            {
                case GutterSerialisation.JSON:
                    Console.WriteLine($"{msg.AsJSON}\0");
                    break;
                case GutterSerialisation.XML:
                    Console.WriteLine($"{msg.AsXML}\0");
                    break;
                default:
                    break;
            }

            // Acknowledge any Auto-Ack messages
            var ackable = msg.FirstByClass<IAcknowledgableMessage>();
            if (ackable != null && !ackable.HasAcknowledged)
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    ackable.Acknowledge();
                }).ContinueWith((x) =>
                {
                    msg.Dispose();
                });
            }
            else
                msg.Dispose();
        }

        private static void LogError(Exception ex)
        {
            Console.WriteLine($"{ex.GetType().Name} thrown: {ex.Message} - {ex.StackTrace}");
        }

        /// <summary>
        /// Load the plugins that form the Meep core library
        /// </summary>
        private static void LoadBasePlugins()
        {
            string exeDirectory = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);

            Assembly.LoadFrom(Path.Combine(exeDirectory, "MeepSQL.dll"));
            Assembly.LoadFrom(Path.Combine(exeDirectory, "MeepGit.dll"));
            Assembly.LoadFrom(Path.Combine(exeDirectory, "MeepSSH.dll"));

            SmartFormat.Smart.Default.AddExtensions(new MeepLib.Algorithms.SmartFormatExtensions.CSVEscape());
        }
    }

    public enum GutterSerialisation
    {
        None,

        XML,

        JSON,

        BSON
    }
}
