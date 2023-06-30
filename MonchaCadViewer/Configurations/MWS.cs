using CadProjectorSDK.Device;
using CadProjectorSDK.Device.Controllers;
using CadProjectorSDK.CadObjects;
using CadProjectorSDK.Setting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using CadProjectorSDK.Device.Mesh;
using System.Threading.Tasks;
using CadProjectorSDK.Scenes;
using System.IO;
using CadProjectorSDK.CadObjects.Abstract;
using CadProjectorSDK.CadObjects.Variable;
using CadProjectorViewer.Modeles;
using CadProjectorViewer.Services;
using AppSt = CadProjectorViewer.Properties.Settings;
using CadProjectorViewer.Opening;
using System.Windows;


namespace CadProjectorViewer.Configurations
{
    public static class mws
    {
        #region Tools
        internal static XElement GetOrMakeElement(XContainer parent, string name, string defvalue)
        {
            if (parent.Element(name) == null)
            {
                parent.Add(new XElement(name, defvalue));
            }

            return parent.Element(name);
        }

        internal static string GetOrMakeValue(XElement parent, string name, string defvalue)
        {
            return GetOrMakeElement(parent, name, defvalue).Value;
        }

        internal static string GetOrMakeAttributeValue(XElement element, string name, string defvalue)
        {
            if (element.Attribute(name) == null)
            {
                element.Add(new XAttribute(name, defvalue));
            }
            return element.Attribute(name).Value;
        }

        internal static void SetOrMakeValue(XElement parent, string name, string value)
        {
            GetOrMakeElement(parent, name, value).Value = value;
        }

        #endregion


        #region Write
        public static bool Save(string filepath, AppMainModel appMain)
        {
            XDocument xDocument = new XDocument();

            XElement ROOT = GetOrMakeElement(xDocument, "Moncha", string.Empty);
            XElement Xkeys = GetOrMakeElement(ROOT, "LicenseKeys", string.Empty);

            Xkeys.Elements("Key").Remove();

            // ProjectorHub.SetProgress(0, 99, "Saving...");
            foreach (string key in appMain.LockKey.LicenseKeys)
            {
                Xkeys.Add(new XElement("Key", key));
            }



            XElement OUTDEVICE = GetOrMakeElement(ROOT, "OutputDeviceHandler", string.Empty);

            ///
            /// LaserMeters Write
            ///
            //this.OUTDEVICE.Elements("LaserMeters").Remove();
            //XElement XlaserMeters = new XElement("LaserMeters");
            //foreach (VLTLaserMeters laserMeters in laserHub.LMeters)
            //{
            //    this.OUTDEVICE.Add(WriteXLaserMeter(laserMeters));
            //}
            //this.OUTDEVICE.Add(XlaserMeters);

            ///
            /// Device Write
            ///
            Progress.Instance.SetProgress(0, appMain.Projectors.Count, "MWS: Devices");
            OUTDEVICE.Elements("Devices").Remove();
            XElement XDevices = GetOrMakeElement(OUTDEVICE, "Devices", string.Empty);
            XDevices.Elements("Device");
            foreach (LProjector device in appMain.Projectors)
            {
                XDevices.Add(WriteXDeviceSett(device));
                Progress.Instance.SetProgress(appMain.Projectors.IndexOf(device), appMain.Projectors.Count - 1, $"MWS: Devices {appMain.Projectors.IndexOf(device) + 1}/{appMain.Projectors.Count}");
            }

            ///
            /// Scene Write
            ///
            ROOT.Elements("Scenes").Remove();
            XElement XScenes = GetOrMakeElement(ROOT, "Scenes", string.Empty);
            foreach (ProjectionScene scene in appMain.Scenes)
            {
                XScenes.Add(WriteXScenes(scene));
            }

            ///
            /// Save XML
            ///
            Progress.Instance.SetProgress(0, 99, "MWS: Save file...");
            if (filepath != string.Empty)
            {
                xDocument.Save(filepath);
                Progress.Instance.SetProgress(0, 99, "MWS: Successful");
                LogList.Instance.PostLog("Save end", "MWS");
                return true;
            }
            Progress.Instance.SetProgress(0, 99, "MWS: Fail");
            LogList.Instance.PostLog("Save fail", "MWS");
            return false;
        }

        internal static XElement WriteXScenes(ProjectionScene scene)
        {
            XElement XScene = new XElement("Scene", new XAttribute("Name", scene.NameID));
            XScene.Add(new XAttribute("ID", scene.TableID));
            XScene.Add(new XAttribute("Uid", scene.Uid));

            XScene.Add(WriteXSize(scene.Size, "Size"));
            XScene.Add(new XElement("Attach", scene.DefAttach));
            XScene.Add(new XElement("AttachDistanceX", scene.AttachDistanceX));
            XScene.Add(new XElement("AttachDistanceY", scene.AttachDistanceY));
            XScene.Add(new XElement("CursorMaskActivated", scene.CursorMaskActivated));
            XScene.Add(new XElement("StepByStep", scene.StepByStep));
            XScene.Add(new XElement("DefaultMirror", scene.DefaultMirror));
            XScene.Add(new XElement("DefaultScaleX", scene.DefaultScaleX));
            XScene.Add(new XElement("DefaultScaleY", scene.DefaultScaleY));
            XScene.Add(new XElement("DefaultAngle", scene.DefaultAngle));

            XScene.Add(WriteXProjectionSetting(scene.ProjectionSetting));

            XScene.Elements("Objects").Remove();
            XElement XObjects = new XElement("Objects");
            foreach (UidObject uidObject in scene)
            {
                if (uidObject is CadLine cadLine)
                {
                    XObjects.Add(cadLine.GetXElement());
                }
            }
            XScene.Add(XObjects);

            XScene.Elements("Masks").Remove();
            XElement XMasks = new XElement("Masks");
            foreach(CadRect3D cadRect in scene.Masks)
            {
                XMasks.Add(WriteXSize(cadRect, cadRect.NameID));
            }
            XScene.Add(XMasks);

            XScene.Descendants("Devices").Remove();
            XElement XDevices = new XElement("Devices");
            foreach (LProjector device in scene.Projectors)
            {
                XDevices.Add(new XElement("Uid", device.Uid));
            }
            XScene.Add(XDevices);

            return XScene;
        }

        private static XElement WriteXDeviceSett(LProjector _device)
        {
            XElement XDevice = new XElement("Device", new XAttribute("Name", _device.NameID));
            XDevice.Add(new XAttribute("Type", ((int)_device.DeviceType).ToString(CultureInfo.InvariantCulture)));
            XDevice.Add(new XAttribute("IP", _device.IPAddress.ToString()));
            XDevice.Add(new XAttribute("Uid", _device.Uid.ToString()));

            SetOrMakeValue(XDevice, "TimeColorShift", _device.TimeColorShift.ToString(CultureInfo.InvariantCulture));

            SetOrMakeValue(XDevice, "Alpha", _device.Alpha.ToString(CultureInfo.InvariantCulture));

            SetOrMakeValue(XDevice, "ScanRate", _device.ScanRate.ToString(CultureInfo.InvariantCulture));
            SetOrMakeValue(XDevice, "FPS", _device.FPS.ToString(CultureInfo.InvariantCulture));                                           

            SetOrMakeValue(XDevice, "InvertedX", _device.InvertedX.ToString(CultureInfo.InvariantCulture));                            
            SetOrMakeValue(XDevice, "InvertedY", _device.InvertedY.ToString(CultureInfo.InvariantCulture));                           

            SetOrMakeValue(XDevice, "HeightResolution", _device.HeightResolution.ToString(CultureInfo.InvariantCulture));
            SetOrMakeValue(XDevice, "WidthResolutuon", _device.WidthResolutuon.ToString(CultureInfo.InvariantCulture));

            SetOrMakeValue(XDevice, "OnlySelectMesh", _device.OnlySelectMesh.ToString(CultureInfo.InvariantCulture));

            XDevice.Descendants("Size").Remove();
            if (_device.Size != null)
            {
                XDevice.Add(WriteXSize(_device.Size, "Size"));
            }

            SetOrMakeValue(XDevice, "PolyMeshUsed", _device.PolyMeshUsed.ToString(CultureInfo.InvariantCulture));

            ///
            /// write mesh
            ///

            SetOrMakeValue(XDevice, "UseEllipsoid", _device.UseEllipsoid.ToString(CultureInfo.InvariantCulture));
            XDevice.Descendants("Ellipsoid").Remove();
            XDevice.Add(WriteEllisoidSetting(_device.Ellipsoid));

            if (_device.SelectMesh != null) SetOrMakeValue(XDevice, "SelectMesh", _device.SelectMesh.Uid.ToString());

            XElement XSelectMesh = new XElement("SelectMeshes");
            foreach (ProjectorMesh mesh in _device.SelectedMeshes)
            {
                SetOrMakeValue(XSelectMesh, "Uid", mesh.Uid.ToString());
            }
            XDevice.Add(XSelectMesh);

            XDevice.Descendants("Meshes").Remove();
            XElement Xmeshes = GetOrMakeElement(XDevice, "Meshes", string.Empty);
            foreach (ProjectorMesh lDeviceMesh in _device.Meshes)
            {
                Xmeshes.Add(WriteXMesh(lDeviceMesh));
            }


            ///
            /// write ProjectionSetting
            ///
            SetOrMakeValue(XDevice, "OwnedSetting", _device.OwnedSetting.ToString(CultureInfo.InvariantCulture));

            if (_device.OwnedSetting == true)
            {
                XDevice.Add(WriteXProjectionSetting(_device.ProjectionSetting));
            }

            return XDevice;
        }

        private static XElement WriteEllisoidSetting(CorrectionEllipsoid ellipsoid)
        {
            XElement XEllipsoid = new XElement("Ellipsoid");
            SetOrMakeValue(XEllipsoid, "AngleX", ellipsoid.AngleX.ToString(CultureInfo.InvariantCulture));
            SetOrMakeValue(XEllipsoid, "AngleY", ellipsoid.AngleY.ToString(CultureInfo.InvariantCulture));
            SetOrMakeValue(XEllipsoid, "KoeffX", ellipsoid.KoeffX.ToString(CultureInfo.InvariantCulture));
            SetOrMakeValue(XEllipsoid, "UseAxisCorrection", ellipsoid.UseAxisCorrection.ToString(CultureInfo.InvariantCulture));

            //XElement XAxisCor = new XElement("XAxisCorrect");
            //for (int i = 0; i < ellipsoid.XAxisCorrect.Count; i += 1)
            //{
            //    XAxisCor.Add(
            //        new XElement("Correction", 
            //        new XAttribute("Value", ellipsoid.XAxisCorrect[i].Value.ToString(CultureInfo.InvariantCulture))));
            //}
            //XElement YAxisCor = new XElement("YAxisCorrect");
            //for (int i = 0; i < ellipsoid.YAxisCorrect.Count; i += 1)
            //{
            //    YAxisCor.Add(
            //        new XElement("Correction",
            //        new XAttribute("Value", ellipsoid.YAxisCorrect[i].Value.ToString(CultureInfo.InvariantCulture))));
            //}

            //XEllipsoid.Add(XAxisCor);
            //XEllipsoid.Add(YAxisCor);

            return XEllipsoid;
        }

        private static XElement WriteXProjectionSetting(LProjectionSetting ProjectionSetting)
        {
            XElement XProjectionSetting = new XElement("ProjectionSetting");


            SetOrMakeValue(XProjectionSetting, "FindSolidElement", ProjectionSetting.FindSolidElement.ToString(CultureInfo.InvariantCulture));                                               //Red
            SetOrMakeValue(XProjectionSetting, "PathFindDeep", ProjectionSetting.PathFindDeep.ToString(CultureInfo.InvariantCulture));

            SetOrMakeValue(XProjectionSetting, "Red", ProjectionSetting.Red.ToString(CultureInfo.InvariantCulture));                                               //Red
            SetOrMakeValue(XProjectionSetting, "RedOn", ProjectionSetting.RedOn.ToString(CultureInfo.InvariantCulture));
            SetOrMakeValue(XProjectionSetting, "Green", ProjectionSetting.Green.ToString(CultureInfo.InvariantCulture));                                         //Green
            SetOrMakeValue(XProjectionSetting, "GreenOn", ProjectionSetting.GreenOn.ToString(CultureInfo.InvariantCulture));
            SetOrMakeValue(XProjectionSetting, "Blue", ProjectionSetting.Blue.ToString(CultureInfo.InvariantCulture));                                            //Blue
            SetOrMakeValue(XProjectionSetting, "BlueOn", ProjectionSetting.BlueOn.ToString(CultureInfo.InvariantCulture));

            SetOrMakeValue(XProjectionSetting, "BlankWait", ProjectionSetting.BlankWait.ToString(CultureInfo.InvariantCulture));                 //EndBlanckWait
            SetOrMakeValue(XProjectionSetting, "LineWait", ProjectionSetting.LineWait.ToString(CultureInfo.InvariantCulture));                 //StartLineWait
            SetOrMakeValue(XProjectionSetting, "BlankTail", ProjectionSetting.BlankTail.ToString(CultureInfo.InvariantCulture));
            SetOrMakeValue(XProjectionSetting, "LineTail", ProjectionSetting.LineTail.ToString(CultureInfo.InvariantCulture));

            SetOrMakeValue(XProjectionSetting, "ParallelLineCount", ProjectionSetting.ParallelLineCount.ToString(CultureInfo.InvariantCulture));
            SetOrMakeValue(XProjectionSetting, "ParallelLineMargin", ProjectionSetting.ParallelLineMargin.ToString(CultureInfo.InvariantCulture));

            SetOrMakeValue(XProjectionSetting, "CRS", ProjectionSetting.PointStep.Value.ToString());

            return XProjectionSetting;
        }

        private static XElement WriteXLaserMeter(VLTLaserMeters _lmeter)
        {
            XElement _device = new XElement(_lmeter.NameID);

            SetOrMakeValue(_device, "IP", _lmeter.IP.ToString());                          //IP
            SetOrMakeValue(_device, "Interval", _lmeter.Interval.ToString(CultureInfo.InvariantCulture));              //Interval
            SetOrMakeValue(_device, "AutoPlay", _lmeter.AutoPlay.ToString(CultureInfo.InvariantCulture));              //AutoPlay

            return _device;
        }

        private static XElement WriteXMesh(ProjectorMesh mesh)
        {
            XElement XMesh = new XElement(mesh.Name, new XAttribute("Uid", mesh.Uid.ToString()));

            SetOrMakeValue(XMesh, "Morph", mesh.Morph.ToString(CultureInfo.InvariantCulture));
            SetOrMakeValue(XMesh, "Width", mesh.GetLength(1).ToString(CultureInfo.InvariantCulture));
            SetOrMakeValue(XMesh, "Height", mesh.GetLength(0).ToString(CultureInfo.InvariantCulture));

            XMesh.Descendants("Size").Remove();
            XMesh.Add(WriteXSize(mesh.Size, "Size"));

            //ScreenMapping
            XMesh.Descendants("Point").Remove();
            for (int i = 0; i < mesh.GetLength(0); i += 1)
            {
                for (int j = 0; j < mesh.GetLength(1); j += 1)
                {
                    XMesh.Add(mesh[i, j].GetXElement());
                }
            }

            return XMesh;
        }

        private static XElement WriteXSize(CadRect3D lSize3D, string Name)
        {
            XElement XSize = new XElement(Name);
            SetOrMakeValue(XSize, "Point1", lSize3D.BL.ToString());
            SetOrMakeValue(XSize, "Point2", lSize3D.TR.ToString());
            return XSize;
        }
        #endregion

        #region Read
        internal static AppMainModel Open(string filepath)
        {
            //send path to hub class
            try
            {
                //check path to setting file
                if (File.Exists(filepath) == false)
                {
                    filepath = FileLoad.BrowseMWS(); //select if not\
                    AppSt.Default.cl_moncha_path = filepath;
                    AppSt.Default.Save();
                }
                XDocument xDocument = XDocument.Load(filepath);
                return GetAppMainModel(xDocument);
            }
            catch
            {
                MessageBox.Show("Ошибка конфигурации!");
            }
            return new AppMainModel();
        }

        private static AppMainModel GetAppMainModel(XDocument xDocument)
        {
            AppMainModel appMainModel = new AppMainModel();

            XElement ROOT = GetOrMakeElement(xDocument, "Moncha", string.Empty);
            XElement XKeys = GetOrMakeElement(ROOT, "LicenseKeys", string.Empty);
            XElement OUTDEVICE = GetOrMakeElement(ROOT, "OutputDeviceHandler", string.Empty);
            

            List<string> Keys = new List<string>();

            foreach (XElement xElement in XKeys.Descendants("Key"))
            {
                Keys.Add(xElement.Value);
            }

            List<XElement> devices = new List<XElement> (GetOrMakeElement(OUTDEVICE, "Devices", string.Empty).Elements());
            appMainModel.Projectors.Clear();
            foreach (XElement dvs in devices)
            {
                int index = devices.IndexOf(dvs);
                Progress.Instance.SetProgress(index, devices.Count, $"Load device {index + 1}/{devices.Count}");
                try
                {
                    Guid Uid = Guid.Parse(dvs.Attribute("Uid").Value);
                    DeviceType deviceType = (DeviceType)int.Parse(dvs.Attribute("Type").Value);
                    LProjector lProjector = GetDevice(dvs, appMainModel.Projectors.Count, deviceType);
                    lProjector.GetLicenseStatus = appMainModel.LockKey.GetLicense;
                    appMainModel.Projectors.Add(lProjector);
                }
                catch
                {
                    LogList.Instance.PostLog($"Exeption load device {dvs.Attribute("IP")}", "HUB");
                }
            }


            List<XElement> scenes = new List<XElement>(GetOrMakeElement(ROOT, "Scenes", string.Empty).Elements());
            appMainModel.Scenes.Clear();
            foreach (XElement XScene in scenes)
            {
                appMainModel.Scenes.Add(mws.GetScene(XScene, appMainModel.Projectors));
            }

            Progress.Instance.End();
            return appMainModel;
        }

        public static ProjectionScene GetScene(XElement XScene, ProjectorCollection devices)
        {
            XElement XSize = GetOrMakeElement(XScene, "Size", string.Empty);
            ProjectionScene Scene = new ProjectionScene()
            {
                NameID = GetOrMakeAttributeValue(XScene, "Name", "Scene"),
                TableID = int.Parse(GetOrMakeAttributeValue(XScene, "ID", "0")),
                Uid = Guid.Parse(GetOrMakeAttributeValue(XScene, "Uid", Guid.NewGuid().ToString())),
                DefAttach = GetOrMakeValue(XScene, "Attach", "Middle%Middle"),
                CursorMaskActivated = bool.Parse(GetOrMakeValue(XScene, "CursorMaskActivated", "False")),
                AttachDistanceX = double.Parse(GetOrMakeValue(XScene, "AttachDistanceX", "0")),
                AttachDistanceY = double.Parse(GetOrMakeValue(XScene, "AttachDistanceY", "0")),
                StepByStep = bool.Parse(GetOrMakeValue(XScene, "StepByStep", "False")),
                DefaultAngle = double.Parse(GetOrMakeValue(XScene, "DefaultAngle", "0")),
                DefaultMirror = bool.Parse(GetOrMakeValue(XScene, "DefaultMirror", "False")),
                DefaultScaleX = double.Parse(GetOrMakeValue(XScene, "DefaultScaleX", "1")),
                DefaultScaleY = double.Parse(GetOrMakeValue(XScene, "DefaultScaleY", "1")),
                Size = new CadRect3D(
                    new CadAnchor(CadPoint3D.Parse(GetOrMakeValue(XSize, "Point1", "0;0;0"))),
                    new CadAnchor(CadPoint3D.Parse(GetOrMakeValue(XSize, "Point2", "3000;3000;3000")))),
                ProjectionSetting = GetProjectionSetting(XScene, true)
            };

            XElement XMasks = GetOrMakeElement(XScene, "Masks", string.Empty);
            foreach (XElement xmask in XMasks.Elements())
            {
                CadRect3D rect = new CadRect3D(
                    new CadAnchor(CadPoint3D.Parse(GetOrMakeValue(xmask, "Point1", "0;0;0"))),
                    new CadAnchor(CadPoint3D.Parse(GetOrMakeValue(xmask, "Point2", "3000;3000;3000"))),
                    true, xmask.Name.LocalName);

                Scene.AddMask(rect);
            }

            XElement XObjects = GetOrMakeElement(XScene, "Objects", string.Empty);
            foreach(XElement XObject in XObjects.Elements())
            {
                if (XObject.Attribute("Type").Value == "1")
                {
                    CadLine cadLine = CadLine.Parse(XObject);
                    cadLine.Init();
                    Scene.Add(cadLine);
                }
            }

            XElement XDevices = GetOrMakeElement(XScene, "Devices", string.Empty);

            foreach (XElement XID in XDevices.Elements())
            {
                if (devices.GetDeviceUid(Guid.Parse(XID.Value)) is LProjector lDevice)
                {
                    Scene.AddDevice(lDevice);
                }
            }
            return Scene;
        }

        private static XElement FindDeviceOnUid(Guid Uid, XElement XDevices)
        {
            foreach(XElement XDevice in XDevices.Elements())
            {
                if (XDevice.Attribute("Uid").Value == Uid.ToString()) return XDevice;
            }
            return null;
        }


        private static LProjector GetDevice(XElement XDevice, int Number, DeviceType deviceType)
        {
            if (XDevice != null)
            {
                if (XDevice != null)
                {
                    IPAddress iPAddress = IPAddress.Parse(XDevice.Attribute("IP").Value);
                    LProjector _device = DevicesMg.GetDeviceAsync(iPAddress, deviceType, Number).Result;
                    _device.Uid = Guid.Parse(GetOrMakeAttributeValue(XDevice, "Uid", deviceType.ToString()));

                    _device.NameID = GetOrMakeAttributeValue(XDevice, "Name", deviceType.ToString());
                    _device.TimeColorShift = double.Parse(GetOrMakeValue(XDevice, "TimeColorShift", _device.TimeColorShift.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);

                    _device.Alpha = double.Parse(GetOrMakeValue(XDevice, "Alpha", _device.Alpha.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
                    _device.FPS = int.Parse(GetOrMakeValue(XDevice, "FPS", _device.FPS.ToString(CultureInfo.InvariantCulture)));
                    _device.ScanRate = int.Parse(GetOrMakeValue(XDevice, "ScanRate", _device.ScanRate.ToString(CultureInfo.InvariantCulture)));                    //ScanRateRealc

                    _device.HeightResolution = double.Parse(GetOrMakeValue(XDevice, "HeightResolution", _device.HeightResolution.ToString(CultureInfo.InvariantCulture)));
                    _device.WidthResolutuon = double.Parse(GetOrMakeValue(XDevice, "WidthResolutuon", _device.WidthResolutuon.ToString(CultureInfo.InvariantCulture)));

                    _device.InvertedX = bool.Parse(GetOrMakeValue(XDevice, "InvertedX", _device.InvertedX.ToString(CultureInfo.InvariantCulture)));                             //InvertedX
                    _device.InvertedY = bool.Parse(GetOrMakeValue(XDevice, "InvertedY", _device.InvertedY.ToString(CultureInfo.InvariantCulture)));
                    _device.OnlySelectMesh = bool.Parse(GetOrMakeValue(XDevice, "OnlySelectMesh", "False"));

                    _device.OwnedSetting = bool.Parse(GetOrMakeValue(XDevice, "OwnedSetting", "False"));
                    _device.PolyMeshUsed = bool.Parse(GetOrMakeValue(XDevice, "PolyMeshUsed", "False"));

                    XElement XSize = GetOrMakeElement(XDevice, "Size", string.Empty);
                    _device.Size = new CadRect3D
                        (CadAnchor.Parse(GetOrMakeValue(XSize, "Point1", "0; 0; 0")),
                        CadAnchor.Parse(GetOrMakeValue(XSize, "Point2", "1; 1; 1")),
                        true);

                    if (_device.OwnedSetting == true) _device.ProjectionSetting = GetProjectionSetting(XDevice, false);

                    ///
                    /// Meshes
                    ///

                    _device.UseEllipsoid = bool.Parse(GetOrMakeValue(XDevice, "UseEllipsoid", _device.UseEllipsoid.ToString(CultureInfo.InvariantCulture)));
                    XElement XEllipsoid = GetOrMakeElement(XDevice, "Ellipsoid", string.Empty);
                    _device.Ellipsoid = GetEllipsoid(XEllipsoid);

                    XElement XMeshes = GetOrMakeElement(XDevice, "Meshes", string.Empty);

                    foreach(XElement XMesh in XMeshes.Elements())
                    {
                        _device.Meshes.Add(GetDeviceMesh(XMesh, _device));
                    }

                    XElement XSMeshes = GetOrMakeElement(XDevice, "SelectMeshes", string.Empty);
                    foreach (XElement XUid in XSMeshes.Elements())
                    {
                       if (_device.GetMeshByID(Guid.Parse(XUid.Value)) is ProjectorMesh mesh)
                        {
                            _device.AddSelectMesh(mesh);
                        }
                    }
                    _device.SelectMesh = _device.GetMeshByID(Guid.Parse(GetOrMakeValue(XDevice, "SelectMesh", Guid.Empty.ToString())));

                    return _device;
                }               
            }
            return null;
        }

        private static CorrectionEllipsoid GetEllipsoid(XElement XEllipsoid)
        {
            CorrectionEllipsoid ellipsoid = new CorrectionEllipsoid()
            {
                AngleX = double.Parse(GetOrMakeValue(XEllipsoid, "AngleX", (Math.PI * 0.25).ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture),
                AngleY = double.Parse(GetOrMakeValue(XEllipsoid, "AngleY", (Math.PI * 0.25).ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture),
                KoeffX = double.Parse(GetOrMakeValue(XEllipsoid, "KoeffX", "0"), CultureInfo.InvariantCulture),
                UseAxisCorrection = bool.Parse(GetOrMakeValue(XEllipsoid, "UseAxisCorrection", "false"))
            };

            
            //XElement XAxisCor = XEllipsoid.Element("XAxisCorrect");
            //if (XAxisCor != null)
            //{
            //    ellipsoid.XAxisCorrect.Clear();
            //    for (int i = 0; i < XAxisCor.Elements().Count(); i += 1)
            //    {
            //        XAttribute attribute = XAxisCor.Elements().ElementAt(i).Attribute("Value");
            //        ellipsoid.XAxisCorrect.Add(new DoubleValue(double.Parse(attribute.Value, CultureInfo.InvariantCulture)));
            //    }
            //}

            
            //XElement YAxisCor = XEllipsoid.Element("YAxisCorrect");
            //if (YAxisCor != null)
            //{
            //    ellipsoid.YAxisCorrect.Clear();
            //    for (int i = 0; i < YAxisCor.Elements().Count(); i += 1)
            //    {
            //        XAttribute attribute = YAxisCor.Elements().ElementAt(i).Attribute("Value");
            //        ellipsoid.YAxisCorrect.Add(new DoubleValue(double.Parse(attribute.Value, CultureInfo.InvariantCulture)));
            //    }
            //}

            return ellipsoid;
        }

        private static LProjectionSetting GetProjectionSetting(XElement ObjectElement, bool force = false)
        {
            if (ObjectElement.Element("ProjectionSetting") != null)
            {
                XElement XProjectionSetting = ObjectElement.Element("ProjectionSetting");

                LProjectionSetting lProjectionSetting = new LProjectionSetting();

                lProjectionSetting.FindSolidElement = bool.Parse(GetOrMakeValue(XProjectionSetting, "FindSolidElement", false.ToString(CultureInfo.InvariantCulture)));
                lProjectionSetting.PathFindDeep = int.Parse(GetOrMakeValue(XProjectionSetting, "PathFindDeep", 1.ToString(CultureInfo.InvariantCulture)));

                //Red
                lProjectionSetting.Red = byte.Parse(GetOrMakeValue(XProjectionSetting, "Red", 255.ToString(CultureInfo.InvariantCulture)));
                lProjectionSetting.RedOn = bool.Parse(GetOrMakeValue(XProjectionSetting, "RedOn", true.ToString(CultureInfo.InvariantCulture)));
                //Green
                lProjectionSetting.Green = byte.Parse(GetOrMakeValue(XProjectionSetting, "Green", 255.ToString(CultureInfo.InvariantCulture)));
                lProjectionSetting.GreenOn = bool.Parse(GetOrMakeValue(XProjectionSetting, "GreenOn", true.ToString(CultureInfo.InvariantCulture)));
                //Blue
                lProjectionSetting.Blue = byte.Parse(GetOrMakeValue(XProjectionSetting, "Blue", 255.ToString(CultureInfo.InvariantCulture)));
                lProjectionSetting.BlueOn = bool.Parse(GetOrMakeValue(XProjectionSetting, "BlueOn", true.ToString(CultureInfo.InvariantCulture)));


                lProjectionSetting.BlankWait = byte.Parse(GetOrMakeValue(XProjectionSetting, "BlankWait", 4.ToString(CultureInfo.InvariantCulture)));               //EndBlanckWait
                lProjectionSetting.LineWait = byte.Parse(GetOrMakeValue(XProjectionSetting, "LineWait", 4.ToString(CultureInfo.InvariantCulture)));             //StartLineWai
                lProjectionSetting.BlankTail = double.Parse(GetOrMakeValue(XProjectionSetting, "BlankTail", 0.5.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
                lProjectionSetting.LineTail = double.Parse(GetOrMakeValue(XProjectionSetting, "LineTail", 0.5.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);

                lProjectionSetting.ParallelLineCount = int.Parse(GetOrMakeValue(XProjectionSetting, "ParallelLineCount", 0.ToString(CultureInfo.InvariantCulture)));
                lProjectionSetting.ParallelLineMargin = int.Parse(GetOrMakeValue(XProjectionSetting, "ParallelLineMargin", 10.ToString(CultureInfo.InvariantCulture)));


                lProjectionSetting.PointStep = MDouble.Parse(GetOrMakeValue(XProjectionSetting, "CRS", "35"));

                return lProjectionSetting;
            }
            else if (force == true)
            {
                return new LProjectionSetting();
            }

            else return null;
        }

        public static ProjectorMesh GetDeviceMesh(XElement XMesh, LProjector device)
        {
            if (XMesh.HasElements == true)
            {
                int Width = Int32.Parse(XMesh.Element("Width").Value);
                int Height = Int32.Parse(XMesh.Element("Height").Value);

                ProjectorMesh mesh = new ProjectorMesh();

                XElement XSize = GetOrMakeElement(XMesh, "Size", string.Empty);
                mesh.Size = new CadRect3D(
                    new CadAnchor(CadPoint3D.Parse(GetOrMakeValue(XSize, "Point1", "0;0;0"))),
                    new CadAnchor(CadPoint3D.Parse(GetOrMakeValue(XSize, "Point2", "1;1;1"))))
                    { 
                        Multiply = device.GetSize
                    };
                ;

                if (XMesh.Descendants("Point").Count() > 0)
                {
                    CadAnchor[,] newPoints = new CadAnchor[Height, Width];

                    for (int i = 0; i < Height; i += 1)
                    {
                        for (int j = 0; j < Width; j += 1)
                        {
                            XElement Xpoint = XMesh.Descendants("Point").ElementAt(i * Width + j);

                            CadAnchor anchor = new CadAnchor(
                                double.Parse(Xpoint.Attribute("X").Value.Trim('"'), CultureInfo.InvariantCulture),
                                double.Parse(Xpoint.Attribute("Y").Value.Trim('"'), CultureInfo.InvariantCulture),
                                0)
                            {
                                Multiply = device.GetSize
                            };

                            newPoints[i, j] = anchor;

                        }

                    }
                    mesh.Points = newPoints;
                }

                mesh.Name = XMesh.Name.LocalName;
                mesh.Uid = Guid.Parse(GetOrMakeAttributeValue(XMesh, "Uid", Guid.NewGuid().ToString()));
                mesh.Morph = bool.Parse(GetOrMakeValue(XMesh, "Morph", "True"));
                
                return mesh;
            }

            return new ProjectorMesh(ProjectorMesh.MakeMeshPoint(5, 5, device.GetSize), XMesh.Name.LocalName, MeshTypes.NONE);
        }
        #endregion

    }

}
