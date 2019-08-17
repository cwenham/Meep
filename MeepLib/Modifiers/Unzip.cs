using System;
using SI = System.IO;
using System.IO.Compression;

using SmartFormat;

using MeepLib.Messages;
using MeepLib.MeepLang;
using System.Threading.Tasks;

namespace MeepLib.Modifiers
{
    [Macro(Name = "Unzip", DefaultProperty = "Path", Position = MacroPosition.Downstream)]
    public class Unzip : AMessageModule
    {
        /// <summary>
        /// Path of zip file
        /// </summary>
        /// <value></value>
        /// <remarks>Defaults to msg.Local, which assumes it's receiving a
        /// LocalisedResource message from just downloading it.</remarks>
        public DataSelector Path { get; set; } = "{msg.Local}";

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            string path = await Path.SelectStringAsync(context);

            return await Task.Run<Message>(() =>
            {
                string extractPath = SI.Path.GetFileName(SI.Path.GetDirectoryName(path));

                try
                {
                    var archive = ZipFile.OpenRead(path);

                    // Do we already have a current extraction?
                    bool differences = false;
                    foreach (var e in archive.Entries)
                    {
                        string extractedFile = SI.Path.Combine(extractPath, e.FullName);

                        SI.FileInfo info = new SI.FileInfo(extractedFile);
                        if (!info.Exists)
                            differences = true;
                        else
                            if (info.Length != e.Length)
                                differences = true;
                            else
                                if (info.LastWriteTime != e.LastWriteTime)
                                    differences = true;

                        if (differences)
                            break;
                    }

                    if (differences)
                        ZipFile.ExtractToDirectory(path, extractPath);

                    return msg;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "{0} thrown when extracting {1}: {2}", ex.GetType().Name, path, ex.Message);
                    return null;
                }

            });
        }
    }
}
