using CadProjectorSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToGeometryConverter;
using ToGeometryConverter.Format;
using ToGeometryConverter.Object;

namespace CadProjectorViewer.StaticTools
{
    public static class GetGC
    {
        public async static Task<GCCollection> Load(string Filename)
        {
            IFormat format = ToGC.GetConverter(Filename);
            return await format.GetAsync(Filename, ProjectorHub.ProjectionSetting.PointStep.MX);
        }
    }
}
