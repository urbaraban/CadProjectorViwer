using MonchaSDK.Object;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MonchaCadViewer.CanvasObj
{
    public class DotShape : CadObject
    {
        private RectangleGeometry _rectg;
        private Rect _rect;
        private bool _calibration;

        public double Size { get; set; }

        protected override Geometry DefiningGeometry => UpdatePoint();

        public DotShape(MonchaPoint3D point, double Size, MonchaPoint3D Multiplier, bool Calibration)
        {
            this.Size = Size;
            this.Multiplier = Multiplier;
            this._calibration = Calibration;
            this.Fill = Brushes.Black;
            ContextMenuLib.DotContextMenu(this.ContextMenu);

            this.ContextMenuClosing += DotShape_ContextMenuClosing;

            this.AllowDrop = true;
            this.DragOver += DotShape_DragOver;
            this.DragEnter += DotShape_DragEnter;

            this.BaseContextPoint = point;
            point.ChangePoint += Point_ChangePoint;
        }

        private void DotShape_DragEnter(object sender, DragEventArgs e)
        {
            Console.WriteLine("Drag Enter DSh");
        }

        private void DotShape_DragOver(object sender, DragEventArgs e)
        {
            Console.WriteLine("Drag Over DSh");
        }

        public DotShape(Point point, double Size, MonchaPoint3D Multiplier, bool Calibration)
        {
            this.Size = Size;
            this.Multiplier = Multiplier;
            this._calibration = Calibration;
            this.Fill = Brushes.Black;
            ContextMenuLib.DotContextMenu(this.ContextMenu);

            this.ContextMenuClosing += DotShape_ContextMenuClosing;


            this.BaseContextPoint = new MonchaPoint3D(point.X, point.Y, 0);

            if (this.BaseContextPoint is MonchaPoint3D monchaPoint)
                monchaPoint.ChangePoint += Point_ChangePoint;

        }



        private void Point_ChangePoint(object sender, MonchaPoint3D e)
        {
            UpdatePoint();
        }

        private void DotShape_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            MenuItem cmindex = (MenuItem)this.ContextMenu.DataContext;
            switch (cmindex.Header)
            {
                case "Fix":
                    if (this.BaseContextPoint is MonchaPoint3D point)
                        point.IsFix = !point.IsFix;
                    break;
                case "Remove":
                    if (this.Parent is CadCanvas canvas)
                    {
                        canvas.Children.Remove(this);
                    }
                    break;
            }
        }

        public Geometry UpdatePoint()
        {
            this._rect = new Rect(new Size(Size, Size));
            this._rectg = new RectangleGeometry(_rect);
            if (this.BaseContextPoint is MonchaPoint3D point && !point.IsFix)
            {
                Canvas.SetLeft(this, this.MultPoint.X - Size / 2);
                Canvas.SetTop(this, this.MultPoint.Y - Size / 2); //Y inverted in calibration stat
                Canvas.SetZIndex(this, 999);

                this.UpdateLayout();
            }

            
            return this._rectg;
        }

        public bool CheckInArea(MonchaPoint3D point)
        {
            return (MultPoint.X - Size / 2 < point.X && MultPoint.X + Size / 2 > point.X) && (MultPoint.Y - Size / 2 < point.Y && MultPoint.Y + Size / 2 > point.Y);
        }

        public void SubOnPoint(object obj, int index)
        {
            if (!this._calibration && (this.BaseContextPoint is MonchaPoint3D point && !point.IsFix))
            {
                if (obj is LineSbcr lineSbcr)
                {
                    if (index == 0) lineSbcr.ChangePoint0 += LineSbcr_ChangePoint;
                    else lineSbcr.ChangePoint1 += LineSbcr_ChangePoint;
                }
            }
        }

        private void LineSbcr_ChangePoint(object sender, MonchaPoint3D e)
        {
            if (this.BaseContextPoint is MonchaPoint3D point && !point.IsFix)
            {
                if (this.Uid == "0:0")
                    Console.WriteLine("LineCP" + e);
                this.MultPoint = e;
                UpdatePoint();
            }
        }
    }
}
