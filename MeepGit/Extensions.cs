using System;
using System.IO;

using MeepLib;

namespace MeepGit
{
    /// <summary>
    /// Common functions exposed as extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Generate the ideal working directory based on a repository's URL
        /// </summary>
        /// <returns>The path.</returns>
        /// <param name="repoUrl">Repo URL.</param>
        public static string ToWorkingDirectory(this string repoUrl)
        {
            // For now, just use the bit between last slash and ".git"
            // ToDo: Improve me
            var dir = Path.GetFileName(repoUrl.TrimEnd('/')).Replace(".git", "");
            return Path.Combine(AHostProxy.Current.BaseDirectory,"Repositories", dir);
        }
    }
}
