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
        /// Category message must be in
        /// </summary>
        /// <value>The category.</value>
        public DataSelector Category { get; set; }

        /// <summary>
        /// Category the message must not be in (the control, eg: "ham")
        /// </summary>
        /// <value>The out.</value>
        public DataSelector Out { get; set; }

        /// <summary>
        /// The Category:Out, used for setting by Meeplang macro, DO NOT SET DIRECTLY
        /// </summary>
        /// <value></value>
        /// <remarks>Used to support the shorthand macro syntax in pipeline definitions. E.G.:
        ///
        /// <code>&lt;Append To="spam.txt" In="spam:ham"%gt;</code>
        ///
        /// <para>Meep re-writes the "In" attribute to this:</para>
        ///
        /// <code>
        /// &lt;Append To="spam.txt"&gt;
        ///     &lt;In InOut="spam:ham"&gt;
        ///         ... rest of pipeline ...
        ///     &lt;/In&gt;
        /// &lt;/Append&gt;
        /// </code>
        ///
        /// <para>Therefore, do not set this property directly.</para>
        /// </remarks>
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
            MessageContext context = new MessageContext(msg, this);
            string dsCat = await Category.SelectStringAsync(context);
            string dsOut = await Out.SelectStringAsync(context);

            if (msg.ID.InCategory(dsCat) && !msg.ID.InCategory(dsOut))
                return ThisPassedTheTest(msg);

            return ThisFailedTheTest(msg);
        }
    }
}
