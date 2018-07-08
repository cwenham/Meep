using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Xml;
using System.Xml.Serialization;

using NLog;
using Mono.Options;
using Mvp.Xml.XInclude;
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
        static Logger logger = LogManager.GetCurrentClassLogger();

        static AMessageModule PipelineRoot { get; set; }

        static IDisposable Subscription { get; set; }

        static GutterSerialisation GutterSerialisation = GutterSerialisation.JSON;

        static void Main(string[] args)
        {
            bool shouldShowHelp = false;
            string pipelineFile = Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]), "Pipelines", "MasterPipeline.meep");
            string redisHost = "localhost";

            var options = new OptionSet
            {
                { "p|pipeline=", "Path to pipeline.", p => pipelineFile = p },
                { "x|xml", "Gutter serialisation in XML", g => GutterSerialisation = GutterSerialisation.XML },
                { "b|bson", "Gutter serialisation in BSON", b => GutterSerialisation = GutterSerialisation.BSON },
                { "r|redis=", "Redis server hostname[:port].", r => redisHost = r },
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

            var proxy = new HostProxy();
            PipelineRoot = DeserialisePipeline(pipelineFile);
            Subscription = SubscribeToPipeline(PipelineRoot);

            System.Threading.ManualResetEvent resetEvent = new System.Threading.ManualResetEvent(false);
            resetEvent.WaitOne();
        }

        static void ShowHelp()
        {
            Console.WriteLine("E.G.: meep -b evilplan.meep");
        }

        static AMessageModule DeserialisePipeline(string file)
        {
            XmlReader includingReader = new XIncludingReader(file);
            XDownstreamReader meeplangReader = new XDownstreamReader(includingReader);
            XmlSerializer serialiser = new XmlSerializer(typeof(AMessageModule));

            try
            {
                return serialiser.Deserialize(meeplangReader) as AMessageModule;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{0} thrown when deserialising pipeline: {1}", ex.GetType().Name, ex.Message);
                return null;
            }
        }

        private static IDisposable SubscribeToPipeline(AMessageModule root)
        {
            Console.WriteLine("Starting pipeline");

            Subscription?.Dispose();

            try
            {
                IConnectableObservable<Message> observable = root.Pipeline.Publish();
                observable.Connect();

                return observable.SubscribeOn(TaskPoolScheduler.Default).Subscribe<Message>(
                    msg => RegisterMessage(msg),
                    ex => LogError(ex),
                    () => Console.WriteLine("Pipeline completed")
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
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
    }

    public enum GutterSerialisation
    {
        XML,

        JSON,

        BSON
    }
}
