using CadProjectorSDK.CadObjects.Variable;
using CadProjectorSDK.CadObjects;
using CadProjectorSDK.Device.Controllers;
using CadProjectorSDK.Device.Mesh;
using CadProjectorSDK.Device;
using CadProjectorSDK.Scenes;
using CadProjectorSDK.Setting;
using CadProjectorViewer.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using CadProjectorSDK.Config;
using CadProjectorSDK;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer.Configurations
{
    internal class Config
    {
        public XElement ROOT;

        public XElement OUTDEVICE;

        public XElement HUB;

        public List<string> Keys = new List<string>();

        private XDocument xDocument;

        public Config(string path)
        {
            FileInfo fileInfo = new FileInfo(AppSt.Default.cl_moncha_path);
            if (fileInfo.Exists == true && fileInfo.Extension.ToLower() == "mws")
                this.xDocument = XDocument.Load(path);
            else
                this.xDocument = new XDocument();

            this.ROOT = GetOrMakeElement(this.xDocument, "Moncha", string.Empty);
            this.OUTDEVICE = GetOrMakeElement(this.ROOT, "OutputDeviceHandler", string.Empty);
            GetOrMakeElement(this.ROOT, "Devices", string.Empty);
            this.HUB = GetOrMakeElement(this.OUTDEVICE, "HubSetting", string.Empty);
            XElement XKeys = GetOrMakeElement(this.HUB, "LicenseKeys", string.Empty);
            GetOrMakeElement(this.HUB, "Scenes", string.Empty);

            foreach (XElement xElement in XKeys.Descendants("Key"))
            {
                this.Keys.Add(xElement.Value);
            }
        }

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


        #region Read

        public AppMainModel Get()
        {
            AppMainModel appMainModel = new AppMainModel();

            return appMainModel;
        }

        public async static Task<ProjectionScene> GetScene(XElement XScene, ProjectorCollection devices)
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
            foreach (XElement XObject in XObjects.Elements())
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

        private XElement FindDeviceOnUid(Guid Uid, XElement XDevices)
        {
            foreach (XElement XDevice in XDevices.Elements())
            {
                if (XDevice.Attribute("Uid").Value == Uid.ToString()) return XDevice;
            }
            return null;
        }


        public async Task<LProjector> GetDevice(Guid Uid, int Number, DeviceType deviceType)
        {
            if (this.ROOT != null)
            {
                XElement XDevices = GetOrMakeElement(this.OUTDEVICE, "Devices", string.Empty);

                XElement XDevice = FindDeviceOnUid(Uid, XDevices);

                if (XDevice != null)
                {
                    IPAddress iPAddress = IPAddress.Parse(XDevice.Attribute("IP").Value);
                    LProjector _device = await DevicesMg.GetDeviceAsync(iPAddress, deviceType, Number);
                    _device.Uid = Uid;

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

                    foreach (XElement XMesh in XMeshes.Elements())
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

        private CorrectionEllipsoid GetEllipsoid(XElement XEllipsoid)
        {
            CorrectionEllipsoid ellipsoid = new CorrectionEllipsoid()
            {
                AngleX = double.Parse(GetOrMakeValue(XEllipsoid, "AngleX", (Math.PI * 0.25).ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture),
                AngleY = double.Parse(GetOrMakeValue(XEllipsoid, "AngleY", (Math.PI * 0.25).ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture),
                AngleOffsetX = double.Parse(GetOrMakeValue(XEllipsoid, "AngleOffsetX", "0"), CultureInfo.InvariantCulture),
                AngleOffsetY = double.Parse(GetOrMakeValue(XEllipsoid, "AngleOffsetY", "0"), CultureInfo.InvariantCulture),
            };


            XElement XAxisCor = XEllipsoid.Element("XAxisCorrect");
            if (XAxisCor != null)
            {
                ellipsoid.XAxisCorrect.Clear();
                for (int i = 0; i < XAxisCor.Elements().Count(); i += 1)
                {
                    XAttribute attribute = XAxisCor.Elements().ElementAt(i).Attribute("Value");
                    ellipsoid.XAxisCorrect.Add(new DoubleValue(double.Parse(attribute.Value, CultureInfo.InvariantCulture)));
                }
            }


            XElement YAxisCor = XEllipsoid.Element("YAxisCorrect");
            if (YAxisCor != null)
            {
                ellipsoid.YAxisCorrect.Clear();
                for (int i = 0; i < YAxisCor.Elements().Count(); i += 1)
                {
                    XAttribute attribute = YAxisCor.Elements().ElementAt(i).Attribute("Value");
                    ellipsoid.YAxisCorrect.Add(new DoubleValue(double.Parse(attribute.Value, CultureInfo.InvariantCulture)));
                }
            }

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
