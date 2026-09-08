using System.ComponentModel;

namespace CadProjector.Core.Devices;

/// <summary>How interior mesh points are recomputed after a corner/edge edit.</summary>
public enum MeshMorphType
{
    [Description("Полная")]
    Full = 0,
    [Description("Горизонтальная")]
    Horizontal = 1,
    [Description("Вертикальная")]
    Vertical = 2,
    [Description("Одиночная")]
    Single = 3
}

/// <summary>Laser / table calibration guide drawn from the selected control point.</summary>
public enum MeshCalibrationForm
{
    [Description("Точка")]
    Dot = 0,
    [Description("Квадрат")]
    Rect = 1,
    [Description("Мини-квадрат")]
    MiniRect = 2,
    [Description("Крест")]
    Cross = 3,
    [Description("Вертикальные линии")]
    HLine = 4,
    [Description("Горизонтальные линии")]
    WLine = 5,
    [Description("Сетка")]
    Mesh = 6,
    [Description("Мини-крест")]
    MiniCross = 7
}
