using System;
using System.Collections.Generic;
using System.Text;

namespace MeepLib
{
    /// <summary>
    /// For classes that contain other ANamables
    /// </summary>
    public interface IParent
    {
        void AddChildren(IEnumerable<ANamable> children);

        IEnumerable<ANamable> GetChildren();
    }

    /// <summary>
    /// For classes contained by an ANamable
    /// </summary>
    /// <remarks>Principally used to pass down references to the phonebook of named modules.</remarks>
    interface IChild
    {
        void AddParent(ANamable parent);

        ANamable Parent { get; }
    }
}
