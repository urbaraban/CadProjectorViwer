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
        public void LineTestMesth_1()
        {
            var rnd = new Random();


            Assert.IsTrue(true);
        }


        [TestMethod]
        public void LineTestMesth_0()
        {
            float KoeffY = -1.0f;

            for (float y = -0.5f; y <= 0.5f; y += 0.1f)
            {
                float y_new = TransformY(y, KoeffY)/TransformY(0.5f, KoeffY) * 0.5f;
                Console.WriteLine($"y = {y}, y' = {y_new}");
            }

            Assert.IsTrue(true);
        }

        static float TransformY(float y, float KoeffY)
        {
            return KoeffY * (float)Math.Pow(y, 3) + y;
        }

    }
}
