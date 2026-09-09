using CadProjector.Core.Devices;
using CadProjector.Ilda;
using CadProjector.Rendering;

namespace CadProjector.Devices;

/// <summary>Same ILDA encode path used by <see cref="VltProjector.SendFrame"/> — for preview / export.</summary>
public static class DeviceIldaEncoder
{
    public static IldaFrame FromDeviceBag(LinesCollection frame, ProjectorProfile profile)
    {
        var ilda = IldaEncoder.FromNormalizedLines(
            frame,
            profile.WidthResolution,
            profile.HeightResolution,
            profile.Red,
            profile.Green,
            profile.Blue,
            profile.Alpha);

        for (var i = 0; i < Math.Min(ilda.Points.Count, frame.Points.Count); i++)
        {
            var rp = frame.Points[i];
            if (rp.Blanked) continue;
            ilda.Points[i].R = rp.Color.R;
            ilda.Points[i].G = rp.Color.G;
            ilda.Points[i].B = rp.Color.B;
        }

        ilda.FrameName = profile.DisplayName;
        return ilda;
    }
}
