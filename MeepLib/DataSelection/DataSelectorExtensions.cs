using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;

using MeepLib.DataSelection;
using MeepLib.Messages;

namespace MeepLib
{
    /// <summary>
    /// Convenience extensions for DataSelector
    /// </summary>
    public static class DataSelectorExtensions
    {
        /// <summary>
        /// Return the first string selected from a MessageContext
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <remarks>For when we're only expecting, or only need one result.</remarks>
        public async static Task<string> SelectStringAsync(this DataSelector selector, MessageContext context)
        {
            // At time-of-writing, Microsoft hadn't added all the LINQ extension methods for IAsyncEnumerable (there
            // would be between 200-600 of them, and I couldn't find any clear discussion about what they were 
            // planning), so we need to do it ourselves.
            var enumerator = selector.Select(context).GetAsyncEnumerator();
            if (await enumerator.MoveNextAsync())
                return enumerator.Current.ToString();

            return null;
        }

        public async static Task<ReadOnlyMemory<byte>> SelectROMByteAsync(this DataSelector selector,
                                                                          MessageContext context,
                                                                          Encoding encoding = null)
        {
            if (encoding is null)
                encoding = Encoding.UTF8;

            // ToDo: I'm expecting there to be a more efficient way of doing this, probably by going to each of the
            // DataSelector subclasses and looking for methods unique to just SmartFormat, JPath, XPath, etc.
            // Until then, this is just a convenience method that does it the dumb way.
            var enumerator = selector.Select(context).GetAsyncEnumerator();
            if (await enumerator.MoveNextAsync())
                return new ReadOnlyMemory<byte>(encoding.GetBytes(enumerator.Current.ToString()));

            return null;
        }

        /// <summary>
        /// Return the first string selected from a MessageContext
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string SelectString(this DataSelector selector, MessageContext context)
        {
            var stringTask = selector.SelectStringAsync(context);
            stringTask.Wait();

            return stringTask.Result;
        }

        /// <summary>
        /// Attempt to return the first long selected from a MessageContext
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async static Task<(bool Parsed,long Value)> TrySelectLongAsync(this DataSelector selector, MessageContext context)
        {
            string val = await selector.SelectStringAsync(context);
            if (String.IsNullOrWhiteSpace(val))
                return (false, 0);

            if (long.TryParse(val, out long result))
                return (true, result);
            else
                return (false, 0);
        }

        /// <summary>
        /// Attempt to return the first long selected from a MessageContext
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static (bool Parsed,long Value) TrySelectLong(this DataSelector selector, MessageContext context)
        {
            var selectTask = selector.TrySelectLongAsync(context);
            selectTask.Wait();

            return selectTask.Result;
        }

        /// <summary>
        /// Attempt to return the first decimal selected from a MessageContext
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async static Task<(bool Parsed, decimal Value)> TrySelectDecimalAsync(this DataSelector selector, MessageContext context)
        {
            string val = await selector.SelectStringAsync(context);
            if (String.IsNullOrWhiteSpace(val))
                return (false, 0);

            if (decimal.TryParse(val, out decimal result))
                return (true, result);
            else
                return (false, 0);
        }

        /// <summary>
        /// Return a String or Batch Message from a MessageContext
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async static Task<Message> SelectMessageAsync(this DataSelector selector, MessageContext context)
        {
            List<object> results = new List<object>();
            var enumerator = selector.Select(context).GetAsyncEnumerator();
            while (await enumerator.MoveNextAsync())
                results.Add(enumerator.Current.ToString());

            if (results.Count > 1)
                return new Batch
                {
                    DerivedFrom = context.msg,
                    Messages = results.Select(x => new StringMessage
                    {
                        DerivedFrom = context.msg,
                        Value = x.ToString()
                    })
                };
            else
                return new StringMessage
                {
                    DerivedFrom = context.msg,
                    Value = results.First().ToString()
                };

        }
    }
}
