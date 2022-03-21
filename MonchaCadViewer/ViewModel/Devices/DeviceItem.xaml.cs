﻿using CadProjectorSDK.Interfaces;
using Microsoft.Xaml.Behaviors.Core;
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
using System.Windows.Shapes;

namespace CadProjectorViewer.ViewModel.Devices
{
    /// <summary>
    /// Логика взаимодействия для DeviceItem.xaml
    /// </summary>
    public partial class DeviceItem : UserControl
    {
        public DeviceItem()
        {
            InitializeComponent();
        }

        public ICommand ReconnectCommand => new ActionCommand(async () => {
            if (this.DataContext is IConnected connected)
            {
                await connected.Reconnect();
            }
        });
    }
}
