using System;
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
    /// Git Pull operation (Fetch + Merge)
    /// </summary>
    [MeepNamespace("http://meep.example.com/MeepGit/V1")]
    public class Pull : AMessageModule
    {
        /// <summary>
        /// Working directory in {Smart.Format} of the repository
        /// </summary>
        /// <value>The path.</value>
        /// <remarks>Directory where a repository was previously cloned to.</remarks>
        public string WorkingDir { get; set; }

        /// <summary>
        /// Username in {Smart.Format} for authentication
        /// </summary>
        /// <value>The user.</value>
        public string User { get; set; }

        /// <summary>
        /// Password in {Smart.Format} for authentication
        /// </summary>
        /// <value>The pass.</value>
        public string Pass { get; set; }

        /// <summary>
        /// Username in {Smart.Format} for the merge
        /// </summary>
        /// <value>The merge user.</value>
        public string MergeUser { get; set; }

        /// <summary>
        /// User email in {Smart.Format} for the merge
        /// </summary>
        /// <value>The merge email.</value>
        public string MergeEmail { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);

            return await Task.Run<Message>(() =>
            {
                string sfWorkDir = Smart.Format(WorkingDir, context);
                string sfUser = Smart.Format(User, context);
                string sfPass = Smart.Format(Pass, context);
                string sfMergeUser = Smart.Format(MergeUser, context);
                string sfMergeEmail = Smart.Format(MergeEmail, context);

                Repository repo = new Repository(sfWorkDir);

                PullOptions options = new LibGit2Sharp.PullOptions();

                if (!String.IsNullOrWhiteSpace(sfUser))
                {
                    options.FetchOptions = new FetchOptions();
                    options.FetchOptions.CredentialsProvider = new CredentialsHandler(
                        (url, usernameFromUrl, types) =>
                            new UsernamePasswordCredentials()
                            {
                                Username = sfUser,
                                Password = sfPass
                            });
                }

                var signature = new LibGit2Sharp.Signature(new Identity(sfMergeUser, sfMergeEmail), DateTimeOffset.Now);

                Commands.Pull(repo, signature, options);

                return msg;
            });
        }
    }
}
