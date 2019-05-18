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
            }
        }
        private string _Name;

        public T ByName<T>(string name) where T : ANamable
        {
            var root = GetRoot();

            if (root != null)
                return root.MineByName<T>(name);

            return MineByName<T>(name);
        }

        private T MineByName<T>(string name) where T : ANamable
        {
            if (Name.Equals(name))
                return this as T;

            IParent parent = this as IParent;
            if (parent != null)
                return (from c in parent.GetChildren()
                        let d = c.MineByName<T>(name)
                        where d != null
                        select d).FirstOrDefault();

            return null;
        }

        public IEnumerable<T> InventoryByBase<T>() where T : ANamable
        {
            var root = GetRoot();
            if (this == root)
                return MyInventoryByBase<T>();
            else
                return root.MyInventoryByBase<T>();
        }

        private IEnumerable<T> MyInventoryByBase<T>() where T : ANamable
        {
            var mine = new List<T>();
            T myself = this as T;
            if (myself != null)
                mine.Add(myself);

            IParent parent = this as IParent;
            if (parent != null)
                mine.AddRange(parent.GetChildren().SelectMany(x => x.MyInventoryByBase<T>()));

            return mine;
        }

        private ANamable GetRoot()
        {
            Pipeline pipeRoot = this as Pipeline;
            if (pipeRoot != null)
                return pipeRoot;

            IChild ancestor = this as IChild;
            if (ancestor != null && ancestor.Parent != null && ancestor.Parent != this)
                return ancestor.Parent.GetRoot();

            return this;
        }
    }
}
