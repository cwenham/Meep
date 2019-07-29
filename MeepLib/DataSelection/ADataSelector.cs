using System;
using System.Collections.Generic;
using System.Text;

using NLog;

using MeepLib.Messages;

namespace MeepLib.DataSelection
{
    /// <summary>
    /// Base class for DataSelectors, which pluck values from messages given a template in some format
    /// </summary>
    /// <remarks>We want to support many syntaxes for "getting a thing", like XPath, Regular Expressions, JPath,
    /// {Smart.Format} and so-on. This should be extendable with plugins and be reasonable about letting us pick which
    /// one we want: either a prefix in the template (like "XP:" for XPath), or a smart selection made by considering
    /// the Message type.</remarks>
    public abstract class ADataSelector
    {
        protected Logger logger = LogManager.GetCurrentClassLogger();

        public ADataSelector(string template)
        {
        }

        public virtual string Template { get; protected set; }

        /// <summary>
        /// Parse an input message and return 1 or more selections
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public abstract IAsyncEnumerable<object> Select(MessageContext context);
    }

    /// <summary>
    /// Mark a class as a DataSelector with its type prefix
    /// </summary>
    /// <remarks>A type prefix should be short and easy to remember. Include separating characters like the colon,
    /// E.G.: "EG:".</remarks>
    [System.AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    sealed class DataSelectorAttribute : Attribute
    {
        /// <summary>
        /// The type prefix that identifies a selector string's syntax
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// The Message type we prefer to work with
        /// </summary>
        /// <remarks>To allow for multiple implementations of the same selector syntax. The biggest reason for having
        /// different implementations is to separate the support for static values vs. streaming.
        /// 
        /// <para>E.G.: XPath can either work on an XDocument fully parsed and read into memory, or it can work on an 
        /// XmlReader that's still midway through reading the document from a network stream. You would support both
        /// cases by having an ADataSelector that takes an <see cref="XContainerMessage"/> and another that takes an 
        /// <see cref="XMLMessage"/>, respectively. The choice will be made separately by <see cref="DataSelector"/> 
        /// for each inbound message.</para>
        /// 
        /// <para>It's recommended to check for a default implementation that can handle a plain <see cref="Message"/>, 
        /// and create one if it doesn't already exist.</para></remarks>
        public Type MessageType { get; set; } = typeof(Messages.Message);
    }
}
