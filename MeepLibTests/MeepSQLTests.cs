using System;

using Xunit;

using MeepSQL;
using MeepLib.Messages;

namespace MeepLibTests
{
    public class MeepSQLTests
    {
        [Fact]
        public void CreateTableSQL()
        {
            Message msg = new Message
            {
            };

            string create1 = msg.ToTableDef("Messages").ToCreateTable();

            Assert.Equal(CreateTableSQL1, create1);
        }

        public static string CreateTableSQL1 = @"CREATE TABLE IF NOT EXISTS Messages (
ID varchar(36) NOT NULL PRIMARY KEY,
DerivedFromID varchar(36) NOT NULL,
CreatedTicks bigint NOT NULL
);
CREATE INDEX IF NOT EXISTS Messages_DerivedFromID_idx ON Messages (DerivedFromID);
CREATE INDEX IF NOT EXISTS Messages_CreatedTicks_idx ON Messages (CreatedTicks);
";
    }
}
