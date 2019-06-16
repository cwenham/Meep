using System;
using System.Threading.Tasks;
using System.IO;

using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Outputs
{
    /// <summary>
    /// Save message to disk, overwriting any existing file
    /// </summary>
    [Macro(Name = "SaveAs", DefaultProperty = "As", Position = MacroPosition.Downstream)]
    public class Save : AMessageModule
    {
        /// <summary>
        /// Filename and path
        /// </summary>
        /// <value>As.</value>
        public DataSelector As { get; set; }

        /// <summary>
        /// Part of the message to save
        /// </summary>
        /// <value>From.</value>
        /// <remarks>Defaults to saving the whole message serialised to JSON</remarks>
        public DataSelector From { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            if (From == null)
                From = "{msg.AsJSON}";

            MessageContext context = new MessageContext(msg, this);

            string dsAs = null;
            string dsFrom = null;

            try
            {
                dsAs = await As.SelectStringAsync(context);
                dsFrom = await From.SelectStringAsync(context);

                if (String.IsNullOrWhiteSpace(dsAs))
                    return null;

                if (!Directory.Exists(Path.GetDirectoryName(dsAs)))
                    Directory.CreateDirectory(Path.GetDirectoryName(dsAs));

                if (!File.Exists(dsAs))
                    File.Create(dsAs);

                await File.WriteAllTextAsync(dsAs, dsFrom);

                return new LocalisedResource
                {
                    DerivedFrom = msg,
                    Local = dsAs
                };
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "{0} thrown when saving to {1}: {2}", ex.GetType().Name, dsAs, ex.Message);
                return null;
            }
        }
    }
}
