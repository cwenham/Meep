using System;

namespace MeepLib
{
    /// <summary>
    /// Clone of System.ComponentModel.DataAnnotations.Schema.IndexAttribute
    /// </summary>
    /// <remarks>This is a temporary fix due to Entity Framework 6 causing problems
    /// with XmlSerialiser, possibly due to some classes that were marked obsolete
    /// and removed but still having a conflict somewhere.
    /// 
    /// <para>Hopefully we can retire this once that problem is solved.</para></remarks>
    public class IndexAttribute : Attribute
    {
        public bool IsUnique { get; set; }
    }
}
