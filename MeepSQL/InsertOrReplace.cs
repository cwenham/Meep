using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
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

                string connectionString = Connection != null ? await Connection.SelectStringAsync(context) : null;
                string dbName = Database != null ? await Database.SelectStringAsync(context) : null;
                string tableName = Table != null ? await Table.SelectStringAsync(context) : null;

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

                Semaphore accessSemaphore = null;
                if (AccessSemaphore.ContainsKey(dbName))
                    accessSemaphore = AccessSemaphore[dbName];

                try
                {
                    Config.Table table = ByName<Config.Table>(tableName);
                    if (table is null)
                        table = sample.ToTableDef(tableName);

                    accessSemaphore?.WaitOne();
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

                            ctcmd.CommandText = String.Format("BEGIN;\n{0}\nCOMMIT;", await table.ToCreateTable(context));
                            ctcmd.ExecuteScalar();
                        }

                        using (DbCommand cmd = table.ToInsertCmd(connection, Extensions.InsertOrReplaceTemplate))
                            foreach (var m in group)
                            {
                                cmd.Parameters.Clear();
                                MessageContext mContext = new MessageContext(m, this);
                                cmd.SetParameters(table, mContext);
                                int written = await cmd.ExecuteNonQueryAsync();
                                if (written == 0)
                                    logger.Warn("Failed to insert to {0}", dbName);
                            }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error saving to DB");
                }
                finally
                {
                    accessSemaphore?.Release();
                }
            }

            return msg;
        }
    }
}
