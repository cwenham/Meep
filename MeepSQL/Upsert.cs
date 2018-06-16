using System;
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
using MeepModel.Messages;

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
    [Macro(Name="SaveDB", DefaultProperty = "DBTable", Position = MacroPosition.Downstream)]
    public class Upsert : AMessageModule
    {
        /// <summary>
        /// Connection string in {Smart.Format}
        /// </summary>
        /// <value>The connection.</value>
        /// <remarks>Defaults to SQLite</remarks>
        [XmlAttribute]
        public string Connection { get; set; }

        private static string _defaultConnection = "Data Source={0}.sqlite;Version=3.0";

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
        [XmlIgnore]
        public string DBTable
        {
            get {
                return String.Format("{0}:{1}", Database, Table);
            }
            set {
                string[] parts = value.Split(':');
                Database = parts[0];
                Table = parts[1];
            }
        }

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);

            string connectionString = Smart.Format(Connection, context);
            string dbName = Smart.Format(Database, context);
            string tableName = Smart.Format(Table, context);

            if (String.IsNullOrWhiteSpace(connectionString))
            {
                string dbFile = Path.Combine(System.Environment.CurrentDirectory, "Databases", dbName);
                connectionString = String.Format(_defaultConnection, dbName);
            }

            if (String.IsNullOrWhiteSpace(tableName))
                tableName = dbName;

            // Warning: At the moment, .ToConnection() blindly assumes its always SQLite
            DbConnection connection = connectionString.ToConnection();

            var tables = connection.GetSchema("Tables");
            if (!tables.Rows.Contains(tableName))
            {
                // Create table
            }

            return await base.HandleMessage(msg);
        }
    }
}
