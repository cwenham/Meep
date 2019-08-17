using System;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;

using SmartFormat;

using MeepLib.Messages;

namespace MeepLib.Modifiers
{
    /// <summary>
    /// Split CSV data or other delimited tabular data
    /// </summary>
    /// <remarks>For XPath, JSON Path, Regex or {Smart.Format} see <see cref="Extract"/>.</remarks>
    public class Split : AMessageModule
    {
        /// <summary>
        /// Delimiter to split the columns on
        /// </summary>
        /// <value></value>
        /// <remarks>Defaults to a comma for CSV input.</remarks>
        public DataSelector On { get; set; } = ",";

        /// <summary>
        /// Line delimiter
        /// </summary>
        /// <value></value>
        /// <remarks>Line separator, defaults to a newline.</remarks>
        public DataSelector With { get; set; } = "\n";

        /// <summary>
        /// Optional column header names
        /// </summary>
        /// <value>The columns.</value>
        /// <remarks>Supported syntax:
        /// 
        /// <list type="number">
        /// <item># - Numbered columns (default)</item>
        /// <item>* - First line names the columns</item>
        /// <item>comma,separated,list</item>
        /// </list>
        /// 
        /// </remarks>
        public DataSelector Columns { get; set; } = "#";

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            string dsOn = await On.SelectStringAsync(context);
            string dsWith = await With.SelectStringAsync(context);
            string dsColumns = await Columns.SelectStringAsync(context);

            return await Task.Run<Message>(() =>
            {
                try
                {
                    //ToDo: Support a streaming version for very large CSVs

                    // First the basic splitting into lines and columns
                    string[] rawLines = msg.ToString().Trim().Split(new string[] { dsWith },
                                        StringSplitOptions.RemoveEmptyEntries);

                    var lines = from r in rawLines
                                select r.Split(new string[] { dsOn }, StringSplitOptions.None);

                    string[] first = lines?.FirstOrDefault();
                    if (first is null)
                        return null;

                    // Second, sort out the column names
                    string[] columns = null;

                    if (dsColumns.Equals("#"))
                        columns = Enumerable.Range(1, first.Length).Select(x => Convert.ToString(x)).ToArray();
                    else
                    if (dsColumns.Equals("*"))
                    {
                        columns = first;
                        lines = lines.Skip(1);
                    }
                    else
                    {
                        columns = dsColumns.Split(',');
                    }

                    // Third, determine the column types and get converters for them
                    var sample = lines.FirstOrDefault();
                    var converters = (from c in sample
                                      let parsed = c.ToBestType()
                                      let converter = TypeDescriptor.GetConverter(parsed.GetType())
                                      select converter).ToArray();


                    // Finally, return the cleaned and polished results as a batch
                    return new Batch
                    {
                        DerivedFrom = msg,
                        Name = this.Name,
                        Messages = (from line in lines
                                    select new RecordMessage
                                    {
                                        DerivedFrom = msg,
                                        Record = (from i in Enumerable.Range(0, Math.Min(columns.Length, line.Length))
                                                  select new
                                                  {
                                                      k = columns[i],
                                                      v = converters[i].ConvertFromString(line[i])
                                                  }).ToDictionary(x => x.k, y => y.v)
                                    }).ToList()
                    };
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"{ex.GetType().Name} thrown when splitting delimited content: {ex.Message}");
                    return null;
                }
            });
        }
    }
}
