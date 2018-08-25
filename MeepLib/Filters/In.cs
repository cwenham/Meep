using System;
using System.Threading.Tasks;

using SmartFormat;

using MeepLib.Messages;

namespace MeepLib.Filters
{
    /// <summary>
    /// Filter messages by category
    /// </summary>
    public class In : AFilter
    {
        /// <summary>
        /// Category message must be in, in {Smart.Format}
        /// </summary>
        /// <value>The category.</value>
        public string Category { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run<Message>(() =>
            {
                MessageContext context = new MessageContext(msg, this);
                string cat = Smart.Format(Category, context);

                if (msg.ID.InCategory(cat))
                    return ThisPassedTheTest(msg);

                return ThisFailedTheTest(msg);
            });
        }
    }
}
