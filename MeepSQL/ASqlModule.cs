using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

using SmartFormat;
using NLog;
using System.Data.SQLite;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepSQL
{
    public abstract class ASqlModule : AMessageModule
    {
        public const string PluginNamespace = "http://meep.example.com/MeepSQL/V1";

        /// <summary>
        /// Connection string in {Smart.Format}
        /// </summary>
        /// <value>The connection.</value>
        /// <remarks>Defaults to SQLite</remarks>
        public string Connection { get; set; }

        protected static string _defaultConnection = "Data Source={0}.sqlite";

        /// <summary>
        /// Database name in {Smart.Format}
        /// </summary>
        /// <value>The database.</value>
        /// Use instead of a connection string to default to SQLite
        public string Database { get; set; }

        /// <summary>
        /// Name of table in {Smart.Format} to insert|update
        /// </summary>
        /// <value>The table.</value>
        /// <remarks>Defaults to name of database, meant for one-table stores and "just dump it somewhere" usage.</remarks>
        public string Table { get; set; } = "";

        /// <summary>
        /// Compound Database:Table name
        /// </summary>
        /// <value>Database and table names separated by colon</value>
        /// <remarks>Mainly for use with the macro syntax</remarks>
        public string DBTable
        {
            private get
            {
                return String.Format("{0}:{1}", Database, Table);
            }
            set
            {
                string[] parts = value.Split(':');
                Database = parts[0];
                Table = parts[1];
            }
        }

        protected DbConnection NewConnection(MessageContext context)
        {
            string sfConnectionString = Connection != null ? Smart.Format(Connection, context) : null;
            string sfDatabase = Smart.Format(Database, context);

            if (String.IsNullOrWhiteSpace(sfConnectionString))
            {
                // Default to a SQLite database if no connection string given
                string dbPath = Path.Combine(AHostProxy.Current.BaseDirectory, "Databases");
                if (!Directory.Exists(dbPath))
                    Directory.CreateDirectory(dbPath);

                string dbFile = Path.Combine(dbPath, sfDatabase);
                sfConnectionString = String.Format(_defaultConnection, dbFile);
            }

            return sfConnectionString.ToConnection();
        }
    }
}
