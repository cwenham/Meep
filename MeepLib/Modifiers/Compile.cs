using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using MeepLib.Messages;
using MeepLib.Messages.Compiled;
using MeepLib.Messages.Exceptions;

namespace MeepLib.Modifiers
{
    /// <summary>
    /// Compile source code to a binary
    /// </summary>
    /// <remarks>Currently only supports Regular Expressions, and will return a RegexMessage.</remarks>
    public class Compile : AMessageModule
    {
        /// <summary>
        /// The language the inbound Message is in
        /// </summary>
        public DataSelector Language { get; set; } = "Regex";

        /// <summary>
        /// A selector to get the Source to be compiled
        /// </summary>
        public DataSelector From { get; set; } = "{msg.Value}";

        /// <summary>
        /// A selector to get the Name of the compiled message from
        /// </summary>
        public DataSelector NameFrom { get; set; }

        public async override Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);

            try
            {
                string dsLanguage = await Language.SelectStringAsync(context);
                string dsSource = await From.SelectStringAsync(context);
                string dsName = this.Name;
                if (NameFrom != null)
                    dsName = await NameFrom.SelectStringAsync(context);

                // ToDo: Expose a way for plugins to add compilers.
                // Also: a "safe" taint, like perl, to prevent running potentially hostile code.
                switch (dsLanguage)
                {
                    case "Regex":
                        return new RegexMessage(dsSource, msg, dsName);
                    default:
                        return null;
                }

            }
            catch (Exception ex)
            {
                return new ExceptionMessage(ex);
            }            
        }
    }
}
