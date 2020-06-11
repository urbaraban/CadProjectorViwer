using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MonchaSDK.Device;
using System.Windows.Documents;
using PropertyTools.DataAnnotations;
using MonchaSDK.Object;
using StclLibrary.Mathematics;
using System.Windows.Media.Media3D;
using MonchaSDK;

namespace MonchaCadViewer.CanvasObj
{
    public class CadObject : Shape
    {
        //Event
        public event EventHandler<CadObject> Selected;
        public event EventHandler<CadObject> Updated;
        public event EventHandler<CadObject> Removed;

        //Geometry
        public Geometry GmtrObj { get; set; }

        public Size Size => GmtrObj.Bounds.Size;

        public bool Mirror { get; set; } = false;
        public double Angle { get; set; } = 0;

        public bool IsSelected { get; set; } = false;

        protected Point MousePos = new Point();
        protected Point BasePos = new Point();

        public TransformGroup Transform = new TransformGroup();
        public TranslateTransform Translate = new TranslateTransform();
        public RotateTransform Rotate = new RotateTransform();
        public ScaleTransform Scale = new ScaleTransform();

        protected override Geometry DefiningGeometry => GmtrObj;

        [Category("Data")]
        [DisplayName("Given name")]
        public bool Render { get; set; } = true;
        
        public bool IsFix { get; set; } = false;

        public bool WasMove { get; set; } = false;

        public bool Editing { get; set; } = false;

        public bool OnBaseMesh { get; set; } = false;

        public bool MouseForce { get; set; } = false;

        public AdornerLayer adornerLayer { get; set; }

        public Adorner ObjAdorner { get; set; }

        public LPoint3D BaseContextPoint { get; set; }

        public CadObject(bool mouseevent, bool move)
        {
            if (mouseevent || move)
            {
                this.MouseLeave += CadObject_MouseLeave;
                this.MouseLeftButtonUp += CadObject_MouseLeftButtonUp;
                this.MouseMove += CadObject_MouseMove;
                this.MouseLeftButtonDown += CadObject_MouseLeftButtonDown;
            }

            if (this.ContextMenu == null) this.ContextMenu = new System.Windows.Controls.ContextMenu();
            this.ContextMenu.ContextMenuClosing += ContextMenu_Closing;

            ContextMenuLib.CadObjMenu(this.ContextMenu);
            this.MouseForce = move;

            this.Transform.Children.Add(Scale);
            this.Transform.Children.Add(Rotate);
            this.Transform.Children.Add(Translate);

            if (Translate.X == 0 && Translate.Y == 0)
            {
                Translate.X = MonchaHub.Size.GetMPoint.X / 2;
                Translate.Y = MonchaHub.Size.GetMPoint.Y / 2;
            }
        }


        private void BaseContextPoint_ChangePoint(object sender, LPoint3D e)
        {
            if (Updated != null)
                Updated(this, this);
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

            MatrixTransform matrixTransform = this.GmtrObj.Transform as MatrixTransform;
            this.BasePos = new Point(this.Translate.X, this.Translate.Y);
        }

        private void CadObject_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.WasMove = false;
            if (Updated != null)
                Updated(this, this);
        }

        public static void StatColorSelect(CadObject obj)
        {
            if (obj.Fill == null) obj.Fill = Brushes.Gray;

            if (obj.IsMouseOver)
            {
                if (obj.Fill != Brushes.Transparent && obj.Fill != null) obj.Fill = Brushes.Orange;
                if (obj.Stroke != null) obj.Stroke = Brushes.Orange;
            }
            else if (obj.IsSelected)
            {
                if (obj.Fill != Brushes.Transparent && obj.Fill != null) obj.Fill = Brushes.Red;
                if (obj.Stroke != null) obj.Stroke = Brushes.Red;
            }
            else if (!obj.Render)
            {
                if (obj.Fill != Brushes.Transparent && obj.Fill != null) obj.Fill = Brushes.LightGray;
                if (obj.Stroke != null) obj.Stroke = Brushes.LightGray;
            }
            else if (obj.BaseContextPoint.IsFix)
            {
                if (obj.Fill != Brushes.Transparent && obj.Fill != null) obj.Fill = Brushes.LightBlue;
                if (obj.Stroke != null) obj.Stroke = Brushes.LightBlue;
            }
            else
            {
                if (obj.Fill != Brushes.Transparent && obj.Fill != null) obj.Fill = Brushes.Gray;
                if (obj.Stroke != null) obj.Stroke = Brushes.Blue;
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

                    if (this.ObjAdorner != null) 
                    {
                        if (this.IsSelected)
                        {
                            this.ObjAdorner.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            this.ObjAdorner.Visibility = Visibility.Hidden;
                        }   
                    }
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

                Translate.X = this.BasePos.X + (tPoint.X - this.MousePos.X);
                Translate.Y = this.BasePos.Y + (tPoint.Y - this.MousePos.Y);



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

            if (Updated != null)
                Updated(this, this);
        }

        public void Update()
        {
            Updated(this, this);
        }
    }
}
