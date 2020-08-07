using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Documents;
using PropertyTools.DataAnnotations;


namespace MonchaCadViewer.CanvasObj
{
    public class CadObject : Shape
    {
        private bool _isfix = false;
        private bool _mirror = false;

        //Event
        public event EventHandler<CadObject> Selected;
        public event EventHandler<bool> Fixed;
        public event EventHandler<CadObject> Updated;
        public event EventHandler<CadObject> Removed;

        //Geometry
        public Geometry GmtrObj { get; set; }

        public Rect Bounds => GmtrObj.Bounds;

        public bool Mirror { 
            get => this._mirror;
            set
            {
                this._mirror = value;
                this.Scale.CenterX = this.GmtrObj.Bounds.X - this.Translate.X + this.GmtrObj.Bounds.Width / 2;
                this.Scale.CenterY = this.GmtrObj.Bounds.Y - this.Translate.Y + this.GmtrObj.Bounds.Height / 2;
                if (this._mirror && this.Scale.ScaleX > 0)
                {
                    this.Scale.ScaleX = -this.Scale.ScaleX;
                }
                else if (this._mirror == false && this.Scale.ScaleX < 0)
                {
                    this.Scale.ScaleX = -this.Scale.ScaleX;
                }


            }
        }
        public double Angle { get; set; } = 0;

        public bool IsSelected { get; set; } = false;

        protected Point MousePos = new Point();
        protected Point BasePos = new Point();

        public TransformGroup Transform = new TransformGroup();
        public TranslateTransform Translate = new TranslateTransform();
        public RotateTransform Rotate = new RotateTransform();
        public ScaleTransform Scale = new ScaleTransform();

        protected override Geometry DefiningGeometry => GmtrObj;

        public bool Render { get; set; } = true;
        
        public bool IsFix { get => _isfix; set => Fixing(value); }

        private void Fixing(bool stat)
        {
            this._isfix = stat;
            if (Fixed != null)
                Fixed(this, stat);
        }

        public bool WasMove { get; set; } = false;

        public bool Editing { get; set; } = false;

        public bool OnBaseMesh { get; set; } = false;

        public bool MouseForce { get; set; } = false;

        public AdornerLayer adornerLayer { get; set; }

        public Adorner ObjAdorner { get; set; }


        public CadObject(bool mouseevent, bool move, Geometry Path)
        {
            this.GmtrObj = Path;

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

            if (!(this.GmtrObj.Transform is TransformGroup))
            {
                TransformGroup tempTransform = new TransformGroup();
                TranslateTransform Translate = new TranslateTransform();
                RotateTransform Rotate = new RotateTransform();
                ScaleTransform Scale = new ScaleTransform();
                tempTransform.Children.Add(Scale);
                tempTransform.Children.Add(Rotate);
                tempTransform.Children.Add(Translate);
                this.GmtrObj.Transform = tempTransform;
               
            }

            this.Transform = this.GmtrObj.Transform as TransformGroup;
            this.Translate = (TranslateTransform)this.Transform.Children[2];
            this.Rotate = (RotateTransform)this.Transform.Children[1];
            this.Scale = (ScaleTransform)this.Transform.Children[0];
        }


        private void ContextMenu_Closing(object sender, RoutedEventArgs e)
        {

        }

        private void CadObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Canvas canvas = this.Parent as Canvas;
            this.MousePos = e.GetPosition(canvas);
            this.BasePos = new Point(this.Translate.X, this.Translate.Y);
        }

        private void CadObject_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.WasMove = false;
            if (Updated != null)
                Updated(this, this);
            this.StatColorSelect();
        }

        public void StatColorSelect()
        {
            if (this.Fill == null) this.Fill = Brushes.Gray;

            if (this.IsMouseOver)
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.Orange;
                if (this.Stroke != null) this.Stroke = Brushes.Orange;
            }
            else if (this.IsSelected)
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.Red;
                if (this.Stroke != null) this.Stroke = Brushes.Red;
            }
            else if (!this.Render)
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.LightGray;
                if (this.Stroke != null) this.Stroke = Brushes.LightGray;
            }
            else if (this.IsFix)
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.LightBlue;
                if (this.Stroke != null) this.Stroke = Brushes.LightBlue;
            }
            else
            {
                if (this.Fill != Brushes.Transparent && this.Fill != null) this.Fill = Brushes.Gray;
                if (this.Stroke != null) this.Stroke = Brushes.Blue;
            }

            if (Updated != null)
                Updated(this, this);
        }

        private void CadObject_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
           
                if (this.WasMove == false)
                {
                    this.IsSelected = !this.IsSelected;

                   /* if (this.IsSelected && !Keyboard.IsKeyDown(Key.LeftShift))
                        if (this.Parent is CadCanvas canvas)
                            canvas.UnselectAll(this);*/

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

                }

            if (Selected != null)
            {
                if (this.IsSelected)
                    Selected(this, this);
                else
                    Selected(this, null);
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
            StatColorSelect();

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
            if (Updated != null)
                Updated(this, this);
        }
    }
}
