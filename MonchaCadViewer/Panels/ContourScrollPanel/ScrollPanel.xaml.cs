using CadProjectorSDK;
using CadProjectorSDK.CadObjects;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorViewer.CanvasObj;
using CadProjectorViewer.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using ToGeometryConverter.Object;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для ScrollPanel.xaml
    /// </summary>
    public partial class ScrollPanel : UserControl
    {
        public ScrollPanel()
        {
            InitializeComponent();
        }


        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is AppMainModel appMainModel)
            {
                appMainModel.ProjectorHub.ScenesCollection.LoadedObjects.Clear();
            }

        }
    }
}
