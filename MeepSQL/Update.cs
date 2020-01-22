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
    /// Update rows in a database
    /// </summary>
    [MeepNamespace(ASqlModule.PluginNamespace)]
    [Macro(Name = "Update", DefaultProperty = "DBTable", Position = MacroPosition.Downstream)]
    public class Update : ASqlModule
    {
        /// <summary>
        /// Set clause in {Smart.Format}
        /// </summary>
        /// <value>The set.</value>
        public string Set { get; set; }

        /// <summary>
        /// Where clause in {Smart.Format}
        /// </summary>
        /// <value>The where.</value>
        public string Where { get; set; } = "";

        public override async Task<Message> HandleMessage(Message msg)
        {
            // Handle a mix of message types by rebatching them into groups.
            // We'll only let the database and table vary by the type, not
            // individual messages.
            IEnumerable<Message> messages = null;
            if (Children)
                messages = msg.AsEnumerable();
            else
                messages = new List<Message> { msg };

            var byType = messages.GroupBy(x => x.GetType());

            foreach (var group in byType)
            {
                var sample = group.First();
                MessageContext context = new MessageContext(sample, this);
                string dbName = await Database.SelectStringAsync(context);
                string tableName = await Table.SelectStringAsync(context);
                string[] parameterised = $"UPDATE {tableName} SET {Set} WHERE {Where}".ToSmartParameterised();

                if (String.IsNullOrWhiteSpace(tableName))
                    tableName = sample.TableName();

                Semaphore accessSemaphore = null;
                if (AccessSemaphore.ContainsKey(dbName))
                    accessSemaphore = AccessSemaphore[dbName];
                try
                {
                    accessSemaphore?.WaitOne();
                    using (DbConnection connection = await NewConnection(context))
                    {
                        connection.Open();

                        using (DbCommand cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = parameterised[0];
                            foreach (var m in group)
                            {
                                cmd.Parameters.Clear();
                                MessageContext mContext = new MessageContext(m, this);

                                for (int i = 1; i <= parameterised.Length - 1; i++)
                                {
                                    var sqlParam = cmd.CreateParameter();
                                    sqlParam.ParameterName = String.Format("arg{0}", i);
                                    sqlParam.Value = Smart.Format(parameterised[i], mContext).ToBestType();
                                    cmd.Parameters.Add(sqlParam);
                                }

                                int written = await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error updating DB");
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
