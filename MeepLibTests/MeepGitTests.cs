using System;
using System.Xml;
using System.Text;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using Xunit;

using MeepLib;
using MeepLib.Filters;
using MeepLib.Sources;
using MeepLib.MeepLang;
using MeepLib.Messages;

using MeepGit;

namespace MeepLibTests
{
    public class MeepGitTests
    {
        //[Fact]
        public void Clone()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "MeepCookbook");
            if (Directory.Exists(testDir))
                Directory.Delete(testDir);

            var cloner = new Clone
            {
                Repo = "https://github.com/cwenham/{Value}.git",
                WorkingDir = Path.Combine(testDir,"{Value}")
            };

            var task = cloner.HandleMessage(new StringMessage
            {
                Value = "MeepCookbook"
            });
            task.Wait();

            Assert.IsType(typeof(LocalisedResource), task.Result);
            var localised = task.Result as LocalisedResource;
            Assert.Equal("https://github.com/cwenham/Meep.git", localised.URL);
            Assert.Equal(Path.Combine(testDir,"Meep"), localised.Local);
        }
    }
}
