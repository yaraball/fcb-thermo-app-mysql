using System.Windows;
using System.Windows.Controls;

using fcb_thermo_app.Controllers;
using fcb_thermo_app.Models;

namespace fcb_thermo_app.Views
{
  /*
   * CanvasAssignmentGroup
   * Represents a group of canvas assignments for a specific DXF model, including a display string for UI.
   */
  public class CanvasAssignmentGroup
  {
    public required DXFModel Model { get; set; }
    public required List<CanvasAssignment> Assignments { get; set; }
    public required string DisplayString { get; set; }
  }

  /*
 * TopToolbar
 * UserControl for the application's top toolbar.
 * Handles model and trial selection, thermoelement placement, measurement import, layer toggling, export, and help.
 */
  public partial class TopToolbar : UserControl
  {
    private readonly DatabaseController dbController = new();
    private readonly DxfController _dxfController;

    /*
     * AnyThermoelementsPlaced
     * Checks if any thermoelements are currently placed on any canvas.
     * Returns: true if at least one thermoelement is placed; otherwise, false.
     */
    private bool AnyThermoelementsPlaced() { return Settings.ThermoelementsByCanvas.Values.Any(list => list.Count > 0); }

    /*
     * TopToolbar()
     * Initializes the TopToolbar, sets up controllers, and loads dropdowns for models and trials.
     */
    public TopToolbar()
    {
      InitializeComponent();
      _dxfController = new DxfController(dbController);
      LoadDxfDropdown();
      LoadTrialDropdown();
    }

    /*
     * LoadDxfDropdown
     * Loads all available DXF models from the database and populates the model selection ComboBox.
     */
    private void LoadDxfDropdown()
    {
      var dxfModels = _dxfController.GetAllDXFModels();
      ModelSelection.ItemsSource = dxfModels;
      ModelSelection.DisplayMemberPath = "NameWithSerial";
      ModelSelection.SelectedIndex = -1;
    }

    /*
     * SelectedModel_Changed
     * Handles changes in the selected DXF model.
     * Prompts the user if thermoelements are placed, resets settings, and updates the UI.
     */
    private void SelectedModel_Changed(object sender, SelectionChangedEventArgs e)
    {
      if (ModelSelection.SelectedItem == null || ModelSelection.SelectedItem == Settings.CurrentDxfModel || TrialSelection.IsDropDownOpen == true)
        return;

      if (AnyThermoelementsPlaced())
      {
        var result = MessageBox.Show("Changing the model will start a new measurement session. Do you want to continue?", "Confirm Model Change", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
          if (Settings.CurrentDxfModel != null)
          {
            ModelSelection.SelectedItem = Settings.CurrentDxfModel;
            ModelSelection.Text = Settings.CurrentDxfModel.NameWithSerial;
          }
          else
          {
            ModelSelection.SelectedIndex = -1;
            ModelSelection.Text = "";
          }
          return;
        }
        Settings.Reset();
        TrialSelection.SelectedIndex = -1;
        TrialSelection.Text = "";
        (Application.Current.MainWindow as MainWindow)?.BottomToolbar?.UpdateTimelineUI();
        (Application.Current.MainWindow as MainWindow)?.BottomToolbar?.UpdateColorScaleDisplay();
      }
      if (ModelSelection.SelectedItem is DXFModel selected)
      {
        Settings.CurrentDxfModel = selected;
        ModelSelection.Text = selected.NameWithSerial;
      }
      else { MessageBox.Show("The selected item is not a valid DXF model.", "Selection Error", MessageBoxButton.OK, MessageBoxImage.Warning); }

      (Application.Current.MainWindow as MainWindow)?.RedrawAll();
    }

    /*
     * TrialSelection_DropDownOpened
     * Reloads the trial dropdown when it is opened to ensure up-to-date data.
     */
    private void TrialSelection_DropDownOpened(object sender, EventArgs e)
    {
      LoadTrialDropdown();
    }

    /*
     * LoadTrialDropdown
     * Loads all canvas assignments from the database, groups them by trial, and populates the trial selection ComboBox.
     */
    private void LoadTrialDropdown()
    {
      // Get all assignments from DB
      var assignments = dbController.GetAllCanvasAssignments(); // Implement this if not present
      var dxfModels = _dxfController.GetAllDXFModels().ToDictionary(m => m.Id);

      // Group by group id (assuming every 4 assignments share a group id or consecutive ids)
      var grouped = assignments
          .OrderBy(a => a.Id)
          .Select((a, idx) => new { Assignment = a, Index = idx })
          .GroupBy(x => x.Index / 4)
          .Select(g =>
          {
            var groupAssignments = g.Select(x => x.Assignment).ToList();
            var modelId = groupAssignments.First().DXFModelId;
            dxfModels.TryGetValue(modelId, out var model);

            if (model == null)
            {
              MessageBox.Show(
                  $"The DXF model with ID {modelId} for this trial could not be found in the database.",
                  "Missing DXF Model",
                  MessageBoxButton.OK,
                  MessageBoxImage.Warning);
              return null; // Skip this group
            }

            // Get Filename as Indicator of trial
            var firstAssignment = groupAssignments.First();
            int measurementId = firstAssignment.Measurement1To10Id != -1
                ? firstAssignment.Measurement1To10Id
                : firstAssignment.Measurement11To20Id;

            string timestamp = "";
            if (measurementId != -1)
            {
              var measurement = dbController.GetMeasurementById(measurementId);
              timestamp = ExtractFirstTimestamp(measurement?.Data ?? "");
            }

            return new CanvasAssignmentGroup
            {
              Model = model,
              Assignments = groupAssignments,
              DisplayString = string.IsNullOrWhiteSpace(model?.SerialNumber)
                ? $"{model?.Name} - {timestamp}"
                : $"{model?.Name} ({model?.SerialNumber}) - {timestamp}"
            };
          })
          .ToList();

      TrialSelection.ItemsSource = grouped;
      TrialSelection.SelectedIndex = -1;
    }

    /*
     * SelectedTrial_Changed
     * Handles changes in the selected trial.
     * Prompts the user if thermoelements are placed, resets settings, loads assignments, and updates the UI.
     */
    private void SelectedTrial_Changed(object sender, SelectionChangedEventArgs e)
    {
      if (TrialSelection.SelectedItem is not CanvasAssignmentGroup group)
        return;

      // Only ask for confirmation if thermoelements are placed but no canvas assignments yet
      bool thermoelementsPlaced = AnyThermoelementsPlaced();
      bool noCanvasAssignments = Settings.CanvasAssignments.Count == 0;

      if (thermoelementsPlaced && noCanvasAssignments)
      {
        var result = MessageBox.Show(
            "Your thermoelemt placement will be lost if you change the trial before importing measurements. Do you want to continue?",
            "Confirm Trial Change",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
          TrialSelection.SelectedIndex = -1;
          return;
        }
      }

      // Clear model selection UI
      ModelSelection.SelectedIndex = -1;
      ModelSelection.Text = "";

      // Reset all settings
      Settings.Reset();

      // Set current DXF model
      Settings.CurrentDxfModel = group.Model;

      foreach (var assignment in group.Assignments)
      {
        var canvasType = assignment.Type;

        // Assign CanvasAssignment
        Settings.CanvasAssignments[canvasType] = assignment;

        // Fetch and assign Thermoelements
        List<Thermoelement> thermoelements = dbController.GetThermoelementsForAssignment(assignment.Thermoelements);
        Settings.ThermoelementsByCanvas[canvasType] = thermoelements;

        // Fetch and assign Measurements
        Settings.Measurements1To10 = dbController.GetMeasurementById(assignment.Measurement1To10Id);
        Settings.Measurements11To20 = dbController.GetMeasurementById(assignment.Measurement11To20Id);
      }

      // Redraw UI
      (Application.Current.MainWindow as MainWindow)?.RedrawAll();
      (Application.Current.MainWindow as MainWindow)?.BottomToolbar?.UpdateTimelineUI();
      (Application.Current.MainWindow as MainWindow)?.BottomToolbar?.UpdateColorScaleDisplay();
    }

    /*
     * ExtractFirstTimestamp
     * Extracts the first timestamp from a measurement data string using regex.
     * Returns: The first timestamp found, or a fallback string if not found.
     */
    private string ExtractFirstTimestamp(string data)
    {
      if (string.IsNullOrEmpty(data)) return "bli";
      var match = System.Text.RegularExpressions.Regex.Match(
        data,
        @"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(?:\.\d+)?"
      );
      return match.Success ? match.Value : "bla";
    }

    /*
     * PlaceThermoelements_Click
     * Opens the thermoelement placement window for the current model.
     * Prompts the user if no model is selected.
     */
    private void PlaceThermoelements_Click(object sender, RoutedEventArgs e)
    {
      if (Settings.CurrentDxfModel is not DXFModel)
      {
        MessageBox.Show("Please select a valid DXF model before placing thermoelements.", "Invalid Model", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      // Only open the placement window; all saving is handled inside the window
      var placementWindow = new ThermoelementPlacementWindow();
      placementWindow.Owner = Application.Current.MainWindow;
      var result = placementWindow.ShowDialog();

      // Optionally, you can trigger a redraw if placement was confirmed
      if (result == true)
      {
        (Application.Current.MainWindow as MainWindow)?.RedrawAll();
        MessageBox.Show("Thermoelement placement completed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
      }
    }

    /*
     * ImportGBD_Click
     * Opens the measurement import window.
     * Prompts the user if no thermoelements are placed.
     */
    private void ImportGBD_Click(object sender, RoutedEventArgs e)
    {
      if (!AnyThermoelementsPlaced())
      {
        MessageBox.Show("Please place at least one thermoelement before importing measurements.", "No Thermoelements", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      var importWindow = new MeasurementImportWindow();
      importWindow.Owner = Application.Current.MainWindow;
      importWindow.ShowDialog();
    }


    /*
     * Layers_Click
     * Opens the context menu for layer visibility toggles.
     */
    private void Layers_Click(object sender, RoutedEventArgs e)
    {
      if (sender is Button button && button.ContextMenu != null) { button.ContextMenu.IsOpen = true; }
    }

    /*
     * TogglePyrometer_Click, TogglePyrometerPositions_Click, ToggleThermoelementHeatmap_Click,
     * ToggleThermoelementValues_Click, TogglePerformanceSettingsGrid_Click,
     * TogglePerformanceSettingsHeatmap_Click, TogglePerformanceSettingsValues_Click
     * Toggles the visibility of various layers and triggers a redraw of the main window.
     */
    private void TogglePyrometer_Click(object sender, RoutedEventArgs e)
    {
      if (sender is MenuItem menuItem)
      {
        Settings.LayerSettings["ShowPyrometer"] = menuItem.IsChecked;
        (Application.Current.MainWindow as MainWindow)?.RedrawAll();
      }
    }

    private void TogglePyrometerPositions_Click(object sender, RoutedEventArgs e)
    {
      if (sender is MenuItem menuItem)
      {
        Settings.LayerSettings["ShowPyrometerPositions"] = menuItem.IsChecked;
        (Application.Current.MainWindow as MainWindow)?.RedrawAll();
      }
    }

    private void ToggleThermoelementHeatmap_Click(object sender, RoutedEventArgs e)
    {
      if (sender is MenuItem menuItem)
      {
        Settings.LayerSettings["ShowThermoelementHeatmap"] = menuItem.IsChecked;
        (Application.Current.MainWindow as MainWindow)?.RedrawAll();
      }
    }

    private void ToggleThermoelementValues_Click(object sender, RoutedEventArgs e)
    {
      if (sender is MenuItem menuItem)
      {
        Settings.LayerSettings["ShowThermoelementValues"] = menuItem.IsChecked;
        (Application.Current.MainWindow as MainWindow)?.RedrawAll();
      }
    }

    private void TogglePerformanceSettingsGrid_Click(object sender, RoutedEventArgs e)
    {
      if (sender is MenuItem menuItem)
      {
        Settings.LayerSettings["ShowPerformanceGrid"] = menuItem.IsChecked;
        (Application.Current.MainWindow as MainWindow)?.RedrawAll();
      }
    }

    private void TogglePerformanceSettingsHeatmap_Click(object sender, RoutedEventArgs e)
    {
      if (sender is MenuItem menuItem)
      {
        Settings.LayerSettings["ShowPerformanceHeatmap"] = menuItem.IsChecked;
        (Application.Current.MainWindow as MainWindow)?.RedrawAll();
      }
    }

    private void TogglePerformanceSettingsValues_Click(object sender, RoutedEventArgs e)
    {
      if (sender is MenuItem menuItem)
      {
        Settings.LayerSettings["ShowPerformanceValues"] = menuItem.IsChecked;
        (Application.Current.MainWindow as MainWindow)?.RedrawAll();
      }
    }

    /*
     * Export_Click
     * Opens the export window for exporting model and trial data.
     * Prompts the user if the main window is not found.
     */
    private void Export_Click(object sender, RoutedEventArgs e)
    {
      var mainWindow = Application.Current.MainWindow as MainWindow;
      if (mainWindow == null)
      {
        MessageBox.Show("Main window not found.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      var exportWindow = new ExportWindow(mainWindow)
      {
        Owner = mainWindow
      };
      exportWindow.ShowDialog();
    }


    /*
     * CreateNewModel_Click
     * Opens the model import window to create a new DXF model.
     * Reloads the model dropdown and updates the UI if a new model is created.
     */
    private void CreateNewModel_Click(object sender, RoutedEventArgs e)
    {
      var createModelWindow = new ModelImportWindow
      {
        Owner = Application.Current.MainWindow
      };

      if (createModelWindow.ShowDialog() == true)
      {
        LoadDxfDropdown();

        if (Settings.CurrentDxfModel is DXFModel dxfModel)
        {
          ModelSelection.SelectedItem = dxfModel;
          ModelSelection.Text = dxfModel.NameWithSerial;
          (Application.Current.MainWindow as MainWindow)?.RedrawAll();
        }
      }
    }

    /*
     * HelpButton_Click
     * Opens the help window with instructions for the user.
     */
    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
      var helpWindow = new HelpWindow();
      helpWindow.Owner = Window.GetWindow(this);
      helpWindow.ShowDialog();
    }
  }
}
