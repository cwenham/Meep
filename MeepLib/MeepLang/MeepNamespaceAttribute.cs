using System;
using System.Linq;

namespace MeepLib.MeepLang
{
    public class MeepNamespaceAttribute : Attribute
    {
        public MeepNamespaceAttribute(string nspace)
        {
            Namespace = nspace;
        }

        public string Namespace { get; set; }
    }

    public static class MeepNamespaceExtension
    {
        public static MeepNamespaceAttribute GetMeepNamespace(this Type t)
        {
            return t.GetCustomAttributes(typeof(MeepNamespaceAttribute), true).Cast<MeepNamespaceAttribute>().FirstOrDefault();
        }
    }
}
