using MonchaSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToGeometryConverter;
using ToGeometryConverter.Format;
using ToGeometryConverter.Object;

namespace MonchaCadViewer.StaticTools
{
    public static class GetGC
    {
        public async static Task<GCCollection> Load(string Filename)
        {
            IFormat format = ToGC.GetConverter(Filename);
            return await format.GetAsync(Filename, MonchaHub.ProjectionSetting.PointStep.MX);
        }
    }
}
