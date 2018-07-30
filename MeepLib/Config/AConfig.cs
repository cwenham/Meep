using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace MeepLib.Config
{
    public abstract class AConfig : ANamable
    {
        public T FindConfig<T>(string name) where T : AConfig
        {
            if (_Phonebook.ContainsKey(name))
                return _Phonebook[name] as T;
            else
                return null;
        }
    }
}
