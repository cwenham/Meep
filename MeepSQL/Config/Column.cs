using System;

using MeepLib.Config;
using MeepLib.MeepLang;

namespace MeepSQL.Config
{
    [MeepNamespace(ASqlModule.PluginNamespace)]
    public class Column : AConfig
    {
        /// <summary>
        /// Column data type, defaults to varchar
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; } = "varchar";

        /// <summary>
        /// Max length of varchars and other scalable types, defaults to 256
        /// </summary>
        /// <value>The length.</value>
        public int Length { get; set; } = 256;

        /// <summary>
        /// Add index and specify its type
        /// </summary>
        /// <value>The index.</value>
        public IndexType Index
        {
            get {
                return _index;
            }
            set {
                _index = value;
                if (_index == IndexType.PrimaryKey)
                    Nullable = false;
            }
        }
        private IndexType _index;

        /// <summary>
        /// Can the column value be null
        /// </summary>
        /// <value></value>
        public bool Nullable { get; set; } = true;

        /// <summary>
        /// Default source of value, in {Smart.Format}
        /// </summary>
        /// <value></value>
        /// <remarks>Defaults to message property with the same name.</remarks>
        public string From { get; set; }

        /// <summary>
        /// Override automatic SQL creation with a user-defined column definition
        /// </summary>
        /// <value>The sql.</value>
        /// <remarks>This should be just the line that defines one column in the
        /// CREATE TABLE statement. EG: "fname VARCHAR(64)". Do not follow with
        /// commas.
        /// 
        /// <para>You should set the other properties even if you intend to
        /// provide your own SQL, since this definition may be used in a high
        /// security context where user-defined SQL is ignored.</para></remarks>
        public string SQL { get; set; }
    }

    public enum IndexType 
    {
        None,

        PrimaryKey,

        Unique,

        NotUnique,

        Clustered
    }
}
