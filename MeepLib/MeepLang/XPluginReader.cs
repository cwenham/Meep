using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Net.Http;
using System.Threading.Tasks;

using SmartFormat;
using NLog;

namespace MeepLib.MeepLang
{
    /// <summary>
    /// Load plugins referenced in namespace URLs or Plugin elements
    /// </summary>
    /// <remarks>Put this after an XIncludingReader but before XDownstreamReader
    /// so it has a chance to load plugins that may have macros for the
    /// Up/Downstream readers to expand.</remarks>
    public class XPluginReader : XmlPassthroughReader
    {
        Logger logger = LogManager.GetCurrentClassLogger();

        public XPluginReader(XmlReader reader) : base(reader)
        {
        }

        private string pluginDir = Path.Combine(AHostProxy.Current.BaseDirectory, "Plugins");

        /// <summary>
        /// Queue of possible paths to the same plugin, populated by the primary element and &lt;Fallback&gt;s
        /// </summary>
        private Queue<string> PossibleURLs;

        public override bool Read()
        {
            bool read = base.Read();

            string url = null;

            // Populate the queue with primary filenames and Fallback paths
            if (read && _Reader.NodeType == XmlNodeType.Element)
                if (_Reader.NamespaceURI == ANamable.DefaultNamespace)
                    switch (_Reader.LocalName)
                    {
                        case "Plugin":
                            PossibleURLs = new Queue<string>();
                            url = GetAttribute("Url") ?? GetAttribute("File");
                            if (!String.IsNullOrWhiteSpace(url))
                                PossibleURLs.Enqueue(url);
                            break;
                        case "Fallback":
                            if (PossibleURLs != null)
                            {
                                url = GetAttribute("Url") ?? GetAttribute("File");
                                if (!String.IsNullOrWhiteSpace(url))
                                    PossibleURLs.Enqueue(url);
                            }
                            break;
                        default:
                            break;
                    }

            // Go through the queue and try to load the plugin once we reach a </Plugin> or <Plugin .../>
            if (read && _Reader.LocalName == "Plugin" && PossibleURLs != null && (_Reader.NodeType == XmlNodeType.EndElement || _Reader.IsEmptyElement))
            {
                bool loaded = false;
                while (!loaded && PossibleURLs.Count > 0)
                    try
                    {
                        url = PossibleURLs.Dequeue();
                        if (url != null)
                        {
                            var task = LoadPlugin(url);
                            task.Wait();
                            loaded = task.Result;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, $"{ex.GetType().Name} thrown when loading plugin: {ex.Message}");
                    }

                if (!loaded)
                    logger.Error("Could not find plugin {0}", url);

                PossibleURLs = null;
            }

            return read;
        }

        public async Task<bool> LoadPlugin(string pluginUrl)
        {
            try
            {
                string filepath = null;

                if (File.Exists(pluginUrl))
                    filepath = pluginUrl;
                else
                {
                    Uri uri = new Uri(pluginUrl);
                    filepath = FindLocalisedPlugin(uri);

                    if (filepath is null)
                        using (HttpClient client = new HttpClient())
                        {
                            var streamTask = client.GetStreamAsync(uri);
                            string localDir = LocalDirectory(uri);
                            if (!Directory.Exists(localDir))
                                Directory.CreateDirectory(localDir);

                            var stream = await streamTask;
                            string localisedFile = Path.Combine(localDir, Path.GetFileName(pluginUrl));
                            using (FileStream fstream = new FileStream(localisedFile, FileMode.CreateNew))
                            {
                                await stream.CopyToAsync(fstream);
                            }

                            if (Path.GetExtension(pluginUrl).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                ZipFile.ExtractToDirectory(localisedFile, localDir);
                            }
                        }

                    filepath = FindLocalisedPlugin(uri);
                }

                if (filepath != null && File.Exists(filepath))
                {
                    Assembly.LoadFrom(filepath);
                    XMeeplangDeserialiser.InvalidateCache();
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{0} thrown trying to load plugin {1}: {2}", ex.GetType().Name, pluginUrl, ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Searches for already existing local plugin
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>Path to DLL, or null if it doesn't appear to be localised yet.</returns>
        private string FindLocalisedPlugin(Uri uri)
        {
            string filepath = null;

            string localDir = LocalDirectory(uri);
            if (!Directory.Exists(localDir))
                return null;

            // Go by convention to derive the plugin DLL's name, taking the name of the
            // plugin directory--itself derived from the URL--and stripping any trailing
            // version numbers that come after a dot.
            string DllName = Path.GetFileName(localDir);
            if (DllName.IndexOf('.') > 0)
                DllName = DllName.Substring(0, DllName.IndexOf('.'));
            DllName = $"{DllName}.dll";

            var searchPaths = SearchTree(localDir);
            foreach (string dir in searchPaths)
            {
                filepath = Path.Combine(dir, DllName);
                if (File.Exists(filepath))
                    break;
            }

            if (File.Exists(filepath))
                return filepath;
            return null;
        }

        private string LocalDirectory(Uri uri)
        {
            string filepath = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            string filename = Path.GetFileNameWithoutExtension(filepath);
            return Path.Combine(AHostProxy.Current.BaseDirectory, "Plugins", filename);
        }

        private IEnumerable<string> SearchTree(string root)
        {
            yield return root;

            foreach (string dir in Directory.EnumerateDirectories(root))
                foreach (string qdir in SearchTree(dir))
                    yield return qdir;

            yield break;
        }
    }
}
