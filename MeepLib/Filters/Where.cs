﻿using System;
using System.Xml.Serialization;

using NCalc;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;
using System.Threading.Tasks;

namespace MeepLib.Filters
{
    /// <summary>
    /// 'Where' condition using compiled NCalc expressions
    /// </summary>
    [XmlRoot(ElementName = "Where", Namespace = "http://meep.example.com/Meep/V1")]
    [Macro(DefaultProperty = "Expr", Name = "Where", Position = MacroPosition.Upstream)]
    public class Where : AMessageModule
    {
        /// <summary>
        /// Expression in [NCalc] format
        /// </summary>
        /// <value>The expr.</value>
        public string Expr
        {
            get {
                return _expr;
            }
            set {
                _expr = value;
                var expression = new Expression(_expr);
                Lambda = expression.ToLambda<Message, bool>();
            }
        }
        private string _expr;

        /// <summary>
        /// Compiled version of the expression
        /// </summary>
        /// <value>The lambda.</value>
        private Func<Message, bool> Lambda { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (Lambda(msg))
                        return msg;

                    return null;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "{0} thrown on Where condition: {1}", ex.GetType().Name, ex.Message);
                    return null;
                }
            });
        }
    }
}
