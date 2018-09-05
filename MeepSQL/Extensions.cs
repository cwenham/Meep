using System;
using System.Linq;
using System.Data.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Data.SqlClient;

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
        /// Return SQL DDL ("CREATE TABLE") for a Message class
        /// </summary>
        /// <returns>The "CREATE TABLE" statement.</returns>
        /// <param name="msg">Message.</param>
        /// <remarks>Observes System.ComponentModel.DataAnnotations. If no
        /// [Key] is specified, ID is used.</remarks>
        public static string ToCreateTable(this Message msg, string tableName)
        {
            var cols = from p in msg.GetType().GetProperties()

                           // Ignore NotMapped properties
                       let nm = p.GetCustomAttributes(typeof(NotMappedAttribute), true)
                       where !nm.Any()

                       let length = p.GetCustomAttributes(typeof(MaxLengthAttribute), true).Cast<MaxLengthAttribute>()
                       let key = p.GetCustomAttributes(typeof(KeyAttribute), true).Cast<KeyAttribute>()
                       let index = p.GetCustomAttributes(typeof(IndexAttribute), true).Cast<IndexAttribute>()
                       let sType = Smart.Format(p.PropertyType.ToSQLType(), length.FirstOrDefault()?.Length.ToString() ?? int.MaxValue.ToString())

                       select new
                       {
                           Name = p.Name,
                           Definition = String.Format("{0} {1}", p.Name, sType),
                           Key = key,
                           Index = index
                       };

            List<string> definitions = new List<string>();
            List<string> indexes = new List<string>();
            foreach (var c in cols)
            {
                string def = c.Definition;
                if (c.Key.Any())
                    def = String.Format("{0} PRIMARY KEY", def);

                definitions.Add(def);

                if (c.Index.Any() && !c.Key.Any())
                    indexes.Add(String.Format(_createIndexTemplate, tableName, c.Name));
            }

            StringBuilder transaction = new StringBuilder();
            transaction.AppendLine(String.Format(_createTableTemplate, tableName, String.Join(",\n", definitions)));
            transaction.AppendLine(String.Join('\n', indexes));

            return transaction.ToString();
        }

        /// <summary>
        /// Return INSERT OR REPLACE command with parameters set, ready to execute
        /// </summary>
        /// <returns>The insert or replace.</returns>
        /// <param name="msg">Message.</param>
        /// <param name="connection">Connection.</param>
        /// <param name="tableName">Table name.</param>
        public static DbCommand ToInsertOrReplace(this Message msg, DbConnection connection, string tableName)
        {
            var cols = (from p in msg.GetType().GetProperties()

                        // Ignore NotMapped properties
                        let nm = p.GetCustomAttributes(typeof(NotMappedAttribute), true)
                        where !nm.Any()

                        select new
                        {
                            p.Name,
                            Value = p.GetValue(msg)
                        }).ToList();

            DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = String.Format(_insertOrReplaceTemplate, tableName,
                                            String.Join(',', cols.Select(x => x.Name)),
                                            String.Join(',', cols.Select(x => $"@{x.Name}")));

            foreach (var c in cols)
            {
                DbParameter param = cmd.CreateParameter();
                param.ParameterName = $"@{c.Name}";
                param.Value = c.Value ?? DBNull.Value;
                cmd.Parameters.Add(param);
            }

            return cmd;
        }

        private static string _createTableTemplate = "CREATE TABLE IF NOT EXISTS {0} (\n{1}\n);";
        private static string _createIndexTemplate = "CREATE INDEX IF NOT EXISTS {0}_{1}_idx ON {0} ({1});";
        private static string _insertOrReplaceTemplate = "INSERT OR REPLACE INTO {0}({1}) VALUES ({2});";

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
            { typeof(decimal), "decimal" }
        };
    }
}
