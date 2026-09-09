namespace CadProjector.FileFormats;

/// <summary>Progress callback for long file imports (UI overlay).</summary>
public readonly record struct ImportProgress(string Message, double? Fraction = null);
