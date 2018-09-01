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
    [MeepNamespace(ASqlModule.PluginNamespace)]
    [Macro(Name = "Store", DefaultProperty = "DBTable", Position = MacroPosition.Downstream)]
    public class InsertOrReplace : ASqlModule
    {
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

                        var tables = connection.GetSchema("Tables");
                        bool tableExists = tables.Rows.Contains(tableName);

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
