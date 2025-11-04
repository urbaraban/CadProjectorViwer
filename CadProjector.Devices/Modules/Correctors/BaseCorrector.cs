using CadProjector.Rendering;

namespace CadProjector.Devices.Modules.Correctors
{
    public abstract class LineCorrector : DeviceModule
    {
        public abstract VectorLine CorrectLine(VectorLine line);
    }

    public abstract class FrameCorrector : DeviceModule
    {
     public abstract LinesCollection CorrectFrame(LinesCollection frame);
    }
}