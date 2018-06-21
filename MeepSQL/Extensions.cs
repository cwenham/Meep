using System;
using System.Linq;
using System.Data.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.Data.Sqlite;
using SmartFormat;

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
        public static DbConnection ToConnection(this string connectionString)
        {
            // For now we're just going to assume SQLite, and update this later
            // when we have some other database instances to test with.
            // There doesn't appear to be a simple and unfussy way of doing this,
            // and I might end up using something like the solutions proposed here:
            // https://stackoverflow.com/questions/185474/c-sharp-retrieving-correct-dbconnection-object-by-connection-string

            // ToDo: Expand this to cover more database providers

            return new SqliteConnection(connectionString);
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
                       let length = p.GetCustomAttributes(typeof(MaxLengthAttribute), true).Cast<MaxLengthAttribute>()
                       let sType = Smart.Format(p.PropertyType.ToSQLType(), length.FirstOrDefault()?.Length.ToString() ?? "max")
                       select String.Format("{0} {1}", p.Name, sType);

            return String.Format(_createTableTemplate, tableName, String.Join(",", cols));
        }
        private static string _createTableTemplate = "CREATE TABLE {0} ({1})";

        public static string ToSQLType(this Type type)
        {
            if (SqlTypeMap.ContainsKey(type))
                return SqlTypeMap[type];
            else
                return "varchar(max)";
        }
        private static Dictionary<Type, string> SqlTypeMap = new Dictionary<Type, string>
        {
            { typeof(string), "varchar({0})" },
            { typeof(DateTime), "datetime" },
            { typeof(int), "int" },
            { typeof(short), "smallint" },
            { typeof(long), "bigint" },
            { typeof(decimal), "decimal" }
        };
    }
}
