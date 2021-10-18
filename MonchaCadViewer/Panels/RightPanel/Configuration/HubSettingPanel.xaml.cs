using CadProjectorSDK;
using CadProjectorSDK.Device.Mesh;
using CadProjectorViewer.StaticTools;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.Panels.RightPanel.Configuration
{
    /// <summary>
    /// Логика взаимодействия для HubSettingPanel.xaml
    /// </summary>
    public partial class HubSettingPanel : UserControl
    {
        private ProjectorHub ProjectorHub => (ProjectorHub)this.DataContext;

        public HubSettingPanel()
        {
            InitializeComponent();
           // FileLoad.LoadMoncha(ProjectorHub, false);

            MashMultiplierUpDn.Value = ProjectorHub.Size.M.Width;

            CalibrationFormCombo.Items.Clear();
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_Dot);
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_Rect);
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_miniRect);
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_Cross);
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_HLine);
            CalibrationFormCombo.Items.Add(CalibrationForm.cl_WLine);
        }


        private void RefreshButton_Click(object sender, RoutedEventArgs e) => FileLoad.LoadMoncha(ProjectorHub, false);



        private void BrowseMonchaBtn_Click(object sender, RoutedEventArgs e) => FileLoad.LoadMoncha(ProjectorHub, true);




        private void MashMultiplierUpDn_ValueIncremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            if (MashMultiplierUpDn.Value == null) MashMultiplierUpDn.Value = 1;
            args.Interval = 0;
            MashMultiplierUpDn.Value = MashMultiplierUpDn.Value.Value * 10;
            ProjectorHub.Size.M.Set(MashMultiplierUpDn.Value.Value);
        }

        private void MashMultiplierUpDn_ValueDecremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            args.Interval = 0;
            MashMultiplierUpDn.Value = MashMultiplierUpDn.Value.Value / 10;
            ProjectorHub.Size.M.Set(MashMultiplierUpDn.Value.Value);
        }

        private void PointStepUpDn_ValueDecremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            if ((Math.Round(PointStepUpDn.Value.Value, 4) - PointStepUpDn.Interval) == 0)
            {
                PointStepUpDn.Interval = PointStepUpDn.Interval / 10;
                args.Interval = args.Interval / 10;
            }
        }

        private void PointStepUpDn_ValueIncremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            if (Math.Round(PointStepUpDn.Value.Value + PointStepUpDn.Interval, 4) >= PointStepUpDn.Interval * 10)
            {
                PointStepUpDn.Interval = PointStepUpDn.Interval * 10;
            }
        }

        private void CalibrationFormCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CalibrationFormCombo.SelectedValue != null)
            {
                ProjectorMesh.ClbrForm = (CalibrationForm)CalibrationFormCombo.SelectedValue;
            }
        }
    }
}
