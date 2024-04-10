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

            var point1 = new RenderPoint(rnd.NextDouble() * 100, rnd.NextDouble() * 100, rnd.NextDouble() * 100);
            var point2 = new RenderPoint(rnd.NextDouble() * 100, rnd.NextDouble() * 100, rnd.NextDouble() * 100);
            var line = new VectorLine(point1, point2)
            {
                T1 = 70,
                T2 = 70
            };

            Console.WriteLine(line.ToString());
            Console.WriteLine($"{line.T1} {line.T2}");

            foreach(var item in line.SplitGradient(10))
            {
                //Console.WriteLine($"{item.P1.X.ToString()} {item.P1.Y.ToString()}");
            }

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
