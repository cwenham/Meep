using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

using SmartFormat;
using NLog;
using Microsoft.Data.Sqlite;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepSQL
{
    /// <summary>
    /// Insert or update rows in a database
    /// </summary>
    /// <remarks>One message = one row. To populate many rows from a chunk of data like
    /// a CSV you'll need a splitter module upstream.
    /// 
    /// <para>Properties of the Message type should match the columns of the table.</para>
    /// 
    /// <para>Table will be created if it doesn't already exist.</para>
    /// </remarks>
    [XmlRoot(ElementName = "Upsert", Namespace = "http://meep.example.com/MeepSQL/V1")]
    [Macro(Name = "Save", DefaultProperty = "DBTable", Position = MacroPosition.Downstream)]
    public class Upsert : AMessageModule
    {
        /// <summary>
        /// Connection string in {Smart.Format}
        /// </summary>
        /// <value>The connection.</value>
        /// <remarks>Defaults to SQLite</remarks>
        [XmlAttribute]
        public string Connection { get; set; }

        private static string _defaultConnection = "Data Source={0}.sqlite";

        /// <summary>
        /// Database name in {Smart.Format}
        /// </summary>
        /// <value>The database.</value>
        /// Use instead of a connection string to default to SQLite
        [XmlAttribute]
        public string Database { get; set; }

        /// <summary>
        /// Name of table in {Smart.Format} to insert|update
        /// </summary>
        /// <value>The table.</value>
        /// <remarks>Defaults to name of database, meant for one-table stores and "just dump it somewhere" usage.</remarks>
        [XmlAttribute]
        public string Table { get; set; }

        /// <summary>
        /// Compound Database:Table name
        /// </summary>
        /// <value>Database and table names separated by colon</value>
        /// <remarks>Mainly for use with the macro syntax</remarks>
        [XmlAttribute]
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

        public override async Task<Message> HandleMessage(Message msg)
        {
            var byType = msg.AsEnumerable().GroupBy(x => x.GetType());

            foreach (var group in byType)
            {
                var sample = group.First();

                MessageContext context = new MessageContext(sample, this);

                string connectionString = Connection != null ? Smart.Format(Connection, context) : null;
                string dbName = Smart.Format(Database, context);
                string tableName = Smart.Format(Table, context);

                if (String.IsNullOrWhiteSpace(connectionString))
                {
                    string dbPath = Path.Combine(AHostProxy.Current.BaseDirectory, "Databases");
                    if (!Directory.Exists(dbPath))
                        Directory.CreateDirectory(dbPath);

                    string dbFile = Path.Combine(dbPath, dbName);
                    connectionString = String.Format(_defaultConnection, dbFile);
                }

                if (String.IsNullOrWhiteSpace(tableName))
                    tableName = dbName;

                try
                {
                    // Warning: At the moment, .ToConnection() blindly assumes its always SQLite
                    using (DbConnection connection = connectionString.ToConnection())
                    {
                        connection.Open();

                        // Microsoft.Data.Sqlite doesn't support GetSchema, so we have to
                        // wait for System.Data.SQLite 0.109 to be released, which does,
                        // and will tell us if the table already exists.
                        // In the mean time, we'll rely on CREATE TABLE IF NOT EXISTS
                        // because it's not worth using the workaround just to throw
                        // it away in a few days/weeks.
                        //var tables = connection.GetSchema("Tables");
                        //bool tableExists = tables.Rows.Contains(tableName);
                        bool tableExists = false;

                        // ToDo: develop something more flexible that supports user
                        // defined schemas. For now we'll just use the message's structure.

                        if (!tableExists)
                        {
                            var ctcmd = connection.CreateCommand();
                            ctcmd.CommandText = sample.ToCreateTable(tableName);
                            ctcmd.ExecuteScalar();
                        }

                        foreach (var m in group)
                        {
                            DbCommand cmd = m.ToInsertOrReplace(connection, tableName);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error saving to DB");
                }
            }

            return msg;
        }
    }
}
