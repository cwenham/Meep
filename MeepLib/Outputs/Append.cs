using System;
using System.Threading.Tasks;
using System.IO;

using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Outputs
{
    /// <summary>
    /// Append to the end of an existing file (creating the file if it didn't already exist)
    /// </summary>
    [Macro(Name = "Append", DefaultProperty = "To", Position = MacroPosition.Downstream)]
    public class Append : AMessageModule
    {
        /// <summary>
        /// File path to append to
        /// </summary>
        /// <remarks>Should evaluate to a full filename and either a path relative to Meep's base directory or absolute.</remarks>
        public DataSelector To { get; set; }

        /// <summary>
        /// Value to append
        /// </summary>
        public DataSelector From { get; set; }

        /// <summary>
        /// A header written to the file first if it had to be created because it didn't already exist
        /// </summary>
        public DataSelector Header { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            if (From == null)
                From = "{msg.AsJSON}";

            MessageContext context = new MessageContext(msg, this);

            string dsTo = null;
            string dsFrom = null;

            try
            {
                dsTo = await To.SelectStringAsync(context);
                dsFrom = await From.SelectStringAsync(context);

                if (String.IsNullOrWhiteSpace(dsTo))
                    return null;

                if (!Directory.Exists(Path.GetDirectoryName(dsTo)))
                    Directory.CreateDirectory(Path.GetDirectoryName(dsTo));

                if (!File.Exists(dsTo))
                {
                    File.Create(dsTo);
                    if (Header != null)
                    {
                        string dsHeader = await Header.SelectStringAsync(context);
                        if (!String.IsNullOrWhiteSpace(dsHeader))
                            await File.WriteAllTextAsync(dsTo, dsHeader);
                    }
                }

                await File.AppendAllTextAsync(dsTo, dsFrom);

                return new LocalisedResource
                {
                    DerivedFrom = msg,
                    Local = dsTo
                };
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "{0} thrown when appending to {1}: {2}", ex.GetType().Name, dsTo, ex.Message);
                return null;
            }
        }

    }
}
