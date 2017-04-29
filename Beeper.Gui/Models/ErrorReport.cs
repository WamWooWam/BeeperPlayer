using Beeper.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beeper.Gui.Models
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
            if (Program.Config.ErrorReporter.IncludeConfig)
                Config = Program.Config;
            if (Program.Config.ErrorReporter.IncludeEnvironmentInfo)
                EnvironmentInfo = new EnvironmentInfo();
            
        }
    }
}
