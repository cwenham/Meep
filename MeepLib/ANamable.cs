using System;
using System.Linq;
using System.Collections.Generic;

namespace MeepLib
{
    public abstract class ANamable
    {
        public static string DefaultNamespace = "http://meep.example.com/Meep/V1";

        /// <summary>
        /// Name of the module
        /// </summary>
        /// <value>The name.</value>
        /// <remarks>This should be unique if it's to be addressed elsewhere in 
        /// the pipeline, such as with the Tap module.</remarks>
        public string Name
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_Name))
                    _Name = this.GetType().Name;

                return _Name;
            }

            set
            {
                // Maintain the directory of named modules.
                // This is used by modules that address other modules, such
                // as Tap.
                if (_Phonebook.ContainsKey(value))
                    _Phonebook.Remove(value);

                _Name = value;

                _Phonebook.Add(_Name, this);
            }
        }
        private string _Name;

        internal static IEnumerable<T> InventoryByBase<T>() where T : ANamable
        {
            return from e in _Phonebook.Values
                   let test = e as T
                   where test != null
                   select test;
        }

        protected static Dictionary<string, ANamable> _Phonebook { get; set; } = new Dictionary<string, ANamable>();
    }
}
