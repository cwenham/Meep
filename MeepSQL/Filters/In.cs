using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Data;
using System.Data.Common;

using SmartFormat;
using NLog;

using MeepLib;
using MeepLib.Filters;
using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepSQL.Filters
{
    /// <summary>
    /// Passes a message through if it is, or is not found by a SQL query
    /// </summary>
    [MeepNamespace(ASqlModule.PluginNamespace)]
    public class In : ASqlModule, IPolarisedFilter
    {
        /// <summary>
        /// Column to search for value
        /// </summary>
        /// <value>The column name.</value>
        public DataSelector Column { get; set; }

        /// <summary>
        /// The value to search for (Supports Meep type prefixes)
        /// </summary>
        public DataSelector From { get; set; }

        /// <summary>
        /// Which named query to use, if any are defined
        /// </summary>
        /// <value>The query.</value>
        public DataSelector Query { get; set; }

        /// <summary>
        /// Full SQL queries in {Smart.Format}
        /// </summary>
        /// <remarks>Ignores the From and Column parameters when you already know what query you want to run.</remarks>
        public IEnumerable<Config.SQL> Queries
        {
            get
            {
                return Config.OfType<Config.SQL>();
            }
        }

        public string Polarity
        {
            get
            {
                return BlockOnMatch ? "Block" : "Pass";
            }
            set
            {
                BlockOnMatch = !value.Equals("PASS", StringComparison.OrdinalIgnoreCase);
            }
        }

        public async override Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);

            string dbName = Database != null ? await Database.SelectStringAsync(context) : null;
            string dsTable = Table != null ? await Table?.SelectStringAsync(context) : null;
            string dsColumn = Column != null ? await Column?.SelectStringAsync(context) : null;
            string dsFrom = From != null ? await From?.SelectStringAsync(context) : null;
            string dsQueryName = Query != null ? await Query?.SelectStringAsync(context) : null;
            string sfSql = null;

            bool hasRows = false;
            Semaphore accessSemaphore = null;
            if (AccessSemaphore.ContainsKey(dbName))
                accessSemaphore = AccessSemaphore[dbName];
            try
            {
                string[] paramValues;

                var namedQuery = dsQueryName != null ? Queries?.Where(x => x.Name.Equals(dsQueryName)).FirstOrDefault() : null;
                if (namedQuery != null)
                {
                    string[] parameterised = namedQuery.Content.Value.ToSmartParameterised("@arg{0}");
                    sfSql = parameterised[0];

                    var selectors = (from p in parameterised.Skip(1)
                                     select new DataSelector(p)).ToArray();

                    paramValues = new string[parameterised.Length - 1];
                    for (int i = 0; i < paramValues.Length; i++)
                        paramValues[i] = await selectors[i].SelectStringAsync(context);
                }
                else
                {
                    sfSql = Smart.Format("SELECT 1 FROM {0} WHERE {1} = @arg1",
                                         dsTable,
                                         dsColumn);

                    paramValues = new string[1];
                    paramValues[0] = dsFrom;
                }

                accessSemaphore?.WaitOne();
                using (DbConnection connection = await NewConnection(context))
                {
                    connection.Open();

                    DbCommand cmd = connection.CreateCommand();
                    cmd.CommandText = sfSql;
                    cmd.CommandType = CommandType.Text;

                    for (int i = 0; i < paramValues.Length; i++)
                    {
                        var param = cmd.CreateParameter();
                        param.ParameterName = String.Format("arg{0}", i+1);
                        param.Value = paramValues[i];
                        cmd.Parameters.Add(param);
                    }

                    var result = await cmd.ExecuteReaderAsync();
                    hasRows = result.HasRows;
                    result.Close();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"{ex.GetType().Name} thrown for \"{this.Name}\": {ex.Message}");
                return null;
            }
            finally
            {
                accessSemaphore?.Release();
            }

            // Wait to get out of the try/using/finally block for lock sensitive databases like SQLite
            if (hasRows)
                return ThisPassedTheTest(msg);
            else
                return ThisFailedTheTest(msg);
        }

        public bool BlockOnMatch { get; set; }

        public Message ThisFailedTheTest(Message msg)
        {
            if (!BlockOnMatch)
                return null;
            else
                return msg;
        }

        public Message ThisPassedTheTest(Message msg)
        {
            if (BlockOnMatch)
                return null;
            else
                return msg;
        }
    }
}
