using MonchaSDK;
using MonchaSDK.Device;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MonchaSDK.Object;
using System.ComponentModel;
using MonchaCadViewer.Interface;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using MonchaCadViewer.Panels;
using System.Windows.Data;
using System.Collections.ObjectModel;

namespace MonchaCadViewer.CanvasObj
{
    public class CadCanvas : Canvas, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        public event EventHandler<CadObject> SelectedObject;
        public event EventHandler UpdateProjection;

        public CadAnchor UnderAnchor;



        public bool MainCanvas { get; }

        public Point LastMouseDownPosition = new Point();

        public CadCanvas()
        {
            this.MainCanvas = true;
            LoadSetting();
        }

        public CadCanvas(LSize3D Size, bool MainCanvas)
        {
            this.MainCanvas = MainCanvas;
            LoadSetting();
            this.Loaded += CadCanvas_Loaded;
        }

        private void CadCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= CadCanvas_Loaded;
        }

        private void LoadSetting()
        {
            this.Background = Brushes.Transparent; //backBrush;
            this.DataContext = LaserHub.Size;

            this.SetBinding(Canvas.WidthProperty, "X");
            this.SetBinding(Canvas.HeightProperty, "Y");

            ResetTransform();
        }


        //Рисуем квадраты в поле согласно схеме


        public static List<CadAnchor> GetMesh(LDeviceMesh mesh, double AnchorSize, bool Render, MeshType meshType)
        {
            List<CadAnchor> objects = new List<CadAnchor>();

            for (int i = 0; i < mesh.GetLength(0); i++)
            {
                for (int j = 0; j < mesh.GetLength(1); j++)
                {
                    mesh[i, j].M = LaserHub.Size;
                    objects.Add(new CadAnchor(mesh[i, j])
                    {
                        IsFix = false,// !mesh.OnlyEdge;
                        Uid = i.ToString() + ":" + j.ToString(),
                        ToolTip = "Позиция: " + i + ":" + j + "\nX: " + mesh[i, j].X + "\n" + "Y: " + mesh[i, j].Y,
                        DataContext = mesh,
                        Render = Render,
                        MeshType = meshType
                    });
                }
            }
            return objects;

            return null;
        }

        #region TransfromObject
        public TransformGroup TransformGroup { get; set; }
        public ScaleTransform Scale { get; set; }
        public RotateTransform Rotate { get; set; }
        public TranslateTransform Translate { get; set; }

        public double X
        {
            get => this.Translate.X;
            set => this.Translate.X = value;
        }
        public double Y
        {
            get => this.Translate.Y;
            set => this.Translate.Y = value;
        }
        public bool IsFix { get; set; }
        public bool Mirror { get; set; } = false;

        public bool WasMove { get; set; } = false;

        public void UpdateTransform(TransformGroup transformGroup, bool ResetPosition)
        {
            if (transformGroup != null)
            {
                this.RenderTransform = TransformGroup;
                this.Scale = this.TransformGroup.Children[0] != null ? (ScaleTransform)this.TransformGroup.Children[0] : new ScaleTransform();
                this.Rotate = this.TransformGroup.Children[1] != null ? (RotateTransform)this.TransformGroup.Children[1] : new RotateTransform();
                this.Translate = this.TransformGroup.Children[2] != null ? (TranslateTransform)this.TransformGroup.Children[2] : new TranslateTransform();
            }
            else ResetTransform();


            if (this.Scale.ScaleX < 0) this.Mirror = true;
        }

        public void ResetTransform()
        {
            this.TransformGroup = new TransformGroup()
            {
                Children = new TransformCollection()
                    {
                        new ScaleTransform(),
                        new RotateTransform(),
                        new TranslateTransform()
                    }
            };
            this.Scale = (ScaleTransform)this.TransformGroup.Children[0];
            this.Rotate = (RotateTransform)this.TransformGroup.Children[1];
            this.Translate = (TranslateTransform)this.TransformGroup.Children[2];
            this.RenderTransform = this.TransformGroup;
        }
        #endregion
    }
}
