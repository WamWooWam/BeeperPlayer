using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Beeper.Common.Exceptions
{
    /// <summary>
    /// An exception thrown when loading an invalid BeeperFile
    /// </summary>
    public class InvalidFileException : Exception, ISerializable
    {
        public override string Message => BaseException.Message;
        public override Exception GetBaseException()
        {
            return WhyCantIAssignAnInnerExceptionYouCunt;
        }

        public new Exception InnerException => WhyCantIAssignAnInnerExceptionYouCunt;

        public Exception BaseException { get; set; }
        private Exception WhyCantIAssignAnInnerExceptionYouCunt { get; set; }

        public InvalidFileException(string FilePath)
        {
            BaseException = new Exception($"\"{FilePath}\" is not a valid BeeperFile");
            WhyCantIAssignAnInnerExceptionYouCunt = null;
        }

        public InvalidFileException(string FilePath, Exception ex)
        {
            BaseException = new Exception($"\"{FilePath}\" is not a valid BeeperFile");
            WhyCantIAssignAnInnerExceptionYouCunt = ex;
        }
    }
}
