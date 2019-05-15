using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;

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
        private Queue<string> PossiblePaths;

        public override bool Read()
        {
            bool read = base.Read();

            string filename = null;

            // Populate the queue with primary filenames and Fallback paths
            if (read && _Reader.NodeType == XmlNodeType.Element)
                if (_Reader.NamespaceURI == ANamable.DefaultNamespace)
                    switch (_Reader.LocalName)
                    {
                        case "Plugin":
                            PossiblePaths = new Queue<string>();
                            filename = GetAttribute("File");
                            if (!String.IsNullOrWhiteSpace(filename))
                                PossiblePaths.Enqueue(filename);
                            break;
                        case "Fallback":
                            if (PossiblePaths != null)
                            {
                                filename = GetAttribute("File");
                                if (!String.IsNullOrWhiteSpace(filename))
                                    PossiblePaths.Enqueue(filename);
                            }
                            break;
                        default:
                            break;
                    }

            // Go through the queue and try to load the plugin once we reach a </Plugin> or <Plugin .../>
            if (read && _Reader.LocalName == "Plugin" && PossiblePaths != null && (_Reader.NodeType == XmlNodeType.EndElement || _Reader.IsEmptyElement))
            {
                bool loaded = false;
                while (!loaded && PossiblePaths.Count > 0)
                    try
                    {
                        filename = PossiblePaths.Dequeue();
                        if (filename != null)
                        {
                            string filepath = null;
                            if (File.Exists(filename))
                                filepath = filename;
                            else
                                filepath = Path.Combine(pluginDir, filename);

                            if (File.Exists(filepath))
                            {
                                Assembly.LoadFrom(filepath);
                                XMeeplangDeserialiser.InvalidateCache();
                                loaded = true;
                            }
                            else
                                logger.Warn($"Plugin {filename} not found");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, $"{ex.GetType().Name} thrown when loading plugin: {ex.Message}");
                    }

                PossiblePaths = null;
            }

            return read;
        }
    }
}
