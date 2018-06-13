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
using MeepModel.Messages;

using MeepGit;

namespace MeepLibTests
{
    public class MeepGitTests
    {
        [Fact]
        public void Clone()
        {
            var cloner = new Clone
            {
                Repo = "https://github.com/cwenham/{Value}.git",
                WorkingDir = "/Users/cwenham/Documents/Meep/{Value}"
            };

            var task = cloner.HandleMessage(new Message
            {
                Value = "Meep"
            });
            task.Wait();

            Assert.IsType(typeof(LocalisedResource), task.Result);
            var localised = task.Result as LocalisedResource;
            Assert.Equal("https://github.com/cwenham/Meep.git", localised.URL);
            Assert.Equal("/Users/cwenham/Documents/Meep/Meep", localised.Local);
        }
    }
}
