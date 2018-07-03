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

        public static (Type,MacroAttribute) GetMacro(string attribName)
        {
            lock (sync)
            {
                if (Macros == null)
                    Macros = (from a in AppDomain.CurrentDomain.GetAssemblies()
                              from t in a.GetTypes()
                              let macro = t.GetMacro()
                              where macro != null
                              select new {t,macro})
                        .ToDictionary(x => x.macro.Name, y => (y.t, y.macro));
            }

            return Macros.ContainsKey(attribName) ? Macros[attribName] : (null,null);
        }
    }
}
