using CadProjectorSDK.Render;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void LineTestMesth_0()
        {
            var rnd = new Random();
            var line = new VectorLine(rnd.Next() % 1000 - 500, 0, rnd.Next() % 1000 - 500, 0, true);
            Debug.WriteLine(line.ToString());

            var lines = line.SplitGradient(8);

            Debug.WriteLine(lines.P1.ToString());

            foreach(var item in lines)
            {
                Debug.WriteLine(item.P2.ToString());
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void LineTestMesth_1()
        {
            var rnd = new Random();
            var line = new VectorLine(79, 0, 54, 0, true);
            Debug.WriteLine(line.ToString());
            var lines = line.SplitGradient(8);
            Debug.WriteLine(lines.P1.ToString());
            foreach (var item in lines)
            {
                Debug.WriteLine(item.P2.ToString());
            }

            Assert.IsTrue(true);
        }

    }
}
