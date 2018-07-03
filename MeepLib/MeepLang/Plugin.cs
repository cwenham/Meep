using System;
using System.Xml;
using System.IO;

using MeepLib.Messages;

namespace MeepLib.MeepLang
{
    /// <summary>
    /// Meeplang support for plugins
    /// </summary>
    /// <remarks>Plugins add modules to the Meep vocabulary and, at minimum,
    /// only need one class that inherits from AMessageModule and overrides
    /// the HandleMessage(). Meep tries to be as "Your Code Here"-compatible as 
    /// it can.
    /// 
    /// <para>Plugins can grow beyond this by the number of modules they expose, 
    /// their integration into the pipeline (by overriding the Pipeline property), 
    /// and their integration into Meeplang (with MacroAttribute).</para>
    /// 
    /// <para>Most of the 1st-party plugins exist to isolate the core library
    /// from 3rd party and platform specific code so the core can remain as
    /// portable as possible. E.G.: MeepGit, which relies on libgit2.</para>
    /// 
    /// <para>Plugins must have their own namespace and, if possible, to use 
    /// the same URL where the plugin binary package can be downloaded. Meep 
    /// will attempt to find the package there if no other location is found 
    /// first. Authors are also encouraged to publish the SHA256 hash of the 
    /// package, which Meep will automatically use for validation.</para>
    /// 
    /// <para>Plugins should not mix modules from more than one namespace in
    /// the same package, or Meep will reject it. This was done to simplify
    /// implementation on this end.</para>
    /// 
    /// <para>Packages are Zip files that contain one or more DLLs named after
    /// the platform and architecture they're compiled for. EG: x86_Win64.dll
    /// or ARM_iOS.dll or Any_All.dll.</para>
    /// <!-- ToDo: find out if that's really necessary -->
    /// </remarks>
    public class Plugin : LocalisedResource
    {
        /// <summary>
        /// Namespace for plugin's modules
        /// </summary>
        /// <value>The namespace.</value>
        /// <remarks>If possible, use the public download URL as the namespace.
        /// This is a convenience measure so users don't have to write a
        /// separate &lt;Plugin&gt; delcaration.</remarks>
        public string Namespace { get; set; }

        /// <summary>
        /// SHA256 of the package file
        /// </summary>
        /// <value>The SHA 256.</value>
        /// <remarks>If set, Meep will refuse to load the plugin if it doesn't
        /// match this signature.
        /// 
        /// <para>If using the namespace-only syntax (namespace is the download
        /// URL), the signature can be passed as a root element attribute with
        /// the namespace prefix. E.G.:</para>
        /// 
        /// <code>&lt;Pipeline ...
        /// xmlns:rq="http://example.com/meep/RabbitMeepQ"
        /// rq:SHA256="XYZ123..."
        /// ... &gt; 
        /// </code>
        /// 
        /// </remarks>
        public string SHA256 { get; set; }

        /// <summary>
        /// Public download URL, if it isn't the same as the namespace
        /// </summary>
        /// <value>The base URL.</value>
        public string BaseURL
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_baseURL))
                    _baseURL = Namespace;

                return _baseURL;
            }
            set
            {
                _baseURL = value;
            }
        }
        private string _baseURL;

        /// <summary>
        /// Local directory where the plugin contents were/are unzipped to
        /// </summary>
        /// <value>The local dir.</value>
        /// <remarks>This will be a directory under /Plugins in the Meep host's
        /// directory. EG: Meep/Plugins/RabbitMeepQ</remarks>
        public string LocalDir { get; set; }

        /// <summary>
        /// Validate the plugin package aganst the SHA256 signature
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool Validates()
        {
            return false;
        }

        private string PluginDirectory
        {
            get
            {
                return Path.Combine(AHostProxy.Current.BaseDirectory, "Plugins", Path.GetDirectoryName(BaseURL));
            }
        }
    }
}
