using System;
using System.Linq;
using System.Xml.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;

using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Sources
{
    /// <summary>
    /// Download a remote resource to a location with a predictable local path
    /// </summary>
    [Macro(Name = "Localise", DefaultProperty = "From", Position = MacroPosition.Downstream)]
    public class Localise : AMessageModule
    {
        /// <summary>
        /// URL to download from in {Smart.Format}
        /// </summary>
        /// <value>From.</value>
        public string From { get; set; }

        /// <summary>
        /// Subdirectory to store downloaded files, or "Memory" to keep in RAM 
        /// as a BinaryResource
        /// </summary>
        /// <value>The subdir.</value>
        /// <remarks>Usually just "Downloads" but is set to "Plugins" when this
        /// module is used by the plugin system.</remarks>
        public string To { get; set; } = "Downloads";

        public override async Task<Message> HandleMessage(Message msg)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    MessageContext context = new MessageContext(msg, this);
                    string sfTo = Smart.Format(To, context);
                    string sfFrom = Smart.Format(From, context);
                    HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, sfFrom);

                    var rTask = client.SendAsync(req);

                    if (sfTo.Equals("MEMORY", StringComparison.OrdinalIgnoreCase))
                        return await ToRam(msg, rTask, sfFrom);
                    else
                        return await ToDisk(msg, rTask, sfTo, sfFrom);
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Error localising file");
                return null;
            }
        }

        private async Task<Message> ToDisk(Message msg, Task<HttpResponseMessage> rTask, string sfTo, string sfFrom)
        {
            string cacheDir = Path.Combine(AHostProxy.Current.BaseDirectory, sfTo, sfFrom.ToMD5());
            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);
            string cacheFile = Path.Combine(cacheDir, Path.GetFileName(sfFrom));

            if (!File.Exists(cacheFile))
                using (var fs = new FileStream(cacheFile, FileMode.Append))
                {
                    await rTask;
                    await rTask.Result.Content.CopyToAsync(fs);
                }

            return new LocalisedResource
            {
                DerivedFrom = msg,
                URL = sfFrom,
                Local = cacheFile
            };
        }

        private async Task<Message> ToRam(Message msg, Task<HttpResponseMessage> rTask, string sfFrom)
        {
            await rTask;
            byte[] bytes = await rTask.Result.Content.ReadAsByteArrayAsync();

            return new BinaryResource
            {
                DerivedFrom = msg,
                URL = sfFrom,
                Bytes = bytes
            };
        }
    }
}
