using System;
using System.Linq;

namespace MeepLib.MeepLang
{
    /// <summary>
    /// Attribute for modules that can be configured from an attribute
    /// </summary>
    /// <remarks>This is to enable simplified invokation of modules within the pipeline
    /// with an attribute on an existing element rather than as a separate element.
    /// 
    /// <para>E.G.: the Timer module is normally invoked like this:</para>
    /// 
    /// <code>
    /// &lt;CheckSomething&gt;
    ///     &lt;Timer Interval="00:30:00"/&gt;
    /// &lt;/CheckSomething&gt;
    /// </code>
    /// 
    /// It would be more convenient and readable if we could simply write it like this:
    /// 
    /// <code>
    /// &lt;CheckSomething Interval="00:30:00"/&gt;
    /// </code>
    /// 
    /// To do this, the Timer module would be decorated with a MacroAttribute like this:
    /// 
    /// <code>
    /// [Macro(Name="Interval", Position=MacroPosition.FirstChild, DefaultProperty="Interval"]
    /// public class Timer : AMessageModule
    /// </code>
    /// 
    /// XMeeplangReader will then convert the later example into the former on the fly
    /// before the XML is deserialised into an object tree. 
    /// </remarks>
    public class MacroAttribute : Attribute
    {
        public MacroAttribute()
        {
        }

        /// <summary>
        /// Attribute name
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        public MacroPosition Position { get; set; }

        /// <summary>
        /// NameOf the property to assign the value of the attribute if it doesn't use "Named=Value;" form
        /// </summary>
        /// <value>The default property.</value>
        /// <remarks></remarks>
        public string DefaultProperty { get; set; }
    }

    public enum MacroPosition
    {
        Downstream,         // Insert downstream (after) module
        Upstream,           // Insert upstream (before) module
        FirstUpstream,      // Insert after the target as first child
        LastUpstream        // Insert after the target as last child
    }

    public static class MacroExtension
    {
        public static MacroAttribute GetMacro(this Type t)
        {
            return t.GetCustomAttributes(typeof(MacroAttribute), true).Cast<MacroAttribute>().FirstOrDefault();
        }
    }
}
