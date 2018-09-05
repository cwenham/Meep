using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using SmartFormat;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepGit
{
    /// <summary>
    /// Git Fetch
    /// </summary>
    [MeepNamespace("http://meep.example.com/MeepGit/V1")]
    public class Fetch : AMessageModule
    {
        /// <summary>
        /// Working directory in {Smart.Format} of the repository
        /// </summary>
        /// <value>The path.</value>
        /// <remarks>Directory where a repository was previously cloned to.
        /// Defaults to the local path of a LocalisedResource message, which
        /// is returned by Clone.</remarks>
        public string WorkingDir { get; set; } = "{msg.Local}";

        /// <summary>
        /// Optional log message in {Smart.Format}
        /// </summary>
        /// <value>The log message.</value>
        public string LogMessage { get; set; } = "";

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run<Message>(() =>
            {
                MessageContext context = new MessageContext(msg, this);
                string sfWorkingDir = Smart.Format(WorkingDir, context);
                string sfLogMessage = Smart.Format(LogMessage, context);

                using (var repo = new Repository(sfWorkingDir))
                {
                    foreach (Remote remote in repo.Network.Remotes)
                    {
                        IEnumerable<string> refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                        Commands.Fetch(repo, remote.Name, refSpecs, null, sfLogMessage);
                    }
                }

                return msg;
            });
        }
    }
}
