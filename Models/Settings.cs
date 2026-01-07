
using System.IO;
using System.Windows;
using fcb_thermo_app.Models;

/*
 * Settings
 * Static class holding global application state, configuration, and references for the thermo app.
 */
public static class Settings
{
  /* CurrentDxfModel: The currently selected DXF model. */
  public static DXFModel? CurrentDxfModel = null;

  /* CanvasAssignments: Canvas assignments by type ("MainTop", "MainBottom", "ReinfTop", "ReinfBottom"). */
  public static Dictionary<string, CanvasAssignment> CanvasAssignments = new();

  /* Measurements1To10: Measurement for channels 1-10, by canvas type. */
  public static Measurement? Measurements1To10 = null;

  /* Measurements11To20: Measurement for channels 11-20, by canvas type. */
  public static Measurement? Measurements11To20 = null;

  /* ThermoelementsByCanvas: Thermoelements for each canvas assignment (by canvas type). */
  public static Dictionary<string, List<Thermoelement>> ThermoelementsByCanvas = new();

  /* ColorScaleMin: Minimum value for color scale (°C), -1 for greyed out display. */
  public static double ColorScaleMin { get; set; } = -1;

  /* ColorScaleMax: Maximum value for color scale (°C), -1 for greyed out display. */
  public static double ColorScaleMax { get; set; } = -1;

  /* IsColorScaleAuto: Whether the color scale is set automatically based on data. */
  public static bool IsColorScaleAuto { get; set; } = true;

  /* CurrentTimeOffset: Which timestamp to show in the UI. */
  public static TimeSpan CurrentTimeOffset = TimeSpan.Zero;

  /* SamplingIntervalSeconds: Default sampling interval in seconds, updated on import. Used for time calculations in slider. */
  public static double SamplingIntervalSeconds = 1.0;

  /* LayerSettings: Settings for which layers/features are visible in the UI. */
  public static Dictionary<string, bool> LayerSettings = new()
    {
      { "ShowPyrometer", true },
      { "ShowPyrometerPositions", true },
      { "ShowThermoelementHeatmap", true },
      { "ShowThermoelementValues", true },
      { "ShowPerformanceGrid", true },
      { "ShowPerformanceHeatmap", true },
      { "ShowPerformanceValues", true }
    };

  /*
   * PyrometerPositions: DXFModel containing file content for pyrometer positions.
   * Used for overlaying pyrometer holes on the canvas.
   */
  public static DXFModel PyrometerPositions { get; } = new DXFModel
  {
    Name = "PyrometerPositions",
    MainBodyFileContent = LoadResourceFile(Path.Combine(AppContext.BaseDirectory, "Resources", "MainbodyPyrometers.dxf")),
    ReinforcementFileContent = LoadResourceFile(Path.Combine(AppContext.BaseDirectory, "Resources", "ReinforcementPyrometers.dxf"))
  };

  /*
   * Reset: Resets all global state and settings to their default values.
   */
  public static void Reset()
  {
    CurrentDxfModel = null;
    CanvasAssignments.Clear();
    Measurements1To10 = null;
    Measurements11To20 = null;
    ThermoelementsByCanvas.Clear();
    ColorScaleMin = -1;
    ColorScaleMax = -1;
    IsColorScaleAuto = true;
    CurrentTimeOffset = TimeSpan.Zero;
    SamplingIntervalSeconds = 1.0;
    foreach (var key in LayerSettings.Keys.ToList())
    {
      LayerSettings[key] = true;
    }
  }

  /*
   * LoadResourceFile: Loads a resource file from disk and returns its byte content.
   * Shows a warning if the file is missing.
   *
   * filePath: Path to the resource file.
   * Returns: Byte array of file content, or null if not found.
   */
  private static byte[]? LoadResourceFile(string filePath)
  {
    if (File.Exists(filePath))
    {
      return File.ReadAllBytes(filePath);
    }
    MessageBox.Show(
        $"The resource file to show pyrometer holes was not found: {filePath}",
        "Resource File Missing",
        MessageBoxButton.OK,
        MessageBoxImage.Warning
    );
    return null;
  }
}
