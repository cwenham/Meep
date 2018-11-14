using System;
using System.Threading.Tasks;

using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Filters
{
    /// <summary>
    /// Filter messages by category
    /// </summary>
    [Macro(Name = "In", DefaultProperty = "InOut", Position = MacroPosition.Upstream)]
    public class In : AFilter
    {
        /// <summary>
        /// Category message must be in, in {Smart.Format}
        /// </summary>
        /// <value>The category.</value>
        public string Category { get; set; }

        /// <summary>
        /// Category the message must not be in (the control, eg: "ham")
        /// </summary>
        /// <value>The out.</value>
        public string Out { get; set; }

        /// <summary>
        /// The Category:Out, used for setting by Meeplang macro
        /// </summary>
        /// <value>The in out.</value>
        public string InOut
        {
            get 
            {
                return $"{Category}:{Out}";
            }
            set 
            {
                string[] parts = value.Split(':');
                if (parts != null && parts.Length == 2)
                {
                    Category = parts[0];
                    Out = parts[1];
                }
            }
        }

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run<Message>(() =>
            {
                MessageContext context = new MessageContext(msg, this);
                string sfCat = Smart.Format(Category, context);
                string sfOut = Smart.Format(Out, context);

                if (msg.ID.InCategory(sfCat) && !msg.ID.InCategory(sfOut))
                    return ThisPassedTheTest(msg);

                return ThisFailedTheTest(msg);
            });
        }
    }
}
