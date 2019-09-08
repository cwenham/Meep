using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

using NLog;

using MeepLib.DataSelection;
using MeepLib.Messages;

// We live outside .DataSelection because we're the Facade to the rest
namespace MeepLib
{
    /// <summary>
    /// A selector, such as an XPath, JPath, Regex, or {Smart.Format} template 
    /// </summary>
    /// <remarks>Supports implicit conversion from strings or via its own TypeConverter so it can be used liberally
    /// as the property type of Message Modules, giving the user the flexibility to use whatever fits the job. Supports
    /// plugins adding their own selector syntax support by implementing ADataSelector and marking with the 
    /// DataSelectorAttribute.
    /// </remarks>
    [TypeConverter(typeof(DataSelectorConverter))]
    public class DataSelector
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();

        public string Value { get; private set; }

        public DataSelector(string value)
        {
            Value = value;
            _configuredSelectors = ConfiguredSelectors(value);
        }

        public IAsyncEnumerable<object> Select(MessageContext context)
        {
            if (_configuredSelectors == null)
                return null;

            // Contexts with no message usually come from setting up the Pipeline before any messages have been created
            if (context.msg is null)
                return _configuredSelectors.First().Value.Select(context);

            var selector = SelectorForMessageType(context.msg.GetType());

            if (selector is null)
                return null;

            return selector.Select(context);
        }

        private Dictionary<Type, ADataSelector> _configuredSelectors;

        private ADataSelector SelectorForMessageType(Type messageType)
        {
            if (_configuredSelectors.ContainsKey(messageType))
                return _configuredSelectors[messageType];

            if (messageType.BaseType != typeof(object))
                return SelectorForMessageType(messageType.BaseType);

            return null;
        }

        /// <summary>
        /// Usable Selector instances keyed by Message type they accept
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private Dictionary<Type, ADataSelector> ConfiguredSelectors(string input)
        {
            var candidates = from s in Selectors
                             where input.StartsWith(s.Key, StringComparison.OrdinalIgnoreCase)
                             let DePrefixed = input.TrimStart(s.Key)
                             let selector = s.Value.Item2.InvokeMember(null, System.Reflection.BindingFlags.CreateInstance, null, null, new object[] { DePrefixed }) as ADataSelector
                             where selector != null
                             select new
                             {
                                 Accepted = s.Value.Item1.MessageType,
                                 Selector = selector
                             };

            var instances = candidates.ToDictionary(x => x.Accepted, y => y.Selector);

            // Default to SmartFormat
            if (!instances.Any())
                instances = new Dictionary<Type, ADataSelector>
                {
                    { typeof(Message), new SmartFormatSelector(input) }
                };
            
            // Add the default
            if (!instances.ContainsKey(typeof(Message)))
                instances.Add(typeof(Message), new SmartFormatSelector(input));

            if (instances.Any())
                return instances;
            else
                return null;
        }

        /// <summary>
        /// Build a cache of all ADataSelectors, keyed by their type prefix and bundled with their accepted Message type
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string,(DataSelectorAttribute,Type)> GetSelectors()
        {
            var validClasses = from a in AppDomain.CurrentDomain.GetAssemblies()
                               from t in a.GetTypes()
                               where t.IsSubclassOf(typeof(ADataSelector))
                               let dsAttrib = t.GetCustomAttributes(typeof(DataSelectorAttribute), true).FirstOrDefault()
                               where dsAttrib != null
                               select new
                               {
                                   Attrib = dsAttrib as DataSelectorAttribute,
                                   Type = t
                               };

            return validClasses.ToDictionary(x => x.Attrib.Prefix, y => (y.Attrib, y.Type));
        }

        /// <summary>
        /// Force the cache of ADataSelector plugins to be rebuilt
        /// </summary>
        /// <remarks>Called whenever a new Meep plugin is loaded.</remarks>
        public static void InvalidateCache()
        {
            _Selectors = null;
        }

        /// <summary>
        /// Private cache of ADataSelector implementations
        /// </summary>
        private static Dictionary<string, (DataSelectorAttribute, Type)> Selectors
        {
            get
            {
                if (_Selectors is null)
                    _Selectors = GetSelectors();

                return _Selectors;
            }
        }
        private static Dictionary<string, (DataSelectorAttribute, Type)> _Selectors = null;

        public static implicit operator DataSelector(string value)
        {
            return new DataSelector(value);
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
