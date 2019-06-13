using System;

using Xunit;

using MeepSQL;
using MeepLib;
using MeepLib.Messages;

namespace MeepLibTests
{
    public class MeepSQLTests
    {
        [Fact]
        public async void CreateTableSQL()
        {
            Message msg = new Message
            {
            };

            string create1 = await msg.ToTableDef("Messages").ToCreateTable(new MessageContext(msg,null));

            Assert.Equal(CreateTableSQL1.ToUnixEndings(), create1.ToUnixEndings());
        }

        public static string CreateTableSQL1 = @"CREATE TABLE IF NOT EXISTS Messages (
ID varchar(36) NOT NULL PRIMARY KEY,
DerivedFromID varchar(36) NOT NULL,
CreatedTicks bigint NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS Messages_ID_idx ON Messages (ID);
CREATE INDEX IF NOT EXISTS Messages_DerivedFromID_idx ON Messages (DerivedFromID);
CREATE INDEX IF NOT EXISTS Messages_CreatedTicks_idx ON Messages (CreatedTicks);
";
    }
}
