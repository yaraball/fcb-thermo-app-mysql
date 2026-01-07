using System.Windows;
using System.Windows.Controls;

using fcb_thermo_app.Models;
using fcb_thermo_app.Services;

namespace fcb_thermo_app.Views;

public partial class MainWindow : Window
{
  /*
   * MainWindow
   * Main application window for the Oven Thermal Analytics Dashboard.
   * Handles layout, event wiring, and orchestrates drawing and statistics updates for all canvases.
   */
  private readonly DrawingService _drawingService = new();
  private readonly TemperatureMappingService _temperatureMappingService = new();

  /*
   * MainWindow()
   * Initializes the main window, sets up event handlers, and triggers the initial redraw.
   */
  public MainWindow()
  {
    InitializeComponent();
    Loaded += (_, __) => RedrawAll();
    SizeChanged += (_, __) => RedrawAll();
  }

  /*
   * RedrawAll
   * Updates statistics and redraws all canvases in the main window.
   */
  public void RedrawAll()
  {
    UpdateStats();
    RedrawCanvases();
  }

  /*
   * RedrawCanvases
   * Calculates canvas sizes, clears previous drawings, and renders all four main/reinforcement canvases using DrawingService.
   */
  public void RedrawCanvases()
  {
    // Calculate canvas dimensions based on the aspect ratio 5:2
    double canvasHeight = ActualWidth - 320;
    double canvasWidth = canvasHeight * (5.0 / 2.0);

    TopViewCanvas.Width = canvasWidth;
    TopViewCanvas.Height = canvasHeight;

    BottomViewCanvas.Width = canvasWidth;
    BottomViewCanvas.Height = canvasHeight;

    TopViewCanvasReinforcement.Width = canvasWidth;
    TopViewCanvasReinforcement.Height = canvasHeight;

    BottomViewCanvasReinforcement.Width = canvasWidth;
    BottomViewCanvasReinforcement.Height = canvasHeight;

    TopViewCanvas.Children.Clear();
    BottomViewCanvas.Children.Clear();
    TopViewCanvasReinforcement.Children.Clear();
    BottomViewCanvasReinforcement.Children.Clear();

    // Draw the Canvases
    var mainTop = Settings.ThermoelementsByCanvas.TryGetValue("MainTop", out var mt) ? mt : new List<Thermoelement>();
    var mainBottom = Settings.ThermoelementsByCanvas.TryGetValue("MainBottom", out var mb) ? mb : new List<Thermoelement>();
    var reinfTop = Settings.ThermoelementsByCanvas.TryGetValue("ReinfTop", out var rt) ? rt : new List<Thermoelement>();
    var reinfBottom = Settings.ThermoelementsByCanvas.TryGetValue("ReinfBottom", out var rb) ? rb : new List<Thermoelement>();

    _drawingService.DrawView(TopViewCanvas, "MainTop", mainTop, canvasWidth, canvasHeight);
    _drawingService.DrawView(BottomViewCanvas, "MainBottom", mainBottom, canvasWidth, canvasHeight);
    _drawingService.DrawView(TopViewCanvasReinforcement, "ReinfTop", reinfTop, canvasWidth, canvasHeight);
    _drawingService.DrawView(BottomViewCanvasReinforcement, "ReinfBottom", reinfBottom, canvasWidth, canvasHeight);
  }

  /*
   * UpdateStats
   * Calculates and updates average, min, max, and difference statistics for each canvas.
   * Updates UI elements and color scale settings accordingly.
   */
  public void UpdateStats()
  {
    // Get stats for each canvas
    var mainBodyTopAvg = _temperatureMappingService.GetAverageTemperature(Settings.ThermoelementsByCanvas.TryGetValue("MainTop", out var mainTop) ? mainTop : new List<Thermoelement>());
    var (mainBodyTopMin, mainBodyTopMax) = _temperatureMappingService.GetMinMaxTemperature(mainTop);
    var mainBodyTopDiff = (mainBodyTopMin.HasValue && mainBodyTopMax.HasValue) ? $"{mainBodyTopMax - mainBodyTopMin:F1}°C" : "N/A";

    var mainBodyBottomAvg = _temperatureMappingService.GetAverageTemperature(Settings.ThermoelementsByCanvas.TryGetValue("MainBottom", out var mainBottom) ? mainBottom : new List<Thermoelement>());
    var (mainBodyBottomMin, mainBodyBottomMax) = _temperatureMappingService.GetMinMaxTemperature(mainBottom);
    var mainBodyBottomDiff = (mainBodyBottomMin.HasValue && mainBodyBottomMax.HasValue) ? $"{mainBodyBottomMax - mainBodyBottomMin:F1}°C" : "N/A";

    var reinfTopAvg = _temperatureMappingService.GetAverageTemperature(Settings.ThermoelementsByCanvas.TryGetValue("ReinfTop", out var reinfTop) ? reinfTop : new List<Thermoelement>());
    var (reinfTopMin, reinfTopMax) = _temperatureMappingService.GetMinMaxTemperature(reinfTop);
    var reinfTopDiff = (reinfTopMin.HasValue && reinfTopMax.HasValue) ? $"{reinfTopMax - reinfTopMin:F1}°C" : "N/A";

    var reinfBottomAvg = _temperatureMappingService.GetAverageTemperature(Settings.ThermoelementsByCanvas.TryGetValue("ReinfBottom", out var reinfBottom) ? reinfBottom : new List<Thermoelement>());
    var (reinfBottomMin, reinfBottomMax) = _temperatureMappingService.GetMinMaxTemperature(reinfBottom);
    var reinfBottomDiff = (reinfBottomMin.HasValue && reinfBottomMax.HasValue) ? $"{reinfBottomMax - reinfBottomMin:F1}°C" : "N/A";

    // Set stats to UI elements
    // MainBody Top section
    SetTextBlock("MainBodyTopAverageTextBlock", mainBodyTopAvg);
    SetTextBlock("MainBodyTopMaxTextBlock", mainBodyTopMax.HasValue ? $"{mainBodyTopMax:F1}°C" : "N/A");
    SetTextBlock("MainBodyTopMinTextBlock", mainBodyTopMin.HasValue ? $"{mainBodyTopMin:F1}°C" : "N/A");
    SetTextBlock("MainBodyTopMaxDifferenceTextBlock", mainBodyTopDiff);

    // MainBody Bottom section
    SetTextBlock("MainBodyBottomAverageTextBlock", mainBodyBottomAvg);
    SetTextBlock("MainBodyBottomMaxTextBlock", mainBodyBottomMax.HasValue ? $"{mainBodyBottomMax:F1}°C" : "N/A");
    SetTextBlock("MainBodyBottomMinTextBlock", mainBodyBottomMin.HasValue ? $"{mainBodyBottomMin:F1}°C" : "N/A");
    SetTextBlock("MainBodyBottomMaxDifferenceTextBlock", mainBodyBottomDiff);

    // Reinforcement Top section
    SetTextBlock("ReinforcementTopAverageTextBlock", reinfTopAvg);
    SetTextBlock("ReinforcementTopMaxTextBlock", reinfTopMax.HasValue ? $"{reinfTopMax:F1}°C" : "N/A");
    SetTextBlock("ReinforcementTopMinTextBlock", reinfTopMin.HasValue ? $"{reinfTopMin:F1}°C" : "N/A");
    SetTextBlock("ReinforcementTopMaxDifferenceTextBlock", reinfTopDiff);

    // Reinforcement Bottom section
    SetTextBlock("ReinforcementBottomAverageTextBlock", reinfBottomAvg);
    SetTextBlock("ReinforcementBottomMaxTextBlock", reinfBottomMax.HasValue ? $"{reinfBottomMax:F1}°C" : "N/A");
    SetTextBlock("ReinforcementBottomMinTextBlock", reinfBottomMin.HasValue ? $"{reinfBottomMin:F1}°C" : "N/A");
    SetTextBlock("ReinforcementBottomMaxDifferenceTextBlock", reinfBottomDiff);

    // Update the color scale display if it is automatic
    if (Settings.IsColorScaleAuto)
    {
      double? overallMin = new[] { mainBodyTopMin, mainBodyBottomMin, reinfTopMin, reinfBottomMin }.Where(v => v.HasValue).Min();
      double? overallMax = new[] { mainBodyTopMax, mainBodyBottomMax, reinfTopMax, reinfBottomMax }.Where(v => v.HasValue).Max();

      Settings.ColorScaleMin = overallMin.HasValue ? overallMin.Value : -1;
      Settings.ColorScaleMax = overallMax.HasValue ? overallMax.Value : -1;

      BottomToolbar?.UpdateColorScaleDisplay();
    }
  }

  /*
   * SetTextBlock
   * Helper method to set the text of a TextBlock by name.
   *
   * Parameters:
   *   name  - The name of the TextBlock control.
   *   value - The string value to set.
   */
  private void SetTextBlock(string name, string value)
  {
    if (FindName(name) is TextBlock tb)
      tb.Text = value;
  }

  /*
   * MaximizeCanvas_Click
   * Handles the maximize button click for any canvas.
   * Opens a DetailWindow for the selected canvas and passes the corresponding thermoelements.
   */
  private void MaximizeCanvas_Click(object sender, RoutedEventArgs e)
  {
    if (sender is Button button && button.Tag is string canvasName)
    {
      DetailWindow detailWin;
      List<Thermoelement> thermoelements;

      switch (canvasName)
      {
        case "MainBodyTop":
          thermoelements = Settings.ThermoelementsByCanvas.TryGetValue("MainTop", out var mainTop) ? mainTop : new List<Thermoelement>();
          detailWin = new DetailWindow("MainTop", thermoelements);
          break;

        case "ReinforcementTop":
          thermoelements = Settings.ThermoelementsByCanvas.TryGetValue("ReinfTop", out var reinfTop) ? reinfTop : new List<Thermoelement>();
          detailWin = new DetailWindow("ReinfTop", thermoelements);
          break;

        case "MainBodyBottom":
          thermoelements = Settings.ThermoelementsByCanvas.TryGetValue("MainBottom", out var mainBottom) ? mainBottom : new List<Thermoelement>();
          detailWin = new DetailWindow("MainBottom", thermoelements);
          break;

        case "ReinforcementBottom":
          thermoelements = Settings.ThermoelementsByCanvas.TryGetValue("ReinfBottom", out var reinfBottom) ? reinfBottom : new List<Thermoelement>();
          detailWin = new DetailWindow("ReinfBottom", thermoelements);
          break;

        default:
          MessageBox.Show("Error opening detail window.");
          return;
      }
      detailWin.Owner = this;
      detailWin.ShowDialog();
    }
  }
}
