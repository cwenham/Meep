using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Threading;

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
        /// Connection string
        /// </summary>
        /// <value>The connection.</value>
        /// <remarks>Defaults to SQLite</remarks>
        public DataSelector Connection { get; set; }

        protected static string _defaultConnection = "Data Source={0}.sqlite";

        /// <summary>
        /// Database name
        /// </summary>
        /// <value>The database.</value>
        /// Use instead of a connection string to default to SQLite
        public DataSelector Database { get; set; }

        /// <summary>
        /// Name of table in {Smart.Format} to insert|update
        /// </summary>
        /// <value>The table.</value>
        /// <remarks>Defaults to name of database, meant for one-table stores and "just dump it somewhere" usage.</remarks>
        public DataSelector Table { get; set; }

        /// <summary>
        /// Compound Database:Table name
        /// </summary>
        /// <value>Database and table names separated by colon</value>
        /// <remarks>For use with the macro syntax</remarks>
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

        /// <summary>
        /// Unpack Batch messages and treat each child message separately
        /// </summary>
        /// <value></value>
        /// <remarks>Set to false if you're saving details about the batch
        /// itself rather than its contents, such as statistical information.</remarks>
        public bool Unbatch { get; set; } = true;

        /// <summary>
        /// Dictionary of mutexes for serialising access to SQLite databases
        /// </summary>
        /// <remarks>Descendant modules are expected to make use of this as needed.</remarks>
        protected static ConcurrentDictionary<string, Mutex> AccessMutex = new ConcurrentDictionary<string, Mutex>();

        protected async Task<DbConnection> NewConnection(MessageContext context)
        {
            string dsConnectionString = Connection != null ? await Connection.SelectString(context) : null;
            string dsDatabase = Database != null ? await Database.SelectString(context) : null;

            if (String.IsNullOrWhiteSpace(dsConnectionString))
            {
                // Default to a SQLite database if no connection string given
                string dbPath = Path.Combine(AHostProxy.Current.BaseDirectory, "Databases");
                if (!Directory.Exists(dbPath))
                    Directory.CreateDirectory(dbPath);

                string dbFile = Path.Combine(dbPath, dsDatabase);
                dsConnectionString = String.Format(_defaultConnection, dbFile);

                if (!AccessMutex.ContainsKey(dsDatabase))
                    AccessMutex.TryAdd(dsDatabase, new Mutex());
            }

            return dsConnectionString.ToConnection();
        }
    }
}
