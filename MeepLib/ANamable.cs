using System;
using System.Linq;
using System.Collections.Generic;

namespace MeepLib
{
    public interface IParent
    {
        void AddChildren(IEnumerable<ANamable> children);
    }

    public abstract class ANamable
    {
        public static string DefaultNamespace = "http://meep.example.com/Meep/V1";

        public ANamable() : base()
        {
            MessageContext.InvalidateCache();
        }


        /// <summary>
        /// Name of the module
        /// </summary>
        /// <value>The name.</value>
        /// <remarks>This should be unique if it's to be addressed elsewhere in 
        /// the pipeline, such as with the Tap module.</remarks>
        public virtual string Name
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_Name))
                    _Name = this.GetType().Name;

                return _Name;
            }

            set
            {
                _Name = value;

                // Maintain the directory of named modules.
                // This is used by modules that address other modules, such
                // as Tap.
                if (!Phonebook.ContainsKey(_Name))
                    Phonebook.Add(_Name, this);
            }
        }
        private string _Name;

        public T ByName<T>(string name) where T : ANamable
        {
            if (Phonebook.ContainsKey(name))
                return Phonebook[name] as T;
            return null;
        }

        public static IEnumerable<T> InventoryByBase<T>() where T : ANamable
        {
            return Phonebook.Values.OfType<T>();
        }

        protected static Dictionary<string, ANamable> Phonebook { get; set; } = new Dictionary<string, ANamable>();
    }
}
