using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;

using Xunit;

using MeepLib.Sources;
using MeepLib.Messages;

namespace MeepLibTests.Sources
{
    public class FileChangedTests
    {
        /// <summary>
        /// Check that it sends an initial listing of files before settling down
        /// for the changes
        /// </summary>
        [Fact]
        public void WetStart()
        {
            string path = Path.GetTempPath();

            List<string> testFiles = new List<string> { "nutmeg.utest", "paprika.utest" };

            foreach (var testfile in testFiles)
                File.WriteAllText(Path.Combine(path, testfile), "Unit test file.");

            FileChanges fc1 = new FileChanges
            {
                Path = path,
                Filter = "*.utest",
                DryStart = false
            };

            int counter = 0;
            fc1.Pipeline.Subscribe(msg =>
            {
                Assert.IsType<FileChanged>(msg);
                Assert.True(testFiles.Contains(Path.GetFileName(((FileChanged)msg).FullPath)));
                counter++;
            });

            System.Threading.Thread.Sleep(500);

            Assert.Equal(testFiles.Count, counter);

            counter = 0;
            Assert.Equal(0, counter);

            File.WriteAllText(Path.Combine(path, testFiles[1]), "Updated.");
            System.Threading.Thread.Sleep(500);
            Assert.Equal(1, counter);

            testFiles.Add("cayenne.utest");
            File.WriteAllText(Path.Combine(path, testFiles[2]), "Created.");
            System.Threading.Thread.Sleep(500);
            Assert.Equal(2, counter);

            foreach (var testfile in testFiles)
            {
                File.Delete(Path.Combine(path, testfile));
                System.Threading.Thread.Sleep(500);
            }
            Assert.Equal(5, counter);
        }
    }
}
