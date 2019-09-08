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

        static IDisposable Subscription { get; set; }

        static Bootstrapper Bootstrapper { get; set; }

        static GutterSerialisation GutterSerialisation = GutterSerialisation.JSON;

        static void Main(string[] args)
        {
            bool shouldShowHelp = false;
            bool shouldShowTypePrefixes = false;
            bool shouldShowLibrary = false;
            string gitRepo = null;
            string pipelineFile = Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]), "Pipelines", "MasterPipeline.meep");
            TimeSpan recheck = TimeSpan.FromMinutes(30);

            var options = new OptionSet
            {
                { "p|pipeline=", "Path or URL to pipeline file", p => pipelineFile = p },
                { "g|git=", "Git repo address", g => gitRepo = g },
                { "t|recheck=", "Time to recheck Git/Url for changes", t => recheck = TimeSpan.Parse(t) },
                { "q|quiet", "No gutter serialisation", g => GutterSerialisation = GutterSerialisation.None },
                { "x|xml", "Gutter serialisation in XML", g => GutterSerialisation = GutterSerialisation.XML },
                { "tp|typePrefixes", "Display a list of type prefixes (if Meep is misidentifying parameter types)", tp => shouldShowTypePrefixes = tp != null },
                { "lb|listBooks", "Display a list of books available with <Enumerate Selection=\"...\"/>", lb => shouldShowLibrary = lb != null },
                { "h|help", "show this message and exit", h => shouldShowHelp = h != null },
            };

            List<string> extra;
            try
            {
                extra = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `meep --help' for more information.");
                return;
            }

            if (shouldShowHelp)
                ShowHelp();

            if (shouldShowTypePrefixes)
                ShowTypePrefixes();

            if (shouldShowLibrary)
                ShowLibrary();

            LoadBasePlugins();

            var proxy = new HostProxy();

            if (String.IsNullOrWhiteSpace(gitRepo))
                if (File.Exists(pipelineFile))
                    Bootstrapper = new Bootstrapper(pipelineFile);
                else
                {
                    Console.WriteLine("Couldn't find a pipeline definition at {0}", pipelineFile);
                    Console.WriteLine("Either create one at the default location (Pipelines/MasterPipeline.meep) or specify it with -p path/to/pipeline.meep");
                    Console.WriteLine("Try `meep --help' for more information.");
                    return;
                }
            else
                Bootstrapper = new Bootstrapper(new Uri(gitRepo), pipelineFile, recheck);

            try
            {
                Bootstrapper.PipelineRefreshed += Bootstrapper_PipelineRefreshed;
                Bootstrapper.Start();

                System.Threading.ManualResetEvent resetEvent = new System.Threading.ManualResetEvent(false);
                resetEvent.WaitOne();

                Bootstrapper.Stop();
                Subscription?.Dispose();
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
            Console.WriteLine("E.G.: meep -b evilplan.meep");
        }

        static void ShowTypePrefixes()
        {
            Console.WriteLine("Type Prefixes and conventions. Meep will try to guess the type by what it looks like, but if that's not working then these prefixes and conventions tell Meep specifically how to handle arguments in pipeline definitions.");
            Console.WriteLine("  /Regex Slashes/      - Regular expression between the slashes.");
            Console.WriteLine("  RX:RegEx             - Explicit Regex prefix.");
            Console.WriteLine("  $.JSONPath           - Begin absolute JSON Paths (JPaths) with '$'.");
            Console.WriteLine("  JP:.path[0]          - Relative JSON Paths with the 'JPath:' prefix.");
            Console.WriteLine("  XP:Element/Path      - Relative XPaths with the 'XPath:' prefix.");
            Console.WriteLine("  //Element/XPath      - Extra prefix not needed if the XPath begins with '//'.");
            Console.WriteLine("  SF:{Smart.Format}    - Smart.Format templates with the 'SF:' prefix.");
            Console.WriteLine("  NC:[NCalc] Format    - NCalc Formatted expressions with the 'NC:' prefix.");
            Console.WriteLine("  ./Path/To/File       - On Unix or Windows, specify relative paths with the './' prefix and forward slashes.");
            Console.WriteLine("  URL:/relative/url    - Specify relative URLs with the 'URL:' prefix.");
            Console.WriteLine("  http://example.com/  - Absolute URLs do not need an extra prefix. Meep knows the 'scheme://' pattern.");
            Console.WriteLine("  <?xml?>              - Begin XML with a standard declaration (include version and encoding optionally) if Meep doesn't guess from the matching root element tags.");
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
            Console.WriteLine("{0}tarting pipeline", Subscription == null ? "S" : "Re");

            Subscription?.Dispose();

            try
            {
                IConnectableObservable<Message> observable = Bootstrapper.PipelineRoot.Pipeline.Publish();
                observable.Connect();

                Subscription = observable.SubscribeOn(TaskPoolScheduler.Default).Subscribe<Message>(
                    msg => EOLMessage(msg),
                    ex => LogError(ex),
                    () => Console.WriteLine("Pipeline completed")
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
            switch (GutterSerialisation)
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

            msg.Dispose();
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
