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
            string tempDir = Path.GetTempPath();
            string file1 = Path.Combine(tempDir, "test1.meeptest");

            var Watcher = new FileChanges
            {
                Path = tempDir,
                Filter = "*.meeptest"
            };

            Message lastReceived = null;
            Exception lastEx = null;

            using (IDisposable subscription = Watcher.Pipeline.Subscribe<Message>(
                    msg => lastReceived = msg,
                    ex => lastEx = ex,
                    () => Console.WriteLine("Pipeline completed")))
            {
                File.Create(file1);
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
