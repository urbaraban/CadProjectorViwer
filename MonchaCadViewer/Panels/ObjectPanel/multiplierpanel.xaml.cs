using MahApps.Metro.Controls;
using MonchaCadViewer.CanvasObj;
using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
using MonchaSDK.Setting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace MonchaCadViewer.Panels
{
    /// <summary>
    /// Логика взаимодействия для multiplierpanel.xaml
    /// </summary>
    public partial class multiplierpanel : UserControl
    {
        public event EventHandler NeedUpdate;

        private CadObject cadObject;

        public multiplierpanel()
        {
            InitializeComponent();
        }


        private void BindingCadObject(LProjectionSetting projectionSetting)
        {           
            DeviceLayerCombo.Items.Clear();
            DeviceLayerCombo.DisplayMemberPath = "HWIdentifier";
            DeviceLayerCombo.Items.Add(null);
        }

        private void ProjectionSetting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.NeedUpdate?.Invoke(this, null);
        }


        private void DeviceLayerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext is CadObject cadObject)
            {
                cadObject.ProjectionSetting.Device = (LDevice)DeviceLayerCombo.SelectedItem;
            }
        }
    }
}
