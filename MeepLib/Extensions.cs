using System;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using SmartFormat;
using SmartFormat.Core.Parsing;
using Newtonsoft.Json;

using MeepLib.Config;
using MeepLib.Messages;
using System.Diagnostics.Contracts;

namespace MeepLib
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Convert line endings to Unix style
        /// </summary>
        /// <returns>The unix endings.</returns>
        /// <param name="text">Text.</param>
        public static string ToUnixEndings(this string text)
        {
            return text.Replace("\r\n", "\n");
        }

        /// <summary>
        /// Fetch the XmlRoot attribute of a type, if any
        /// </summary>
        /// <returns>The xml root.</returns>
        /// <param name="t">T.</param>
        public static XmlRootAttribute GetXmlRoot(this Type t)
        {
            return t.GetCustomAttributes(typeof(XmlRootAttribute), true).Cast<XmlRootAttribute>().FirstOrDefault();
        }

        /// <summary>
        /// Copies the current node of reader to the writer
        /// </summary>
        /// <param name="reader">Reader.</param>
        /// <param name="writer">Writer.</param>
        /// <remarks>Does not copy the child elements like WriteNode() does.</remarks>
        public static void CopyCurrentNode(this XmlReader reader, XmlWriter writer)
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                    writer.WriteAttributes(reader, true);
                    if (!String.IsNullOrWhiteSpace(reader.Value))
                        writer.WriteValue(reader.Value);
                    if (reader.IsEmptyElement)
                        writer.WriteEndElement();
                    break;
                case XmlNodeType.Text:
                    writer.WriteString(reader.Value);
                    break;
                case XmlNodeType.EndElement:
                    writer.WriteEndElement();
                    break;
                case XmlNodeType.Comment:
                    writer.WriteComment(reader.Value);
                    break;
                case XmlNodeType.Whitespace:
                    writer.WriteWhitespace(reader.Value);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Add a module upstream
        /// </summary>
        /// <param name="module">Module.</param>
        /// <remarks>This is used when constructing pipelines in code, not
        /// when deserialising them.</remarks>
        public static void AddUpstream(this AMessageModule parent, AMessageModule module)
        {
            if (parent.Upstreams == null)
                parent.Upstreams = new List<AMessageModule>();

            parent.Upstreams.Add(module);
        }

        /// <summary>
        /// Find the first ancestor in the DerivedFrom chain that matches a type
        /// </summary>
        /// <returns>The ancestor.</returns>
        /// <param name="msg">Message.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static T FirstAncestor<T>(this Message msg) where T : Message
        {
            if (msg == null)
                return null;

            if (msg is T)
                return msg as T;

            if (msg.DerivedFrom == null)
                return null;

            if (msg.DerivedFrom is T)
                return msg.DerivedFrom as T;

            return FirstAncestor<T>(msg.DerivedFrom);
        }

        public static HttpRequestMessage AddHeaders(this HttpRequestMessage req, MessageContext context, IEnumerable<Header> headers)
        {
            var reqHeaders = from h in headers
                             select new
                             {
                                 h.Name,
                                 Value = Smart.Format(h.Value, context)
                             };

            foreach (var h in reqHeaders)
                req.Headers.Add(h.Name, h.Value);

            return req;
        }

        /// <summary>
        /// Try to deserialise a string to a Meep Message, otherwise return
        /// a StringMessage
        /// </summary>
        /// <returns></returns>
        /// <param name="data">Data.</param>
        /// <remarks>Intended for bridging pipelines across IPC or networks
        /// where we don't want to burden the user to write explicit syntax
        /// that it's going to carry Meep Messages, we just want Meep to listen
        /// and go "oh okay, it's a Message".</remarks>
        public static Message DeserialiseOrBust(this string data)
        {
            Contract.Ensures(Contract.Result<Message>() != null);

            if (data[0].Equals('{'))
                try
                {
                    var msg = JsonConvert.DeserializeObject<Message>(data);
                    if (msg != null)
                        return msg;
                }
                catch
                {
                    return new StringMessage(null, data);
                }

            return new StringMessage(null, data);
        }

        /// <summary>
        /// Convert a {Smart.Format} template to a @Parameterised template and
        /// arguments
        /// </summary>
        /// <returns>Parameterised template followed by Smart.Format placeholders.</returns>
        /// <param name="template">Template.</param>
        /// <param name="argName">Function for returning the name of each argument, given the placeholder string.</param>
        /// <remarks>For SQL queries and NCalc expressions that use their own
        /// parameter syntax, but a template in {Smart.Format} is desired instead.
        /// 
        /// <para>E.G.: "UPDATE foo SET bar = {msg.bar} WHERE baz = {msg.baz}"
        /// is the input template, this function will convert it to:</para>
        /// 
        /// <code>UPDATE foo SET bar = @arg1 WHERE baz = @arg2</code>
        /// 
        /// <para>Followed by a collection of strings that consist of "{msg.bar}"
        /// and "{msg.baz}" that can be Smart.Formatted separately and passed
        /// as SQL or NCalc command parameters.</para> 
        /// </remarks>
        public static string[] ToSmartParameterised(this string template, Func<string,string> argName)
        {
            var format = Smart.Default.Parser.ParseFormat(template, null);
            var pieces = (from f in format.Items
                          let isPlaceholder = f is Placeholder
                          select new
                          {
                              isPlaceholder,
                              Raw = f.RawText,
                              Text = isPlaceholder
                                     ? argName(f.RawText)
                                     : f.RawText
                          }).ToList();

            var placeHolders = pieces.Where(x => x.isPlaceholder)
                                     .Select(y => y.Raw)
                                     .ToArray();

            string[] results = new string[placeHolders.Length + 1];
            results[0] = String.Join("", pieces.Select(x => x.Text).ToArray());
            placeHolders.CopyTo(results, 1);

            return results;
        }

        public static string[] ToSmartParameterised(this string template, string argPrefix = "@arg{0}")
        {
            int argCounter = 1;
            Func<string,string> nextArgName = (x) => String.Format(argPrefix, argCounter++);

            return ToSmartParameterised(template, nextArgName);
        }

        /// <summary>
        /// Read all the text from a StreamMessage
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static async Task<string> ReadAllText(this IStreamMessage msg)
        {
            if (msg is null)
                return null;

            if (msg.Stream is null)
                return null;

            var reader = new StreamReader(await msg.Stream);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Identify the likely syntax type (scent) of a string, and strip any type prefixes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>E.G.: if passed "JPATH:packages.richtracks[0]" it'll identify the syntax as JPath and return
        /// "packages.richtracks[0]" as the stripped value. If passed "/(some|regex)/" it'll identify it as RegEx and
        /// return "(some|regex)" as the value.
        /// 
        /// <para>The 'options' return value is for any meta-options detected, such as Regex options, JSON deserialiser
        /// options, etc.</para></remarks>
        public static (DataScent scent, string value, object options) IdentifyAndStrip(this string value)
        {
            DataScent scent = value.SmellsLike();
            string stripped = value;
            object options = null;

            int prefixPos = -1;
            switch (scent)
            {
                case DataScent.Unknown:
                    break;
                case DataScent.URL:
                    stripped = value.TrimStart("URL:");
                    break;
                case DataScent.JSON:
                    stripped = value.TrimStart("JSON:");
                    break;
                case DataScent.XML:
                    break;
                case DataScent.Integer:
                    break;
                case DataScent.Decimal:
                    break;
                case DataScent.XPath:
                    stripped = value.TrimStart("XPath:");
                    break;
                case DataScent.JsonPath:
                    stripped = value.TrimStart("JPath:");
                    break;
                case DataScent.Regex:
                    stripped = value.TrimStart('/').TrimEnd('/');
                    break;
                case DataScent.SmartFormat:
                    stripped = value.TrimStart("SF:");
                    break;
                case DataScent.CLang:
                    break;
                case DataScent.SQLang:
                    stripped = value.TrimStart("SQL:");
                    break;
                case DataScent.UnixPath:
                    break;
                case DataScent.WinPath:
                    break;
                default:
                    break;
            }

            return (scent, stripped, options);
        }

        /// <summary>
        /// Removes leading occurence of a string from the current <see cref="string"/> object
        /// </summary>
        /// <param name="value"></param>
        /// <param name="trimString"></param>
        /// <returns></returns>
        /// <remarks>Supplements the existing TrimStart() methods that only take chars. This will only remove from the beginning of a string.</remarks>
        public static string TrimStart(this string value, string trimString)
        {
            if (value.IndexOf(trimString) == 0)
                return value.Substring(trimString.Length);

            return value;
        }

        /// <summary>
        /// What syntax data appears to be coded in, based on convention and cursory exam
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>Programs like Meep that try to avoid forcing the user to identify the input type need a function 
        /// like this one to look at the input and give an educated guess of its type. Then we know how to parse or 
        /// query it intelligently. When that's not enough, we want to support some standard conventions (like /Regex 
        /// slashes/) and some of our own to positively resolve the syntax in question.
        /// 
        /// <para>A typical use case is in a pipeline definition, the user may be expecting text and wants to extract
        /// fragments with a regex, so they just type &lt;Extract From="/(some|regex)/" ...&gt;. We want to just handle
        /// that the way the user expected, as well as the next time when they expect XML and use an XPath, or JSON and
        /// give us a JPath.</para>
        /// 
        /// <para>Meep also tries to be as fast as it can, so we're not going to try parsing a whole document as JSON 
        /// or XML just to find out if it's well formed. If it's not, then we're going to let that fall back to the 
        /// user to watch the logs and tweak the conventions/parameters to give us the hints we need.</para></remarks>
        public static DataScent SmellsLike(this string value)
        {
            // Go from fastest to slowest. Minimize RegEx and other slow pattern matchers, if they're even used at all

            if (String.IsNullOrWhiteSpace(value))
                return DataScent.Unknown;

            // Meep's global type prefixes, useful when there's ambiguity
            if (value.StartsWith("JPath:"))
                return DataScent.JsonPath;

            if (value.StartsWith("XPath:"))
                return DataScent.XPath;

            if (value.StartsWith("URL:"))   // For relative URLs. We will recognise 'scheme://' on its own
                return DataScent.URL;

            if (value.StartsWith("SF:"))
                return DataScent.SmartFormat;

            if (value.StartsWith("SQL:"))
                return DataScent.SQLang;

            // Do a scan to pick out telltale characters, or the lack of them
            bool couldBeNumber = true;
            int newlines = 0;

            // Index of interesting character counts:
            // 0 = .
            // / = 1
            // () = 2,3
            // [] = 4,5
            // {} = 6,7
            // <> = 8,9
            // '" = 10,11
            string interestingChars = "./()[]{}<>'\"";
            int[] interestingCount = new int[interestingChars.Length];

            foreach (char c in value)
            {
                if (c == '\n')
                {
                    newlines++;
                    continue;
                }

                if (c != '.')
                    if (!((byte)c >= 48 && (byte)c <= 57))
                        couldBeNumber = false;

                for (int i = 0; i < interestingChars.Length; i++)
                    if (c == interestingChars[i])
                        interestingCount[i]++;
            }

            // Easy numbers
            if (couldBeNumber && interestingCount[0] == 1)
                return DataScent.Decimal;

            if (couldBeNumber)
                return DataScent.Integer;

            // The easy ones that begin with forward slashes
            if (value[0] == '/')
            {
                // Easy XPath that begins with "//"
                if (value[1] == '/' && newlines == 0)
                    return DataScent.XPath;

                // Regex by unqualified /slash convention/
                if (value.Length > 2 && value[value.Length - 1] == '/')
                    return DataScent.Regex;
            }

            // Easy ones beginning with '$'
            if (value[0] == '$')
            {
                // JPath doesn't use forward slashes and doesn't support double-quote chars
                if (interestingCount[0] > 0 && interestingCount[1] == 0 && interestingCount[11] == 0)
                    return DataScent.JsonPath;
            }

            // Easy WinPath with drive letter
            if ((byte)value[0] >= 65 && (byte)value[0] <= 90 && value.Substring(1, 2).Equals(":/"))
                return DataScent.WinPath;

            // Is it easily XML?
            if (value.Substring(0, 5).Equals("<?xml", StringComparison.OrdinalIgnoreCase))
                return DataScent.XML;

            // Is it easily JSON?
            if (value[0] == '{' && value[value.Length - 1] == '}' && (interestingCount[4] > 0 && interestingCount[4] == interestingCount[5]))
                return DataScent.JSON;

            if (value[0] == '[' && value[value.Length - 1] == ']' && (interestingCount[6] > 0 && interestingCount[6] == interestingCount[7]))
                return DataScent.JSON;

            // Is it slowly identifiable as XML?
            if (r_SlowIsXML.IsMatch(value))
                return DataScent.XML;

            return DataScent.Unknown;
        }

        // Match on XML without a <?xml> declaration, since we're testing that separately
        private static Regex r_SlowIsXML = new Regex(@"<([\w]+)>((.|\n)*)<\/\1>$", RegexOptions.Compiled);
    }

    /// <summary>
    /// What data "smells" like, based on cursory exam for characteristics and deliberate syntax
    /// </summary>
    public enum DataScent
    {
        Unknown,

        URL,

        JSON,

        XML,

        Integer,

        Decimal,

        XPath,
        
        JsonPath,

        Regex,

        SmartFormat,

        // C-like language (curly braces, etc.)
        CLang,

        // SQL-like language (SELECT, UPDATE, INSERT, DELETEs, etc.)
        SQLang,

        // Forward-slash path, relative or absolute
        UnixPath,

        // Windows style backward-slash path with/without drive letters
        WinPath
    }
}
