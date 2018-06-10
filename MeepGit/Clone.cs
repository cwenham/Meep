using System;
using System.Xml.Serialization;
using System.Threading.Tasks;

using LibGit2Sharp;
using SmartFormat;

using MeepLib;
using MeepModel.Messages;

namespace MeepGit
{
    /// <summary>
    /// Clone a repository
    /// </summary>
    [XmlRoot(ElementName = "Clone", Namespace = "http://meep.example.com/MeepGit/V1")]
    public class Clone : AMessageModule
    {
        /// <summary>
        /// Repository address in {Smart.Format}
        /// </summary>
        /// <value>The repository.</value>
        [XmlAttribute]
        public string Repository { get; set; }

        /// <summary>
        /// Working directory in {Smart.Format} to clone into
        /// </summary>
        /// <value>The path.</value>
        /// <remarks>Leave empty or null to create one based on the repo address automatically.</remarks>
        [XmlAttribute]
        public string WorkingDir { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            string repoURL = Smart.Format(Repository, msg);
            string workDir = Smart.Format(WorkingDir, msg);
            if (String.IsNullOrWhiteSpace(workDir))
                workDir = repoURL.ToWorkingDirectory();

            // ToDo: implement me
            return await base.HandleMessage(msg);
        }
    }
}
