using System;
using System.Linq;
using System.Collections.Generic;

using MeepLib;
using MeepLib.Config;
using MeepLib.MeepLang;

namespace MeepSQL.Config
{
    [MeepNamespace(ASqlModule.PluginNamespace)]
    public class Table : AConfig, IParent
    {
        public List<Column> Columns { get; set; } = new List<Column>();

        public SQL Create { get; set; }

        public void AddChildren(IEnumerable<ANamable> children)
        {
            Columns.AddRange(children.OfType<Column>());
            Create = children.OfType<SQL>().FirstOrDefault();
        }
    }
}
