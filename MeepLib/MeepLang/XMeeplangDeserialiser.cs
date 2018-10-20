using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using System.ComponentModel;

using MeepLib.Config;

namespace MeepLib.MeepLang
{
    /// <summary>
    /// Deserialise XML to a Meeplang tree
    /// </summary>
    public class XMeeplangDeserialiser
    {
        public AMessageModule Deserialise(XmlReader reader)
        {
            var modules = DeserialiseRecursive(reader);
            return modules.FirstOrDefault() as AMessageModule;
        }

        /// <summary>
        /// Container elements that are invisible to the deserialiser
        /// </summary>
        /// <remarks>These include:
        /// 
        /// <list type="bullet">
        ///     <item>Structural elements that are not modules or config elements
        ///           on their own, such as the container for XIncluded content.</item>
        ///     <item>Elements evaluated by other XmlReader subclasses, such as
        ///           XPluginReader.</item>
        ///     <item>Inline documentation.</item>
        ///     <item>Elements evaluated by other programs when pipelines are
        ///           defined within profiles of mixed scope.</item>
        /// </list>
        /// </remarks>
        public List<string> Invisibles = new List<string> { 
            $"{ANamable.DefaultNamespace}:Config",
            $"{ANamable.DefaultNamespace}:Plugin"
        };

        private Stack<string> _invisiblesStack = new Stack<string>();

        private IEnumerable<ANamable> DeserialiseRecursive(XmlReader reader)
        {
            List<ANamable> modules = new List<ANamable>();

            if (reader.EOF)
                return modules;

            while (reader.Read())
            {
                string fullName = $"{reader.NamespaceURI}:{reader.LocalName}";

                if (reader.NodeType == XmlNodeType.EndElement)
                    if (_invisiblesStack.Count > 0 && _invisiblesStack.Peek() == fullName)
                        _invisiblesStack.Pop();
                    else
                        return modules;

                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (Invisibles.Contains(fullName))
                    {
                        if (!reader.IsEmptyElement)
                            _invisiblesStack.Push(fullName);
                    }
                    else
                    {
                        ANamable newModule = ConfiguredModule(reader);
                        if (newModule != null)
                        {
                            modules.Add(newModule);

                            var newParent = newModule as IParent;
                            if (!reader.IsEmptyElement && newParent != null)
                                newParent.AddChildren(DeserialiseRecursive(reader));
                        }
                        else
                            throw new UnknownElementException(fullName);
                    }
                }
            }

            return modules;
        }

        /// <summary>
        /// Return an instance of a module configured with the attributes in the
        /// current node
        /// </summary>
        /// <returns>The module.</returns>
        private ANamable ConfiguredModule(XmlReader reader)
        {
            if (reader.NodeType != XmlNodeType.Element)
                return null;

            Type imp = FindImplementation(reader.NamespaceURI, reader.LocalName);
            if (imp == null)
                return null;

            ANamable inst = Activator.CreateInstance(imp) as ANamable;
            if (inst == null)
                return null;

            IMeepDeserialisable selfDeserialiser = inst as IMeepDeserialisable;
            if (selfDeserialiser != null)
                selfDeserialiser.ReadXML(reader);
            else
                if (reader.MoveToFirstAttribute())
            {
                var props = imp.GetRuntimeProperties();

                do
                {
                    var prop = props.FirstOrDefault(x => x.Name == reader.LocalName);
                    if (prop != null)
                    {
                        TypeConverter typeConverter = TypeDescriptor.GetConverter(prop.PropertyType);
                        object propValue = typeConverter.ConvertFromString(reader.Value);

                        prop.SetValue(inst, propValue);
                    }
                } while (reader.MoveToNextAttribute());
                reader.MoveToElement();
            }

            return inst;
        }

        public static void InvalidateCache()
        {
            Implementations = null;
        }

        private static Dictionary<string, Type> Implementations;

        private static Type FindImplementation(string namespaceURI, string localName)
        {
            if (Implementations == null)
                Implementations = Implementors<ANamable>();

            string elemName = $"{namespaceURI}:{localName}";
            if (Implementations.ContainsKey(elemName))
                return Implementations[elemName];
            else
                return null;
        }

        private static Dictionary<string, Type> Implementors<T>()
        {
            return (from a in AppDomain.CurrentDomain.GetAssemblies()
                    from t in a.GetTypes()
                    where t.IsSubclassOf(typeof(T))
                    let na = t.GetMeepNamespace()
                    let nspace = na != null ? na.Namespace : ANamable.DefaultNamespace
                    select new { name = $"{nspace}:{t.Name}", type = t })
                    .ToDictionary(x => x.name, y => y.type);
        }
    }

    public class UnknownElementException : Exception
    {
        public string Element { get; set; }

        public UnknownElementException(string element)
        {
            Element = element;
        }
    }
}
