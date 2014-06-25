using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jhu.SharpFitsIO;

namespace Jhu.SharpFitsIO
{
    [TestClass]
    public class CardCollectionTest
    {
        [TestMethod]
        public void AddTest()
        {
            var cc = new CardCollection();

            cc.Add(new Card("TEST1"));

            // Test add with existing keyword
        }
    }
}
