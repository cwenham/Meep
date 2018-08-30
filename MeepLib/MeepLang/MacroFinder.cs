using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace MeepLib.MeepLang
{
    /// <summary>
    /// Methods for finding Meeplang macro implementations
    /// </summary>
    public static class MacroFinder
    {
        private static Dictionary<string, (Type, MacroAttribute)> Macros;

        public static void DirtyCache()
        {
            Macros = null;
        }

        private static readonly object sync = new { };

        public static (Type, MacroAttribute) GetMacro(string nspace, string attribName, MacroPosition position)
        {
            lock (sync)
            {
                if (Macros == null)
                    Macros = (from a in AppDomain.CurrentDomain.GetAssemblies()
                              from t in a.GetTypes()
                              let macro = t.GetMacro()
                              where macro != null
                              let xr = t.GetMeepNamespace()
                              let ns = xr?.Namespace ?? ANamable.DefaultNamespace
                              let keyname = $"{ns}:{macro.Name}"
                              select new { keyname, t, macro })
                        .ToDictionary(x => x.keyname, y => (y.t, y.macro));
            }

            if (nspace == null)
                nspace = "";
            string key = $"{nspace}:{attribName}";

            var candidate = Macros.ContainsKey(key) ? Macros[key] : (null, null);
            if (candidate.Item1 != null && candidate.Item2.Position == position)
                return candidate;
            else
                return (null, null);
        }
    }
}
