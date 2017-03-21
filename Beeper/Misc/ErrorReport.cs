using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beeper.Common.Models
{
    public class ErrorReportGenerator : ErrorReport
    {
        /// <summary>
        /// Generates an error report
        /// </summary>
        /// <param name="ex">The exception that occured</param>
        public ErrorReportGenerator(Exception ex)
        {
            Exception = ex;
            AppState = Program.AppState;
            List<string> strList = new List<string>();
            foreach (string str in AppState.CommandLineArgs)
            {
                var newstr = str.Replace(Environment.UserName, "<USERNAME>");
                strList.Add(str);
            }
            AppState.CommandLineArgs = strList.ToArray();
            if (Program.Config.ErrorReporter.IncludeConfig)
                Config = Program.Config;
            if (Program.Config.ErrorReporter.IncludeEnvironmentInfo)
                EnvironmentInfo = new EnvironmentInfo();
        }
    }
}
