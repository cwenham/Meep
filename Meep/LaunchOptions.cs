using System;
using System.Collections.Generic;

using Mono.Options;

namespace Meep
{
    public class LaunchOptions
    {
        public string PipelineURI { get; set; }

        public string GitRepository { get; set; }

        /// <summary>
        /// How frequently to recheck a Git repo or URL for changes
        /// </summary>
        public TimeSpan RecheckInterval { get; set; } = TimeSpan.FromMinutes(30);   

        public GutterSerialisation GutterSerialisation { get; set; }

        /// <summary>
        /// List books available with Enumerate
        /// </summary>
        public bool ListBooks { get; set; }

        /// <summary>
        /// Show help message and exit
        /// </summary>
        public bool Help { get; set; }

        /// <summary>
        /// Anything left over from parsing command-line arguments
        /// </summary>
        public List<string> Extras { get; set; }

        public LaunchOptions(string[] args)
        {
            var options = new OptionSet
            {
                { "p|pipeline=", "Path or URL to pipeline file", p => PipelineURI = p },
                { "g|git=", "Git repo address", g => GitRepository = g },
                { "t|recheck=", "Time to recheck Git/Url for changes", t => RecheckInterval = TimeSpan.Parse(t) },
                { "q|quiet", "No gutter serialisation", g => GutterSerialisation = GutterSerialisation.None },
                { "x|xml", "Gutter serialisation in XML", g => GutterSerialisation = GutterSerialisation.XML },
                { "lb|listBooks", "Display a list of books available with <Enumerate Selection=\"...\"/>", lb => ListBooks = lb != null },
                { "h|help", "show this message and exit", h => Help = h != null },
            };

            Extras = options.Parse(args);
        }
    }
}
