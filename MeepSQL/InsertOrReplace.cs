﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Data;

using SmartFormat;
using NLog;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepSQL
{
    /// <summary>
    /// Insert or update rows in a database
    /// </summary>
    /// <remarks>One message = one row. To populate many rows from a chunk of data like
    /// a CSV you'll need to split them upstream. 
    /// 
    /// Messages can be gathered into Batch messages to make inserts more efficient.
    /// 
    /// <para>Properties of the Message type should match the columns of the table.</para>
    /// 
    /// <para>Table will be created if it doesn't already exist.</para>
    /// </remarks>
    [MeepNamespace(ASqlModule.PluginNamespace)]
    [Macro(Name = "Store", DefaultProperty = "DBTable", Position = MacroPosition.Downstream)]
    public class InsertOrReplace : ASqlModule
    {
        /// <summary>
        /// Unpack Batch messages and insert each child message separately
        /// </summary>
        /// <value></value>
        /// <remarks>Set to false if you're saving details about the batch
        /// itself rather than its contents, such as statistical information.</remarks>
        public bool Unbatch { get; set; } = true;
   
        public override async Task<Message> HandleMessage(Message msg)
        {
            // Handle a mix of message types by rebatching them into groups.
            // We'll only let the database and table vary by the type, not
            // individual messages.
            IEnumerable<Message> messages = null;
            if (Unbatch)
                messages = msg.AsEnumerable();
            else
                messages = new List<Message> { msg };

            var byType = messages.GroupBy(x => x.GetType());

            foreach (var group in byType)
            {
                var sample = group.First();

                MessageContext context = new MessageContext(sample, this);

                string connectionString = Connection != null ? Smart.Format(Connection, context) : null;
                string dbName = Smart.Format(Database, context);
                string tableName = Smart.Format(Table, context);

                if (String.IsNullOrWhiteSpace(connectionString))
                {
                    // Default to a SQLite database if no connection string given
                    string dbPath = Path.Combine(AHostProxy.Current.BaseDirectory, "Databases");
                    if (!Directory.Exists(dbPath))
                        Directory.CreateDirectory(dbPath);

                    string dbFile = Path.Combine(dbPath, dbName);
                    connectionString = String.Format(_defaultConnection, dbFile);
                }

                if (String.IsNullOrWhiteSpace(tableName))
                    tableName = sample.TableName();

                try
                {
                    Config.Table table = ByName<Config.Table>(tableName);
                    if (table is null)
                        table = sample.ToTableDef(tableName);

                    using (DbConnection connection = connectionString.ToConnection())
                    {
                        connection.Open();

                        var tables = connection.GetSchema("Tables");
                        bool tableExists = false;
                        foreach (DataRow t in tables.Rows)
                            if (t.ItemArray[2].ToString() == tableName)
                        {
                                tableExists = true;
                                break;
                        }

                        if (!tableExists)
                        {
                            var ctcmd = connection.CreateCommand();
                            ctcmd.CommandText =  String.Format("BEGIN;\n{0}\nCOMMIT;", table.ToCreateTable());
                            ctcmd.ExecuteScalar();
                        }

                        DbCommand cmd = table.ToInsertCmd(connection, Extensions.InsertOrReplaceTemplate);
                        foreach (var m in group)
                        {
                            MessageContext mContext = new MessageContext(sample, this);
                            cmd = cmd.SetParameters(table, mContext);
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
