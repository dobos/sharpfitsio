using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jhu.SharpFitsIO
{
    [TestClass]
    public class SpillMemoryStreamTest
    {
        [TestMethod]
        public void MemoryOnlyTest()
        {
            var buffer = new byte[0x10000];     // 64k

            using (var sms = new SpillMemoryStream())
            {
                sms.Write(buffer, 0, buffer.Length);

                var ms = new MemoryStream();
                sms.WriteTo(ms);

                Assert.AreEqual(0x10000, ms.Position);
            }
        }

        [TestMethod]
        public void SpillToTempTest()
        {
            var buffer = new byte[0x10000];     // 64k

            using (var sms = new SpillMemoryStream())
            {
                // Write 2M
                for (int i = 0; i < 32; i++)
                {
                    sms.Write(buffer, 0, buffer.Length);
                }

                var ms = new MemoryStream();
                sms.WriteTo(ms);

                Assert.AreEqual(0x200000, ms.Position);
            }
        }
    }
}
