using System;
using System.Collections.Generic;
using System.Text;

namespace MeepLib.DataSelection
{
    /// <summary>
    /// Selectors that support breaking the template into individual, separately-evaluated substrings that can be 
    /// passed as parameters to a query or expression
    /// </summary>
    /// <remarks>EG: Smart.Format exposes a method for discovering individual tokens within a template and replacing
    /// them with placeholder tokens. This means we can use it for SQL or NCalc expressions and pass the parameters
    /// out-of-band in a parameter collection.</remarks>
    public interface IParameterisable
    {
        /// <summary>
        /// Convert a <see cref="DataSelector"/> to a tokenised template and individual parameter selectors
        /// </summary>
        /// <param name="tokenTemplate">Template for token placeholders in the master expression, E.G.: "@arg{0}" would 
        /// be passed by a SQL module, while an NCalc module would give you "[arg{0}]". Use a counter and 
        /// String.Format() to set "{0}" to a unique number and use it as the key for 
        /// <see cref="TokenisedExpresion.ParameterTemplates"/>.</param>
        /// <returns></returns>
        TokenisedExpresion Tokenise(string tokenTemplate);
    }

    public class TokenisedExpresion
    {
        /// <summary>
        /// The main template for the expression
        /// </summary>
        /// <remarks>E.G.: "SELECT foo FROM table WHERE bar = @arg1 AND orange = @arg2"</remarks>
        public string TokenisedExpression { get; set; }

        /// <summary>
        /// Individual DataSelectors for each parameter
        /// </summary>
        /// <remarks>E.G.: 
        /// 
        /// <code>
        /// ParameterTemplates["@arg1"] = new DataSelector("SF:{msg.Value.FirstName}");
        /// </code>
        /// 
        /// </remarks>
        public Dictionary<string,DataSelector> ParameterTemplates { get; set; }
    }
}
