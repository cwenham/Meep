using System;
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
    /// Select rows from database, returning a batch message
    /// </summary>
    /// <remarks>Use the individual clause attributes to specify the columns,
    /// table, where clause, etc., or a &lt;SQL&gt; block for the whole query.
    /// 
    /// <para>The main reason for separating the clauses of the query is to 
    /// make it easier to read and arrange in XML, especially when using 
    /// Smart.Format templates. Otherwise, just use &lt;SQL&gt;.</para></remarks>
    [MeepNamespace(ASqlModule.PluginNamespace)]
    public class Select : ASqlModule
    {
        /// <summary>
        /// Limit the number of results, if Order is specified
        /// </summary>
        /// <value></value>
        public DataSelector Top { get; set; }

        /// <summary>
        /// Specific columns to select
        /// </summary>
        /// <value>Comma separated list, in SQL syntax</value>
        /// <remarks>Defaults to all ('*')</remarks>
        public DataSelector Columns { get; set; } = "*";

        /// <summary>
        /// Where clause in {Smart.Format}
        /// </summary>
        /// <value>The where.</value>
        public DataSelector Where { get; set; } = "";

        /// <summary>
        /// Order By clause
        /// </summary>
        /// <value>The order.</value>
        public DataSelector Order { get; set; } = "";

        /// <summary>
        /// Which named query to use, if any are defined
        /// </summary>
        /// <value>The query.</value>
        public DataSelector Query { get; set; } = "";

        /// <summary>
        /// Full SQL queries in {Smart.Format}
        /// </summary>
        /// <remarks>Ignores the From and Where parameters when you already
        /// know what query you want to run.</remarks>
        public IEnumerable<Config.SQL> Queries
        {
            get
            {
                return Config.OfType<Config.SQL>();
            }
        }

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);

            (bool topDefined, long dsTop) = await Top.TrySelectLong(context);
            string dsTable = await Table.SelectString(context);
            string dsColumn = await Columns.SelectString(context);
            string dsWhere = await Where.SelectString(context);
            string dsOrder = await Order.SelectString(context);
            string dsQueryName = await Query.SelectString(context);
            string dsSql = null;

            // Empty Where
            if (String.IsNullOrWhiteSpace(dsWhere))
                dsWhere = "1 = 1";

            try
            {
                var namedQuery = Queries?.Where(x => x.Name.Equals(dsQueryName)).FirstOrDefault();
                if (namedQuery != null)
                    dsSql = await namedQuery.Content.SelectString(context);
                else
                    dsSql = Smart.Format("SELECT {0} FROM {1} WHERE {2} {3} {4}",
                                         dsColumn,
                                         dsTable,
                                         dsWhere,
                                         !String.IsNullOrWhiteSpace(dsOrder) ? $"ORDER BY {dsOrder}" : "",
                                         topDefined ? $"LIMIT {Top}" : ""
                                        );

                using (DbConnection connection = await NewConnection(context))
                {
                    connection.Open();

                    // ToDo: Work out a sensible way to pass named parameters 
                    // into the query.

                    DbCommand cmd = connection.CreateCommand();
                    cmd.CommandText = dsSql;
                    cmd.CommandType = CommandType.Text;
                    var result = await cmd.ExecuteReaderAsync();

                    if (result.HasRows)
                        return new Batch
                        {
                            DerivedFrom = msg,
                            Name = this.Name,
                            Messages = (from IDataRecord r in result
                                        select new RecordMessage
                                        {
                                            DerivedFrom = msg,
                                            Name = this.Name,
                                            Record = (from i in Enumerable.Range(0, r.FieldCount)
                                                      let fieldName = r.GetName(i)
                                                      select (fieldName, r[fieldName]))
                                             .ToDictionary(x => x.fieldName, y => y.Item2)
                                        }).ToList()
                        };
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"{ex.GetType().Name} thrown for \"{dsSql}\": {ex.Message}");
                return null;
            }
        }

    }
}
