using MonchaCadViewer.CanvasObj;
using MonchaSDK.Device;
using MonchaSDK.Object;
using System;

using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace MonchaCadViewer.Calibration
{
    public static class MeshTool
    {
        //gives a position on the Y axis between the extreme points in the X plane
        public static double VerticalPosition(CadCanvas canvas, DotShape dotShape)
        {
            if (canvas.DataContext is MonchaDevice device)
            {
                Tuple<int, int> pos = device.BaseMesh.CoordinatesOf(dotShape.BaseContextPoint);

                DotShape[,] ShapeMesh = canvas.GetMeshDot(device.BaseMesh.GetLength(0), device.BaseMesh.GetLength(1));

                Tuple<int, int> pos0 = GetNearFixIndex(ShapeMesh, 0, pos.Item2, pos.Item1, pos.Item2);
                Tuple<int, int> pos1 = GetNearFixIndex(ShapeMesh, device.BaseMesh.GetLength(1) - 1, pos.Item2, pos.Item1, pos.Item2);

                double prop = (ShapeMesh[pos.Item2, pos.Item1].BaseContextPoint.X - ShapeMesh[pos0.Item2, pos0.Item1].BaseContextPoint.X) /
                    (ShapeMesh[pos1.Item2, pos1.Item1].BaseContextPoint.X - ShapeMesh[pos0.Item2, pos0.Item1].BaseContextPoint.X);

                return ShapeMesh[pos0.Item2, pos0.Item1].BaseContextPoint.Y +
                    (ShapeMesh[pos1.Item2, pos1.Item1].BaseContextPoint.Y - ShapeMesh[pos0.Item2, pos0.Item1].BaseContextPoint.Y) 
                    * prop;
            }

            return dotShape.BaseContextPoint.Y;

        }

        private static Tuple<int, int> GetNearFixIndex(DotShape[,] ShapeMesh, int _xend, int _yend, int _xstart, int _ystart)
        {
            int _xstep = _xend < _xstart ? -1 : 1;
            int _ystep = _yend < _ystart ? -1 : 1;

            int y = 0;
            while (_ystart + y != _yend + _ystep && _ystart + y >= 0)
            {
                int x = 0;
                while (_xstart + x != _xend + _xstep && _xstart + x >= 0)
                {
                    if (ShapeMesh[_ystart + y, _xstart + x].BaseContextPoint is MonchaPoint3D point && point.IsFix)
                        return new Tuple<int, int>(_xstart + x, _ystart + y);

                    x += _xstep;
                }
                y += _ystep;
            }
            return new Tuple<int, int>(_xend, _yend);
        }
    }
}
