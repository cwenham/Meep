using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

using NLog;
using SmartFormat;
using Newtonsoft.Json.Linq;

using MeepLib.Messages;

namespace MeepLib
{
    /// <summary>
    /// A selector such as an XPath, JPath, Regex, or {Smart.Format} template 
    /// </summary>
    /// <remarks>Supports implicit conversion from strings or via its own TypeConverter so it can be used as the type
    /// for "From" properties in modules that need to provide a selector that's standardised across Meep.
    /// </remarks>
    [TypeConverter(typeof(DataSelectorConverter))]
    public class DataSelector
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();

        public DataSelector(string value)
        {
            (DataScent scent, string val, object options) = value.IdentifyAndStrip();
            this.Value = val;
            this.Scent = scent;
            this.Options = options;

            if (scent == DataScent.Regex)
                r_Value = new Regex(val, RegexOptions.Compiled);
        }

        public string Value { get; set; }

        public DataScent Scent { get; set; }

        public object Options { get; set; }

        private Regex r_Value { get; set; }

        public static implicit operator DataSelector(string value)
        {
            return new DataSelector(value);
        }

        /// <summary>
        /// A Regex Match from a string (or null if the DataSelector is not a Regex)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Match Match(string input)
        {
            if (r_Value is null)
                return null;

            return r_Value.Match(input);
        }

        /// <summary>
        /// Apply the selection to string input, returning the selected string(s)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <remarks>If the input had to be parsed first, the extracted fragments will be re-serialised back to
        /// strings. If you want them to stay in their parsed format (JObjects and XElements, for example) then
        /// use Select(Message) instead and unwrap the results.</remarks>
        public IEnumerable<string> Select(string input, object smartFormatParameter = null)
        {
            switch (Scent)
            {
                case DataScent.XPath:
                    var doc = XDocument.Parse(input);
                    if (doc != null)
                        foreach (var e in doc.XPathSelectElements(Value))
                            yield return e.ToString();
                    break;
                case DataScent.JsonPath:
                    var jdoc = JObject.Parse(input);
                    if (jdoc != null)
                        foreach (var o in jdoc.SelectTokens(Value))
                            yield return o.ToString();
                    break;
                case DataScent.Regex:
                    var match = Match(input);
                    if (match != null && match.Success)
                        foreach (var g in match.Groups.Skip(1))
                            yield return g.Value;
                    break;
                case DataScent.SmartFormat:
                case DataScent.Unknown:
                case DataScent.URL:
                case DataScent.JSON:
                case DataScent.XML:
                case DataScent.Integer:
                case DataScent.Decimal:
                case DataScent.CLang:
                case DataScent.SQLang:
                case DataScent.UnixPath:
                case DataScent.WinPath:
                default:
                    yield return Smart.Format(Value, smartFormatParameter);
                    break;
            }
            yield break;
        }

        /// <summary>
        /// Apply the selection to a Message, returning a single result in best specific type or a BatchMessage if 
        /// there are multiple results
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="module"></param>
        /// <returns></returns>
        /// <remarks>Mainly for modules like Extract that will return the results directly.</remarks>
        public async Task<Message> Select(Message msg, AMessageModule module = null)
        {
            Task<string> contentTask = null;
            IStreamMessage streamMsg = msg as IStreamMessage;
            if (streamMsg != null)
                contentTask = streamMsg.ReadAllText();

            if (msg is XContainerMessage)
                return new Batch
                {
                    DerivedFrom = msg,
                    Messages = ExtractXPath(((XContainerMessage)msg).Value, msg)
                };

            if (msg is JTokenMessage)
                return new Batch
                {
                    DerivedFrom = msg,
                    Messages = ExtractJPath(((JTokenMessage)msg).Value, msg)
                };

            if (msg is StringMessage)
                return HandleMessage((StringMessage)msg, module);

            if (contentTask != null)
            {
                await contentTask;
                if (!String.IsNullOrWhiteSpace(contentTask?.Result))
                {
                    // Create a proxy StringMessage
                    StringMessage smsg = new StringMessage
                    {
                        DerivedFrom = msg.DerivedFrom,
                        ID = msg.ID,
                        Value = contentTask.Result
                    };
                    return HandleMessage(smsg, module);
                }
            }

            return ExtractSmartFormat(msg, module);
        }

        private Message HandleMessage(StringMessage msg, AMessageModule module = null)
        {
            try
            {
                switch (Scent)
                {
                    case DataScent.XPath:
                        XDocument xdoc = XDocument.Parse(msg.Value);
                        if (xdoc != null)
                            return new Batch
                            {
                                DerivedFrom = msg,
                                Messages = ExtractXPath(xdoc, msg)
                            };
                        break;
                    case DataScent.JsonPath:
                        JObject jdoc = JObject.Parse(msg.Value);
                        if (jdoc != null)
                            return new Batch
                            {
                                DerivedFrom = msg,
                                Messages = ExtractJPath(jdoc, msg)
                            };
                        break;
                    case DataScent.Regex:
                        return new Batch
                        {
                            DerivedFrom = msg,
                            Messages = ExtractRegex(msg.Value, msg)
                        };
                    case DataScent.SmartFormat:
                        return ExtractSmartFormat(msg, module);
                    case DataScent.SQLang:
                        // ToDo: Can we treat msg contents as a DB connection and query it?
                        break;
                    case DataScent.Unknown:
                    case DataScent.URL:
                    case DataScent.JSON:
                    case DataScent.XML:
                    case DataScent.Integer:
                    case DataScent.Decimal:
                    case DataScent.CLang:
                    case DataScent.UnixPath:
                    case DataScent.WinPath:
                    default:
                        break;
                }
                return ExtractSmartFormat(msg, module);
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "{0} thrown when extracting message fragment: {1}", ex.GetType().Name, ex.Message);
                return null;
            }
        }

        private IEnumerable<Message> ExtractXPath(XContainer content, Message msg)
        {
            return from m in content.XPathSelectElements(Value)
                   select new XContainerMessage
                   {
                       DerivedFrom = msg,
                       Value = m
                   };
        }

        private IEnumerable<Message> ExtractJPath(JToken content, Message msg)
        {
            return from t in content.SelectTokens(Value, false)
                   select new JTokenMessage
                   {
                       DerivedFrom = msg,
                       Value = t
                   };
        }

        private Message ExtractSmartFormat(Message msg, AMessageModule module = null)
        {
            try
            {
                MessageContext context = new MessageContext(msg, module);
                string sfFragment = Smart.Format(Value, context);
                if (!String.IsNullOrWhiteSpace(sfFragment))
                    return new StringMessage
                    {
                        DerivedFrom = msg,
                        Value = sfFragment
                    };
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "{0} thrown Extracting from SmartFormat: {1}", ex.GetType().Name, ex.Message);
            }
            return null;
        }

        private IEnumerable<Message> ExtractRegex(string content, Message msg)
        {
            var match = Match(content);

            if (match != null && match.Success)
                return from g in match.Groups.Skip(1)
                       select new StringMessage
                       {
                           DerivedFrom = msg,
                           Value = g.Value
                       };
            else
                return null;
        }
    }

    /// <summary>
    /// Convert a string to a DataSelector
    /// </summary>
    /// <remarks>Lets our deserialiser create a DataSelector from a string attribute, making it dirt easy to add them
    /// as properties to any AMessageModule.</remarks>
    public class DataSelectorConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return false;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string sValue = value as string;
            if (value is null)
                return null;

            return new DataSelector(sValue);
        }
    }
}
