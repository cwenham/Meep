using System;
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

        public override bool Read()
        {
            bool read = base.Read();

            if (read && _Reader.NodeType == XmlNodeType.Element)
                if (_Reader.NamespaceURI == ANamable.DefaultNamespace && _Reader.LocalName == "Plugin")
                    try
                    {
                        string filename = GetAttribute("File");
                        if (filename != null)
                        {
                            string filepath = null;
                            if (File.Exists(filename))
                                filepath = filename;
                            else
                                filepath = Path.Combine(pluginDir, filename);

                            if (File.Exists(filepath))
                                Assembly.LoadFrom(filepath);
                            else
                                logger.Warn($"Plugin {filename} not found");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"{ex.GetType().Name} thrown when loading plugin: {ex.Message}");
                    }

            return read;
        }
    }
}
