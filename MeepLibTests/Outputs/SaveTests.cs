using System;
using System.IO;

using Xunit;

using MeepLib.Outputs;
using MeepLib.Messages;

namespace MeepLibTests.Outputs
{
    public class SaveTests
    {
        [Fact]
        public void SaveText()
        {
            var savePath = Path.GetTempPath();

            var Saver = new Save
            {
                Name = "TestMeepSave",
                As = savePath.Replace(@"\", @"\\") + "{mdl.Name}.txt",
                From = "{msg.Value}"
            };

            var task = Saver.HandleMessage(PlainText);
            task.Wait();

            Assert.NotNull(task.Result);
            Assert.IsType(typeof(LocalisedResource), task.Result);

            LocalisedResource result = task.Result as LocalisedResource;
            Assert.Equal(savePath + "TestMeepSave.txt", result.Local);
            Assert.True(File.Exists(result.Local));

            var actualText = File.ReadAllText(result.Local);
            Assert.Equal(PlainText.Value, actualText);

            File.Delete(result.Local);
        }


        public static StringMessage PlainText = new StringMessage
        {
            Value = "Meeps eat fruit, nuts, leafy vegetables, and continuous streams of complex structured data packaged in discrete messages."
        };
    }
}
