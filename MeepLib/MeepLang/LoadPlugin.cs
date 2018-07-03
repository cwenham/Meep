using System;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

using SmartFormat;

using MeepLib.Messages;

namespace MeepLib.MeepLang
{
    /// <summary>
    /// Load a plugin assembly into the current AppDomain
    /// </summary>
    /// <remarks>This makes a plugin's modules available for use in the next
    /// pipeline to be instantiated.</remarks>
    public class LoadPlugin : AMessageModule
    {
        /// <summary>
        /// Name of the DLL in {Smart.Format}
        /// </summary>
        /// <value>The name of the dll file.</value>
        /// <remarks>Defaults to the suggested "Plugin.dll" for zipped plugin packages.</remarks>
        public string DLL { get; set; } = "Plugin.dll";

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run<Message>(() =>
            {
                Plugin plugin = msg as Plugin;
                if (plugin == null)
                    return null;

                MessageContext context = new MessageContext(plugin, this);
                string filename = Smart.Format(DLL, context);

                if (!File.Exists(filename))
                    return null;

                if (AHostProxy.Current.DevelMode || plugin.Validates())
                    try
                    {
                        // As an alternative, we could use the Add-ins mechanism
                        // introduced in .net 3.5, but this would also add lots
                        // more complexity. The decision was made to use plain
                        // assembly loading, since we'd get isolation and
                        // unloading capabilities by spawning child-processes of
                        // the Meep host instead.
                        var assembly = Assembly.LoadFrom(filename);
                        return msg;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "{0} thrown when loading plugin {1}: {2}", ex.GetType().Name, filename, ex.Message);
                        return null;
                    }
                return null;
            });
        }
    }
}
