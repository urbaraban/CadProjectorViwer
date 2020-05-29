using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MonchaSDK.Device;
using MonchaSDK.Object;
using System.Windows.Documents;
using MonchaCadViewer.CanvasObj.DimObj;
using PropertyTools.DataAnnotations;

namespace MonchaCadViewer.CanvasObj
{
    public class CadObject : Shape
    {
        public event EventHandler<CadObject> Selected;
        public event EventHandler<CadObject> Updated;
        public event EventHandler<CadObject> Removed;

        protected Point MousePos = new Point();
        protected Point BasePos = new Point();

        public bool IsSelected { get; set; } = false;

        protected override Geometry DefiningGeometry => throw new NotImplementedException();

        [Category("Data")]
        public bool Render { get; set; } = true;
        
        public bool IsFix { get; set; } = false;

        public bool WasMove { get; set; } = false;

        public bool Editing { get; set; } = false;

        public bool OnBaseMesh { get; set; } = false;

        public bool MouseForce { get; set; } = false;

        public AdornerLayer adornerLayer { get; set; }

        public Adorner ObjAdorner { get; set; }

        public MonchaPoint3D BaseContextPoint { get; set; }

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
            this.ContextMenu.ContextMenuClosing += ContextMenu_Closing;

            ContextMenuLib.CadObjMenu(this.ContextMenu);
            StatColorSelect();
            this.MouseForce = move;
        }

        public void intEvent()
        {
            if (this.Updated != null)
                this.Updated(this, this);
        }
        private void ContextMenu_Closing(object sender, RoutedEventArgs e)
        {

        }

        private void CadObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            memberpoint();
        }

        private void memberpoint()
        {
            Canvas canvas = this.Parent as Canvas;
            this.MousePos = Mouse.GetPosition(canvas);
            this.BasePos = this.BaseContextPoint.GetMPoint;
        }

        private void CadObject_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.WasMove = false;
            StatColorSelect();
        }

        private void CadObject_LayoutUpdated(object sender, EventArgs e)
        {
            //StatColorSelect();
        }

        private void StatColorSelect()
        {
            if (this.IsMouseOver)
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.Orange;
                if (this.Stroke != null) this.Stroke = Brushes.Orange;
            }
            else if (!this.BaseContextPoint.IsFix)
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.Black;
                if (this.Stroke != null) this.Stroke = Brushes.Black;
            }
            else if (this.IsSelected)
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.Red;
                if (this.Stroke != null) this.Stroke = Brushes.Red;
            }
            else
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.Blue;
                if (this.Stroke != null) this.Stroke = Brushes.Blue;
            }

            if (Updated != null)
                Updated(this, this);
        }

        private void CadObject_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.Parent is CadCanvas canvas)
                if (!this.WasMove)
                {
                    this.IsSelected = !this.IsSelected;

                    if (this.IsSelected && !Keyboard.IsKeyDown(Key.LeftShift))

                        canvas.UnselectAll(this);

                    if (this.IsSelected && this.ObjAdorner != null)
                        this.ObjAdorner.Visibility = Visibility.Visible;
                    else
                        this.ObjAdorner.Visibility = Visibility.Hidden;
                }
                else
                {
                    this.MouseForce = false;
                    this.WasMove = false;
                    this.Editing = false;
                    this.ReleaseMouseCapture();

                    if (this.Selected != null)
                        this.Selected(this, this);
                }
            StatColorSelect();
        }

        public bool Remove()
        {
            if (this.Parent is CadCanvas canvas)
            {
                canvas.Children.Remove(this);
                if (Removed != null)
                    Removed(this, this);
                return true;
            }

            return false;
        }

        private void CadObject_MouseMove(object sender, MouseEventArgs e)
        {
            CadCanvas canvas = this.Parent as CadCanvas;

            if ((e.LeftButton == MouseButtonState.Pressed && canvas.Status == 0) || MouseForce)
            {
                this.WasMove = true;
                this.Editing = true;

                Point tPoint = e.GetPosition(canvas);

                this.BaseContextPoint.Update(this.BasePos.X + (tPoint.X - this.MousePos.X), 
                    this.BasePos.Y + (tPoint.Y - this.MousePos.Y));

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
