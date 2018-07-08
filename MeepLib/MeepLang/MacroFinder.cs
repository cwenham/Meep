using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace MeepLib.MeepLang
{
    public static class MacroFinder
    {
        private static Dictionary<string, (Type,MacroAttribute)> Macros;

        public static void DirtyCache()
        {
            Macros = null;
        }

        private static readonly object sync = new { };

        public static (Type,MacroAttribute) GetMacro(string nspace, string attribName)
        {
            lock (sync)
            {
                if (Macros == null)
                    Macros = (from a in AppDomain.CurrentDomain.GetAssemblies()
                              from t in a.GetTypes()
                              let macro = t.GetMacro()
                              where macro != null
                              let xr = t.GetXmlRoot()
                              let ns = xr?.Namespace ?? ""
                              let keyname = $"{ns}:{macro.Name}"
                              select new {keyname,t,macro})
                        .ToDictionary(x => x.keyname, y => (y.t, y.macro));
            }

            if (nspace == null)
                nspace = "";
            string key = $"{nspace}:{attribName}";

            return Macros.ContainsKey(key) ? Macros[key] : (null,null);
        }
    }
}
