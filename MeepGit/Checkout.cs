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
    /// Checkout a branch/tag
    /// </summary>
    [XmlRoot(ElementName = "Checkout", Namespace = "http://meep.example.com/MeepGit/V1")]
    public class Checkout : AMessageModule
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

        /// <summary>
        /// Branch to switch to in {Smart.Format}
        /// </summary>
        /// <value>The branch.</value>
        /// <remarks>Defaults to master</remarks>
        [XmlAttribute]
        public string Branch { get; set; } = "master";

        /// <summary>
        /// Commit or tag in {Smart.Format}
        /// </summary>
        /// <value>The commit.</value>
        /// <remarks>Defaults to head</remarks>
        [XmlAttribute]
        public string Commit { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);

            string repoURL = Smart.Format(Repository, context);
            string branch = Smart.Format(Branch, context);
            string commit = Smart.Format(Commit, context);
            string workDir = Smart.Format(WorkingDir, context);
            if (String.IsNullOrWhiteSpace(workDir))
                workDir = repoURL.ToWorkingDirectory();
                
            try
            {
                using (Repository repo = new Repository(workDir))
                {
                    //ToDo: implement me
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{0} thrown when checking out: {1}", ex.GetType().Name, ex.Message);
                return null;
            }

            return msg;
        }
    }
}
