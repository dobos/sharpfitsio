using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jhu.SharpFitsIO
{
    [TestClass]
    public class BinaryTableHduTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var fits = new FitsFile("test.fits", FitsFileMode.Write);

            // Primary header
            var prim = HduBase.Create(fits, true, true, true);
            prim.WriteHeader();


            // Table
            var tab = BinaryTableHdu.Create(fits, true);

            tab.CreateColumns(new[]
            {
                FitsTableColumn.Create("test", FitsDataTypes.Byte)
            });

            tab.SetAxisLength(2, 1);

            tab.WriteHeader();

            tab.WriteNextRow(new object[] { (byte)9 });
        }
    }
}
