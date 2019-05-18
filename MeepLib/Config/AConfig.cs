using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace MeepLib.Config
{
    public abstract class AConfig : ANamable, IChild
    {
        public ANamable Parent { get; private set; }

        public void AddParent(ANamable parent)
        {
            Parent = parent;
        }

        public T FindConfig<T>(string name) where T : AConfig
        {
            return ByName<T>(name);
        }
    }
}
