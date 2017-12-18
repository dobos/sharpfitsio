using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Jhu.SharpFitsIO
{
    internal static class Error
    {
        public static InvalidOperationException HeaderNotMofiableAfterStart()
        {
            return new InvalidOperationException(ExceptionMessages.HeaderNotMofiableAfterStart);
        }

        public static InvalidOperationException HeaderMustBeReadOrWritten()
        {
            return new InvalidOperationException(ExceptionMessages.HeaderMustBeReadOrWritten);
        }

        public static InvalidOperationException CannotReadFileOpenedForWrite()
        {
            return new InvalidOperationException(ExceptionMessages.CannotReadFileOpenedForWrite);
        }

        public static InvalidOperationException CannotWriteFileOpenedForRead()
        {
            return new InvalidOperationException(ExceptionMessages.CannotWriteFileOpenedForRead);
        }

        public static InvalidOperationException HeaderAlreadyReadOrWritten()
        {
            return new InvalidOperationException(ExceptionMessages.HeaderAlreadyReadOrWritten);
        }

        public static InvalidOperationException AxisLengthMustBeSet()
        {
            return new InvalidOperationException(ExceptionMessages.AxisLengthMustBeSet);
        }

        public static EndOfStreamException UnexpectedEndOfStream()
        {
            return new EndOfStreamException(ExceptionMessages.UnexpectedEndOfStream);
        }

        public static InvalidOperationException CannotMarkEnd()
        {
            return new InvalidOperationException(ExceptionMessages.CannotMarkEnd);
        }

        public static ArgumentNullException ArgumentNull(string paramName)
        {
            return new ArgumentNullException(paramName);
        }
    }
}
