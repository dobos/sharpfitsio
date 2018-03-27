﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jhu.SharpFitsIO
{
    [Serializable]
    public class FitsException : Exception
    {
        public FitsException()
            :base()
        {
        }

        public FitsException(string message)
            : base(message)
        {
        }

        public FitsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public FitsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
