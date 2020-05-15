using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Reflection;
using MonchaSDK.Device;
using MonchaSDK.Object;

namespace MonchaCadViewer.CanvasObj
{
    public class CadObject : Shape
    {
        protected Point MousePos = new Point();
        protected Point BasePos = new Point();

        private MonchaPoint3D _multiplier = new MonchaPoint3D(1, 1, 1);
        public bool IsSelected { get; set; } = false;

        public bool Render { get; set; } = true;
        
        public bool IsFix { get; set; } = false;

        public bool WasMove { get; set; } = false;

        public bool Editing { get; set; } = false;

        public bool OnBaseMesh { get; set; } = false;

        public bool MouseForce { get; set; } = false;

        public MonchaPoint3D Multiplier
        {
            get => _multiplier;
            set => _multiplier = value;
        }

        public MonchaPoint3D BaseContextPoint { get; set; }


        public MonchaPoint3D MultPoint
        {
            get => getmultpoint();
            set => ((MonchaPoint3D)this.MultPoint).Update(value, true);
        }

        private MonchaPoint3D getmultpoint()
        {
            return new MonchaPoint3D(((MonchaPoint3D)this.BaseContextPoint).X * this._multiplier.X, ((MonchaPoint3D)this.BaseContextPoint).Y * this._multiplier.Y, ((MonchaPoint3D)this.BaseContextPoint).Z * this._multiplier.Z);
        }

        protected override Geometry DefiningGeometry => throw new NotImplementedException();

        public CadObject(bool mouseevent, MonchaPoint3D monchaPoint, bool move)
        {
            if (mouseevent || move)
            {
                this.MouseLeave += CadObject_MouseLeave;
                this.MouseLeftButtonUp += CadObject_MouseLeftButtonUp;
                this.LayoutUpdated += CadObject_LayoutUpdated;
                this.MouseMove += CadObject_MouseMove;
                this.MouseLeftButtonDown += CadObject_MouseLeftButtonDown;
            }

            this.BaseContextPoint = monchaPoint;

            if (this.ContextMenu == null) this.ContextMenu = new System.Windows.Controls.ContextMenu();

            ContextMenuLib.CadObjMenu(this.ContextMenu);

            this.MouseForce = move;
        }

        private void CadObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            memberpoint();
        }

        private void memberpoint()
        {
            Canvas canvas = this.Parent as Canvas;
            Point temp = Mouse.GetPosition(canvas);
            this.MousePos = new Point(temp.X / _multiplier.X, temp.Y / _multiplier.Y);
            this.BasePos = this.BaseContextPoint.GetPoint;
        }

        private void CadObject_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.WasMove = false;
        }

        private void CadObject_LayoutUpdated(object sender, EventArgs e)
        {
            if (this.IsMouseOver)
            {
                if (this.Fill != Brushes.Transparent) this.Fill = Brushes.Orange;
                if (this.Stroke != null) this.Stroke = Brushes.Orange;
            }
            else if (this.BaseContextPoint is MonchaPoint3D point && !point.IsFix)
            {
                if (this.Fill != Brushes.Transparent) this.Fill = Brushes.Black;
                if (this.Stroke != null) this.Stroke = Brushes.Black;
            }
            else if (this.IsSelected)
            {
                if (this.Fill != Brushes.Transparent) this.Fill = Brushes.Red;
                if (this.Stroke != null) this.Stroke = Brushes.Red;
            }
            else
            {
                if (this.Fill != Brushes.Transparent) this.Fill = Brushes.Blue;
                if (this.Stroke != null) this.Stroke = Brushes.Blue;
            }
        }

        private void CadObject_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.Parent is CadCanvas canvas)
                if (!this.WasMove)
                {
                    this.IsSelected = !this.IsSelected;

                    if (this.IsSelected && !Keyboard.IsKeyDown(Key.LeftShift))

                        canvas.UnselectAll(this);
                }
                else
                {
                    this.MouseForce = false;
                    this.WasMove = false;
                    this.Editing = false;
                    this.ReleaseMouseCapture();
                }
        }

        private void CadObject_MouseMove(object sender, MouseEventArgs e)
        {
            CadCanvas canvas = this.Parent as CadCanvas;

            if ((e.LeftButton == MouseButtonState.Pressed && canvas.Status == 0) || MouseForce)
            {
                this.WasMove = true;
                this.Editing = true;

                Point newpoint = new Point(e.GetPosition(canvas).X / _multiplier.X, e.GetPosition(canvas).Y / _multiplier.Y);

                this.BaseContextPoint.Update(this.BasePos.X + (newpoint.X - this.MousePos.X), this.BasePos.Y + (newpoint.Y - this.MousePos.Y));

                this.CaptureMouse();
                this.Cursor = Cursors.SizeAll;


                if (this.DataContext is MonchaDeviceMesh mesh)
                {
                    if (mesh.OnlyEdge)
                        mesh.OnEdge();
                    else
                        mesh.MorphMesh(this.BaseContextPoint);
                }

            }
            else
            {
                this.Cursor = Cursors.Hand;
            }
        }


    }
}
