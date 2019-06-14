using System;
using System.Linq;
using System.Data.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Data.SqlClient;
using System.Threading.Tasks;

using System.Data.SQLite;
using SmartFormat;

using MeepLib;
using MeepLib.Messages;

namespace MeepSQL
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Create a DbConnection based on the connection string
        /// </summary>
        /// <returns>The connection.</returns>
        /// <param name="connectionString">Connection string.</param>
        /// <param name="provider">Optional name of the DB provider</param>
        /// <remarks>If provider isn't given, will try to guess by looking at
        /// clues such as ".sqlite" in the data source.</remarks>
        public static DbConnection ToConnection(this string connectionString, string provider = null)
        {
            // Cover dumb-n-simple cases, like if it's clearly SQLite
            if (connectionString.Contains(".sqlite"))
                return new SQLiteConnection(connectionString);

            // Otherwise, assume SQL Server and its siblings
            return new SqlConnection(connectionString);
        }

        /// <summary>
        /// Get Message's preferred table name
        /// </summary>
        /// <returns>The name.</returns>
        /// <param name="msg">Message.</param>
        /// <remarks>Used when the name isn't specified any other way, such as
        /// in the Table attribute of ASqlModule.</remarks>
        public static string TableName(this Message msg)
        {
            TableAttribute tableAttrib = msg.GetType().GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() as TableAttribute;
            if (!(tableAttrib is null))
                return tableAttrib.Name;

            return msg.GetType().Name;
        }

        /// <summary>
        /// Fetch maximum byte/character length either set by the MaxLength 
        /// attribute or appropriate for the type
        /// </summary>
        /// <returns>The length.</returns>
        /// <param name="type">Type.</param>
        /// <remarks>Intended to be used with SQL scalar types with user 
        /// definable sizes, eg: varchar(n)</remarks>
        public static int TypeLength(this Type type)
        {
            MaxLengthAttribute lengthAttrib = type.GetCustomAttributes(typeof(MaxLengthAttribute), true)
                                                  .FirstOrDefault() as MaxLengthAttribute;
            if (lengthAttrib != null)
                return lengthAttrib.Length;

            if (type == typeof(string))
                return 256;     // Any larger and it's better to use "text"

            if (type == typeof(decimal))
                return 18;      // Default decimal precision of most databases

            return 1;
        }

        /// <summary>
        /// Convert a message to a table definition
        /// </summary>
        /// <returns>The table def.</returns>
        /// <param name="msg">Message.</param>
        /// <param name="tableName">Table name.</param>
        public static Config.Table ToTableDef(this Message msg, string tableName)
        {
            var cols = (from p in msg.GetType().GetProperties()
                            // Ignore NotMapped properties
                        let nm = p.GetCustomAttributes(typeof(NotMappedAttribute), true)
                        where !nm.Any()

                        let length = p.PropertyType.TypeLength()
                        let key = p.GetCustomAttributes(typeof(KeyAttribute), true).Cast<KeyAttribute>()
                        let index = p.GetCustomAttributes(typeof(IndexAttribute), true).Cast<IndexAttribute>()

                        select new Config.Column
                        {
                            Name = p.Name,
                            Type = p.PropertyType.Name,
                            Length = length,
                            Index = PickIndexType(key, index),
                            Nullable = !p.PropertyType.IsValueType,
                            From = $"{{msg.{p.Name}}}"
                        }).ToList();

            var ownKey = (from p in msg.GetType().GetProperties()
                              // Ignore NotMapped properties
                          let nm = p.GetCustomAttributes(typeof(NotMappedAttribute), true)
                          where !nm.Any()

                          let key = p.GetCustomAttributes(typeof(KeyAttribute), false).Cast<KeyAttribute>()
                          where key != null
                          select new { p.Name, key }).ToList();

            // Subclass primary keys override inherited keys
            if (ownKey.Any())
            {
                foreach (var c in cols.Where(x => x.Index == Config.IndexType.PrimaryKey))
                    c.Index = Config.IndexType.Unique;

                cols.First(x => x.Name == ownKey.First().Name)
                    .Index = Config.IndexType.PrimaryKey;
            }

            return new Config.Table
            {
                Name = tableName,
                Columns = cols
            };
        }

        private static Config.IndexType PickIndexType(IEnumerable<KeyAttribute> keys, IEnumerable<IndexAttribute> indecies)
        {
            if (keys.Any())
                return Config.IndexType.PrimaryKey;

            if (indecies is null || indecies.FirstOrDefault() is null)
                return Config.IndexType.None;

            if (indecies.FirstOrDefault().IsUnique)
                return Config.IndexType.Unique;

            return Config.IndexType.NotUnique;
        }

        /// <summary>
        /// Return CREATE TABLE DDL from a table definition
        /// </summary>
        /// <returns>The CREATE TABLE statement.</returns>
        /// <param name="table">Table definition.</param>
        public static async Task<string> ToCreateTable(this Config.Table table, MessageContext context)
        {
            if (!(table.Create is null))
                return await table.Create.Content.SelectStringAsync(context);

            List<string> definitions = new List<string>();
            List<string> indexes = new List<string>();

            foreach (var col in table.Columns)
            {
                definitions.Add(col.ToSQLColumnDefinition());

                switch (col.Index)
                {
                    case Config.IndexType.NotUnique:
                        indexes.Add(Smart.Format(CreateIndexTemplate, table.Name, col.Name));
                        break;
                    case Config.IndexType.PrimaryKey:
                    case Config.IndexType.Unique:
                        indexes.Add(Smart.Format(CreateUniqueTemplate, table.Name, col.Name));
                        break;
                    default:
                        break;
                }

            }

            StringBuilder transaction = new StringBuilder();
            transaction.AppendLine(String.Format(CreateTableTemplate, table.Name, String.Join(",\n", definitions)));
            transaction.AppendLine(String.Join('\n', indexes));

            return transaction.ToString();
        }

        /// <summary>
        /// Return full column definition ready to include in CREATE TABLE DDL
        /// </summary>
        /// <returns>The SQL Column definition.</returns>
        /// <param name="column">Column.</param>
        public static string ToSQLColumnDefinition(this Config.Column column)
        {
            if (!String.IsNullOrWhiteSpace(column.SQL))
                return column.SQL;

            string findType = column.Type.ToLower();

            if (!SqlShortMap.ContainsKey(findType))
                throw new ArgumentException($"Unknown SQL type {column.Type}");

            string type = Smart.Format(SqlShortMap[findType], column.Length.ToString());
            string def = Smart.Format("{0} {1}", column.Name, type);

            if (!column.Nullable)
                def = Smart.Format("{0} NOT NULL", def);

            if (column.Index == Config.IndexType.PrimaryKey)
                def = Smart.Format("{0} PRIMARY KEY", def);

            return def;
        }

        /// <summary>
        /// Return a parameterised INSERT command
        /// </summary>
        /// <returns>DbCommand ready to populate parameters</returns>
        /// <param name="table">Table definition</param>
        /// <param name="template">INSERT template, defaults to a regular 
        /// INSERT, but could be substituted for an "INSERT OR REPLACE" or 
        /// UPSERT.</param>
        public static DbCommand ToInsertCmd(this Config.Table table, DbConnection connection, string template = InsertTemplate)
        {
            DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = Smart.Format(template, table.Name,
                                          String.Join(',', table.Columns.Select(x => x.Name)),
                                          String.Join(',', table.Columns.Select(x => $"@{x.Name}")));

            cmd.Prepare();
            return cmd;
        }

        /// <summary>
        /// Set the named parameters of a DbCommand with the values in a message
        /// </summary>
        /// <returns>The parameters.</returns>
        /// <param name="cmd">The prepared DbCommand from something like ToInsertCmd()</param>
        /// <param name="table">The table definition</param>
        /// <param name="context">The message and its context wrapped in a SmartObjects</param>
        public static DbCommand SetParameters(this DbCommand cmd, Config.Table table, MessageContext context)
        {
            cmd.Parameters.Clear();
            foreach (var c in table.Columns)
            {
                DbParameter param = cmd.CreateParameter();
                param.ParameterName = $"@{c.Name}";
                param.Value = Smart.Format(c.From, context);

                // Convert datetimes to ISO 8601
                if (c.Type.ToLower().Equals("datetime"))
                    param.Value = DateTime.Parse((string)param.Value).ToString("yyyy-MM-dd HH:mm:ss");

                cmd.Parameters.Add(param);
            }

            return cmd;
        }

        /// <summary>
        /// Template for CREATE TABLE
        /// </summary>
        public const string CreateTableTemplate = "CREATE TABLE IF NOT EXISTS {0} (\n{1}\n);";

        /// <summary>
        /// Template for CREATE INDEX
        /// </summary>
        public const string CreateIndexTemplate = "CREATE INDEX IF NOT EXISTS {0}_{1}_idx ON {0} ({1});";

        /// <summary>
        /// Template for CREATE UNIQUE INDEX
        /// </summary>
        public const string CreateUniqueTemplate = "CREATE UNIQUE INDEX IF NOT EXISTS {0}_{1}_idx ON {0} ({1});";

        /// <summary>
        /// Template for INSERT
        /// </summary>
        public const string InsertTemplate = "INSERT INTO {0} ({1}) VALUES ({2});";

        /// <summary>
        /// Template for INSERT OR REPLACE
        /// </summary>
        /// <remarks>Typically interchangable with INSERT for databases that
        /// support it, such as SQLite.
        /// 
        /// <para>For other databases, you might prefer UPSERT, which SQLite
        /// does not support. The two are not the same concept in databases,
        /// however, since UPSERT will usually not overwrite unspecified columns
        /// with NULL the way INSERT OR REPLACE will.</para></remarks>
        public const string InsertOrReplaceTemplate = "INSERT OR REPLACE INTO {0}({1}) VALUES ({2});";

        public static string ToSQLType(this Type type)
        {
            string sqlType = $"varchar({int.MaxValue})";

            if (SqlTypeMap.ContainsKey(type))
                sqlType = SqlTypeMap[type];

            if ((type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                || !type.IsValueType)
                sqlType = $"{sqlType} NULL";
            else
                sqlType = $"{sqlType} NOT NULL";

            return sqlType;
        }

        /// <summary>
        /// Map .Net data types to SQL types
        /// </summary>
        private static Dictionary<Type, string> SqlTypeMap = new Dictionary<Type, string>
        {
            // Store GUIDs as a formatted string. According to tests done by others
            // (see https://stackoverflow.com/questions/11337324/how-to-efficient-insert-and-fetch-uuid-in-core-data/11337522#11337522)
            // the trade-off favours strings for query speed and programmer productivity,
            // while binary wins for storage space. We're going to choose the former.
            { typeof(Guid), "varchar(36)" },

            { typeof(string), "varchar({0})" },
            { typeof(DateTime), "datetime" },
            { typeof(byte), "tinyint" },
            { typeof(short), "smallint" },
            { typeof(int), "int" },
            { typeof(long), "bigint" },
            { typeof(decimal), "decimal" },
            { typeof(Uri), "text" }
        };

        /// <summary>
        /// Map shorthand type names and aliases to SQL types
        /// </summary>
        /// <remarks>Short type names are for users to define columns. Their
        /// purpose is to make it "just do what I want" when they don't RTFM, 
        /// so this lookup table should cover types that people are likely 
        /// to think of from top-of-the-head as well as reasonable picks from 
        /// those who know SQL and other programming languages.</remarks>
        private static Dictionary<string, string> SqlShortMap = new Dictionary<string, string>
        {
            { "id", "varchar(36)" },
            { "guid", "varchar(36)" },
            { "text", "varchar({0})" },
            { "string", "varchar({0})" },
            { "varchar", "varchar({0})" },
            { "varchar({0})", "varchar({0})" },
            { "date", "datetime" },
            { "time", "datetime" },
            { "datetime", "datetime" },
            { "byte", "tinyint" },
            { "tinyint", "tinyint" },
            { "short", "smallint" },
            { "smallint", "smallint" },
            { "int2", "smallint" },
            { "int", "int" },
            { "integer", "int" },
            { "int32", "int" },
            { "int4", "int" },
            { "long", "bigint" },
            { "bigint", "bigint" },
            { "int64", "bigint" },
            { "int8", "bigint" },
            { "decimal", "decimal" },
            { "number", "decimal" },
            { "money", "decimal" },
            { "currency", "decimal" },
            { "float", "real" },
            { "real", "real" },
            { "double", "real" },
            { "blob", "blob" },
            { "bool", "bool" },
            { "boolean", "bool" },
            { "uri", "text" }
        };
    }
}
