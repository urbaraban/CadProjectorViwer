using CadProjectorSDK.Render;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MonchaSDKTests
{
    [TestClass]
    public class UnitTest1
    {


        [TestMethod]
        public void TestLine()
        {
            var rnd = new Random();
            var line = new VectorLine(rnd.Next(), 0, rnd.Next(), 0, false);
            Console.WriteLine(line);
        }
    }
}
