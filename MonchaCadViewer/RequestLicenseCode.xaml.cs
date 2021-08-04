using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MonchaCadViewer
{
    /// <summary>
    /// Логика взаимодействия для RequestLicenseCode.xaml
    /// </summary>
    public partial class RequestLicenseCode : Window
    {
        public RequestLicenseCode()
        {
            InitializeComponent();
            RequestBox.Text = $"{System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(GetKey()))}";
        }

        private string GetKey()
        {
            Dictionary<string, string> ids =
            new Dictionary<string, string>();

            ManagementObjectSearcher searcher;

            //UUID
            searcher = new ManagementObjectSearcher("root\\CIMV2",
                   "SELECT UUID FROM Win32_ComputerSystemProduct");
            foreach (ManagementObject queryObj in searcher.Get())
            {
                ids.Add($"key_{ids.Count}", queryObj["UUID"].ToString());
            }

            string key = string.Empty;
            foreach (var x in ids)
            {
                key += x.Key + ": " + x.Value + "\r\n";
            }

            return key;
        }

        private void KeyBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.IsLoaded == true)
            {
                byte[] a = System.Text.Encoding.UTF8.GetBytes(RequestBox.Text);
                string b = $"{System.Convert.ToBase64String(a)}";
                if (KeyBox.Text == b)
                {
                    SuccefulLbl.Content = "Yes!";
                }
                else
                {
                    SuccefulLbl.Content = "No =(";
                }
            }
        }
    }
}
