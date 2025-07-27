using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace CadProjectorViewer.Services
{
    internal class DxCeilParser
    {
        public static UidObject Parse(string input)
        {
            PathGeometry pathGeometry = new PathGeometry();
            var points = GetPoints(input);
            if (points.Count == 0)
                return null;

            var group = new CadGroup("DxCeil");
            PathFigure path = new PathFigure()
            {
                IsClosed = true,
                StartPoint = new Point(points[0].X, points[0].Y)
            };
            for (int i = 1; i < points.Count; i += 1)
            {
                Point cp = new Point(points[i].X, points[i].Y);
                path.Segments.Add(new LineSegment(cp, true));   
            }
            pathGeometry.Figures.Add(path);
            ScaleTransform scaleTransform = new ScaleTransform();
            pathGeometry.Transform = scaleTransform;
            group.Add(new CadGeometry(pathGeometry));
            return group;
        }

        private static List<(string Name, double X, double Y)> GetPoints(string input)
        {
            var result = new List<(string Name, double X, double Y)>();
            var entries = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                var parts = entry.Replace("\r\n", string.Empty)
                    .Split(new[] { '(', ';', ')' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 3)
                {
                    string name = parts[0];
                    double x = double.Parse(parts[1].Replace('.', ','));
                    double y = double.Parse(parts[2].Replace('.', ','));
                    result.Add((name, x, y));
                }
                else
                {

                }
            }
            return result;
        }
    }
        // example input:
        // A(423,2;0)     B(423,2;119,6)     C(416,2;122,0)     D(409,1;124,3)     E(401,9;126,4)     F(394,7;128,3)     G(387,5;130,1)     H(380,2;131,7)     I(372,9;133,1)     J(365,6;134,3)     K(358,2;135,4)     L(350,8;136,2)     M(343,4;137,0)     N(336,0;137,5)     O(328,6;137,8)     P(321,1;138,0)     Q(313,7;138,0)     R(306,2;137,9)     S(298,8;137,5)     T(291,4;137,0)     U(284,0;136,3)     V(276,6;135,4)     W(269,2;134,3)     X(261,9;133,1)     Y(254,6;131,7)     Z(247,3;130,1)     A1(240,1;128,4)     B1(232,9;126,5)     C1(225,7;124,4)     D1(218,6;122,1)     E1(211,6;119,7)     F1(204,6;117,3)     G1(197,5;115,0)     H1(190,3;112,9)     I1(183,1;111,0)     J1(175,9;109,2)     K1(168,6;107,6)     L1(161,3;106,2)     M1(154,0;105,0)     N1(146,6;103,9)     O1(139,2;103,1)     P1(131,8;102,3)     Q1(124,4;101,8)     R1(117,0;101,5)     S1(109,5;101,3)     T1(102,1;101,3)     U1(94,6;101,4)     V1(87,2;101,8)     W1(79,8;102,3)     X1(72,4;103,0)     Y1(65,0;103,9)     Z1(57,6;105,0)     A2(50,3;106,2)     B2(43,0;107,6)     C2(35,7;109,2)     D2(28,5;110,9)     E2(21,3;112,8)     F2(14,1;114,9)     G2(7,0;117,2)     H2(0;119,6)     I2(0;0)

}
