using CadProjectorSDK.CadObjects;
using CadProjectorSDK.Scenes;
using CadProjectorViewer.ViewModel;
using KompasLib.KompasTool;
using KompasLib.Tools;
using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using ToGeometryConverter.Object;
using ToGeometryConverter.Object.Elements;

namespace CadProjectorViewer.Panels.RightPanel
{
    /// <summary>
    /// Логика взаимодействия для KompasPage.xaml
    /// </summary>
    public partial class KompasPage : Page
    {
        private KmpsAppl kmpsAppl;
        private AppMainModel MainModel => (AppMainModel)this.DataContext;

        public KompasPage()
        {
            InitializeComponent();
        }

        private void KmpsAppl_OpenedDoc(object newDoc, int docType)
        {
            KmpsNameLbl.Invoke(() => {
                if (newDoc is KmpsDoc kmpsDoc)
                {
                    if (kmpsDoc.D7.Name != null)
                    {
                        KmpsNameLbl.Content = kmpsDoc.D7.Name;
                    }
                    else
                    {
                        KmpsNameLbl.Content = "Пустой";
                    }
                }
            });
        }

        private void KmpsAppl_ChangeDoc(object sender, KmpsDoc e)
        {
            KmpsNameLbl.Invoke(new Action(() => {
                if (e.D7 != null)
                    KmpsNameLbl.Content = e.D7.Name;
            }));
        }

        private void KmpsAppl_ConnectBoolEvent(object sender, bool e)
        {
            kmpsConnectToggle.IsOn = e;
        }

        private async void kmpsSelectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI != null)
            {
                KmpsDoc doc = new KmpsDoc(KmpsAppl.Appl.ActiveDocument);

                CadGroup cadGeometries =
                    new CadGroup(
                        await ContourCalc.GetGeometry(doc, MainModel.ProjectorHub.ScenesCollection.SelectedScene.ProjectionSetting.PointStep.Value, false, true),
                        doc.D7.Name);

                SceneTask sceneTask = new SceneTask()
                {
                    Object = cadGeometries,
                    TableID = MainModel.ProjectorHub.ScenesCollection.SelectedScene.TableID,
                };
                MainModel.ProjectorHub.ScenesCollection.AddTask(sceneTask);
            }
        }

        private async void kmpsAddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI != null)
            {
                KmpsDoc doc = new KmpsDoc(KmpsAppl.Appl.ActiveDocument);

                GCCollection gCObjects = new GCCollection(doc.D7.Name);

                gCObjects.Add(new GeometryElement(await ContourCalc.GetGeometry(doc, MainModel.ProjectorHub.ScenesCollection.SelectedScene.ProjectionSetting.PointStep.Value, true, true), "Kompas"));

                CadGroup cadGeometries =
                      new CadGroup(
                          await ContourCalc.GetGeometry(doc, MainModel.ProjectorHub.ScenesCollection.SelectedScene.ProjectionSetting.PointStep.Value, true, true),
                          doc.D7.Name);

                SceneTask sceneTask = new SceneTask()
                {
                    Object = cadGeometries,
                    TableID = MainModel.ProjectorHub.ScenesCollection.SelectedScene.TableID,
                };
                MainModel.ProjectorHub.ScenesCollection.AddTask(sceneTask);
            }
        }

        private void kmpsConnectToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI == null)
            {
                kmpsAppl = new KmpsAppl();

                if (kmpsAppl.Connect())
                {
                    kmpsAppl.ConnectBoolEvent += KmpsAppl_ConnectBoolEvent;

                    kmpsAppl.AppEvent.DocumentOpened += KmpsAppl_OpenedDoc;

                    kmpsConnectToggle.IsOn = KmpsAppl.KompasAPI != null;
                }

            }
        }
    }
}
