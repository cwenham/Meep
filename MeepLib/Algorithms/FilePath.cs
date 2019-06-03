using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using NCalc;

namespace MeepLib.Algorithms
{
    /// <summary>
    /// NCalc Functions for working with file paths, mainly exposing System.IO.Path functions
    /// </summary>
    public static class FilePath
    {
        public static object GetFileName(FunctionArgs args)
        {
            string input = Convert.ToString(args.Parameters[0].Evaluate());

            if (!(input is null))
                return Path.GetFileName(input);

            return null;
        }
    }
}
