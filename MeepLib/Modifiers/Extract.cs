using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

using NLog;
using SmartFormat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;
using System.Threading.Tasks;

namespace MeepLib.Modifiers
{
    /// <summary>
    /// Extract portion(s) of a message according to an XPath, JSON Path, RegEx or {Smart.Format}
    /// </summary>
    /// <remarks>Packages multiple results as a Batch message that can be unbundled with <see cref="Unbatch"/>.</remarks>
    public class Extract : AMessageModule
    {
        /// <summary>
        /// XPath, JSON Path, RegEx or {Smart.Format}, chosen according to the inbound message type
        /// </summary>
        /// <remarks>Recognises Meep conventions and type prefixes.</remarks>
        public DataSelector From { get; set; }

        /// <summary>
        /// Extract message fragments
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        /// <remarks>Just a wrapper for DataSelector.Select().</remarks>
        public async override Task<Message> HandleMessage(Message msg)
        {
            return await From.SelectMessage(new MessageContext(msg, this));
        }       
    }
}
