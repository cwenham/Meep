﻿using System;
using System.Linq;
using System.Xml.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Sources
{
    /// <summary>
    /// Download a remote resource to a location with a predictable local path
    /// </summary>
    [XmlRoot(ElementName = "Localise", Namespace = "http://meep.example.com/Meep/V1")]
    [Macro(Name = "Localise", DefaultProperty = "From", Position = MacroPosition.Downstream)]
    public class Localise : AMessageModule
    {
        /// <summary>
        /// URL to download from in {Smart.Format}
        /// </summary>
        /// <value>From.</value>
        [XmlAttribute]
        public string From { get; set; }

        /// <summary>
        /// Subdirectory from Meep's base to store downloaded files
        /// </summary>
        /// <value>The subdir.</value>
        /// <remarks>Usually just "Downloads" but is set to "Plugins" when this
        /// module is used by the plugin system.</remarks>
        [XmlAttribute]
        public string Subdir { get; set; } = "Downloads";

        public override async Task<Message> HandleMessage(Message msg)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    MessageContext context = new MessageContext(msg, this);

                    string url = Smart.Format(From, context);
                    HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url);

                    var rTask = client.SendAsync(req);
                    string cacheFile = Path.Combine(AHostProxy.Current.BaseDirectory, Subdir, url.ToMD5(), Path.GetFileName(url));

                    if (!File.Exists(cacheFile))
                        using (var fs = new FileStream(cacheFile, FileMode.Append))
                        {
                            await rTask;
                            var bTask = rTask.Result.Content.CopyToAsync(fs);
                            await bTask;
                        }

                    return new LocalisedResource
                    {
                        DerivedFrom = msg,
                        URL = url,
                        Local = cacheFile
                    };
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Error localising file");
                return null;
            }
        }
    }
}
