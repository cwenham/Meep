using System;
using System.IO;
using System.Reactive.Linq;

using Xunit;

using MeepLib.Sources;
using MeepLib.Messages;

namespace MeepLibTests.Outputs
{
    public class FSWTests
    {
        [Fact]
        public void FSWTest()
        {
            System.Random rand = new System.Random();

            string tempDir = Path.GetTempPath();
            string file1 = Path.Combine(tempDir, String.Format("test{0}.meeptest", rand.Next(1000)));
            if (File.Exists(file1))
                File.Delete(file1);

            var Watcher = new FileChanges
            {
                Path = tempDir,
                DryStart = true,
                Filter = "*.meeptest"
            };

            Message lastReceived = null;
            Exception lastEx = null;

            using (IDisposable subscription = Watcher.Pipeline.Subscribe<Message>(
                    msg => lastReceived = msg,
                    ex => lastEx = ex,
                    () => Console.WriteLine("Pipeline completed")))
            {
                var fstream = File.Create(file1);
                fstream.Close();
                System.Threading.Thread.Sleep(300);
                Assert.NotNull(lastReceived);
                Assert.Null(lastEx);

                Assert.IsType(typeof(FileChanged), lastReceived);
                FileChanged change = lastReceived as FileChanged;
                Assert.Equal(file1, change.FullPath);
                Assert.Equal(WatcherChangeTypes.Created, change.ChangeType);
            }


            File.Delete(file1);
        }
    }
}
