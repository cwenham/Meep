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

using MeepSQL.Messages;

namespace MeepSQL
{
    /// <summary>
    /// Select rows from database, returning a batch message
    /// </summary>
    [MeepNamespace(ASqlModule.PluginNamespace)]
    public class Select : ASqlModule
    {
        /// <summary>
        /// Specific columns to select
        /// </summary>
        /// <value>Comma separated list, in SQL syntax</value>
        /// <remarks>Defaults to all ('*')</remarks>
        public string Columns { get; set; } = "*";

        /// <summary>
        /// Name of the table in {Smart.Format}
        /// </summary>
        /// <value>From.</value>
        public string From { get; set; }

        /// <summary>
        /// Where clause in {Smart.Format}
        /// </summary>
        /// <value>The where.</value>
        public string Where { get; set; }

        /// <summary>
        /// Which named query to use, if any are defined
        /// </summary>
        /// <value>The query.</value>
        public string Query { get; set; } = "";

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

            string sfTable = Smart.Format(Table, context);
            string sfQueryName = Smart.Format(Query, context);
            string sfSql = null;

            try
            {
                var namedQuery = Queries?.Where(x => x.Name.Equals(sfQueryName)).FirstOrDefault();
                if (namedQuery != null)
                    sfSql = Smart.Format(namedQuery.Content, context);
                else
                    sfSql = Smart.Format("SELECT {0} FROM {1} WHERE {2}",
                                         Smart.Format(Columns, context),
                                         sfTable,
                                         Smart.Format(Where, context)
                                        );

                using (DbConnection connection = NewConnection(context))
                {
                    connection.Open();

                    // ToDo: Work out a sensible way to pass named parameters 
                    // into the query.

                    DbCommand cmd = connection.CreateCommand();
                    cmd.CommandText = sfSql;
                    cmd.CommandType = CommandType.Text;
                    var result = await cmd.ExecuteReaderAsync();

                    if (result.HasRows)
                        return new Batch
                        {
                            DerivedFrom = msg,
                            Name = this.Name,
                            Messages = (from IDataRecord r in result
                                        select new DataRecordMessage
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
                logger.Error(ex, $"{ex.GetType().Name} thrown for \"{sfSql}\": {ex.Message}");
                return null;
            }
        }

    }
}
