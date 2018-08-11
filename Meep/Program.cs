using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Reflection;
using System.Xml.Serialization;

using NLog;
using Mono.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;
using System.Diagnostics;
using System.Net;

namespace Meep
{
    class Program
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        static IDisposable Subscription { get; set; }

        static Bootstrapper Bootstrapper { get; set; }

        static GutterSerialisation GutterSerialisation = GutterSerialisation.JSON;

        static void Main(string[] args)
        {
            bool shouldShowHelp = false;
            string pipelineFile = Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]), "Pipelines", "MasterPipeline.meep");
            string redisHost = "localhost";

            var options = new OptionSet
            {
                { "p|pipeline=", "Path to pipeline file", p => pipelineFile = p },
                { "q|quiet", "No gutter serialisation", g => GutterSerialisation = GutterSerialisation.None },
                { "x|xml", "Gutter serialisation in XML", g => GutterSerialisation = GutterSerialisation.XML },
                { "b|bson", "Gutter serialisation in BSON", b => GutterSerialisation = GutterSerialisation.BSON },
                { "r|redis=", "Redis server hostname[:port]", r => redisHost = r },
                { "h|help", "show this message and exit", h => shouldShowHelp = h != null },
            };

            List<string> extra;
            try
            {
                extra = options.Parse(args);
            }
            catch (OptionException e)
            {
                // output some error message
                Console.Write("meep: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `meep --help' for more information.");
                return;
            }

            if (shouldShowHelp)
                ShowHelp();

            LoadBasePlugins();

            IConnectionMultiplexer multiplexer = null;
            try
            {
                multiplexer = ConnectionMultiplexer.Connect(redisHost);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} thrown connecting to Redis. Continuing without. ({1})", ex.GetType().Name, ex.Message);
            }
            var proxy = new HostProxy(multiplexer);

            Bootstrapper = new Bootstrapper(pipelineFile);
            Bootstrapper.PipelineRefreshed += Bootstrapper_PipelineRefreshed;
            Bootstrapper.Start();

            System.Threading.ManualResetEvent resetEvent = new System.Threading.ManualResetEvent(false);
            resetEvent.WaitOne();

            Bootstrapper.Stop();
            Subscription?.Dispose();
        }

        static void ShowHelp()
        {
            Console.WriteLine("E.G.: meep -b evilplan.meep");
        }

        static void Bootstrapper_PipelineRefreshed(object sender, PipelineRefreshEventArgs e)
        {
            Console.WriteLine("{0}tarting pipeline", Subscription == null ? "S" : "Re");

            Subscription?.Dispose();

            try
            {
                IConnectableObservable<Message> observable = Bootstrapper.PipelineRoot.Pipeline.Publish();
                observable.Connect();

                Subscription = observable.SubscribeOn(TaskPoolScheduler.Default).Subscribe<Message>(
                    msg => RegisterMessage(msg),
                    ex => LogError(ex),
                    () => Console.WriteLine("Pipeline completed")
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void RegisterMessage(Message msg)
        {
            switch (GutterSerialisation)
            {
                case GutterSerialisation.JSON:
                    Console.WriteLine($"{msg.AsJSON}\0");
                    break;
                case GutterSerialisation.XML:
                    Console.WriteLine($"{msg.AsXML}\0");
                    break;
                case GutterSerialisation.BSON:
                    MemoryStream ms = new MemoryStream();
                    msg.ToBSONStream(ms);
                    Console.WriteLine(Convert.ToBase64String(ms.ToArray()) + "\0");
                    break;
                default:
                    break;
            }
        }

        private static void LogError(Exception ex)
        {
            Console.WriteLine($"{ex.GetType().Name} thrown: {ex.Message}");
        }

        /// <summary>
        /// Load the plugins that form the Meep core library
        /// </summary>
        private static void LoadBasePlugins()
        {
            string exeDirectory = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);

            Assembly.LoadFrom(Path.Combine(exeDirectory, "MeepSQL.dll"));
            Assembly.LoadFrom(Path.Combine(exeDirectory, "MeepGit.dll"));
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
