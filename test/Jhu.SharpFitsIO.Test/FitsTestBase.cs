using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jhu.SharpFitsIO
{
    public abstract class FitsTestBase
    {
        protected string GetTestName()
        {
            var stackTrace = new System.Diagnostics.StackTrace();

            foreach (var stackFrame in stackTrace.GetFrames())
            {
                var methodBase = stackFrame.GetMethod();
                var attributes = methodBase.GetCustomAttributes(typeof(TestMethodAttribute), false);

                if (attributes.Length >= 1)
                {
                    return methodBase.Name;
                }
            }

            return "Not called from a test method";
        }

        protected FitsFile CreateFitsFile()
        {
            var fits = new FitsFile(this.GetType().Name + "_" + GetTestName() + ".fits", FitsFileMode.Write);

            return fits;
        }
    }
}
