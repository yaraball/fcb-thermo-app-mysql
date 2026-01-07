using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using fcb_thermo_app.Controllers;
using fcb_thermo_app.Models;

namespace fcb_thermo_app.Views
{
  /*
   * MeasurementImportWindow
   * Dialog window for importing measurement files and entering performance/pyrometer settings.
   * Handles file selection, dynamic grid generation, pyrometer placement, and import logic.
   */
  public partial class MeasurementImportWindow : Window, INotifyPropertyChanged
  {
    private readonly DatabaseController dbController = new DatabaseController();
    public string? Channels1To10FilePath { get; private set; }
    public string? Channels11To20FilePath { get; private set; }
    public int PyrometerPositionMainTop { get; private set; } = -1; // Default to -1 (none selected)
    public int PyrometerPositionMainBottom { get; private set; } = -1; // Default to -1 (none selected)
    public int PyrometerPositionReinforcement { get; private set; } = -1; // Default to -1 (none selected)

    public int PyrometerNumberUpper => int.TryParse(PyrometerNumberUpperTextBox?.Text, out var val) ? val : -1;
    public int PyrometerNumberLower => int.TryParse(PyrometerNumberLowerTextBox?.Text, out var val) ? val : -1;
    public int PyrometerNumberReinforcement => int.TryParse(PyrometerNumberReinforcementTextBox?.Text, out var val) ? val : -1;

    public List<int> PerformanceSettings { get; private set; } = new List<int>();
    public event PropertyChangedEventHandler? PropertyChanged;
    private bool _reinforcement;
    public bool Reinforcement
    {
      get => _reinforcement;
      set
      {
        if (_reinforcement != value)
        {
          _reinforcement = value;
          OnPropertyChanged(nameof(Reinforcement));
          UpdateGridVisibility();
        }
      }
    }

    /*
     * MeasurementImportWindow()
     * Initializes the window, sets up data context, and generates performance settings input grids.
     */
    public MeasurementImportWindow()
    {
      InitializeComponent();
      DataContext = this;
      GeneratePerformanceSettingsInputs(); // Dynamically generate performance settings input
      UpdateGridVisibility();
    }

    /*
     * UpdateGridVisibility
     * Shows or hides the main body and reinforcement performance settings grids based on the reinforcement toggle.
     */
    private void UpdateGridVisibility()
    {
      if (Reinforcement)
      {
        ReinforcementStackPanel.Visibility = Visibility.Visible;
        UpperLowerStackPanel.Visibility = Visibility.Collapsed;
      }
      else
      {
        ReinforcementStackPanel.Visibility = Visibility.Collapsed;
        UpperLowerStackPanel.Visibility = Visibility.Visible;
      }
    }

    /*
     * GeneratePerformanceSettingsInputs
     * Dynamically generates input grids for performance settings for main body and reinforcement.
     */
    private void GeneratePerformanceSettingsInputs()
    {

      // Clear existing inputs
      PerformanceSettingsGridUpper.Children.Clear();
      PerformanceSettingsGridLower.Children.Clear();
      ReinforcementGrid.Children.Clear();

      // Generate inputs for the upper grid (88 inputs)
      GeneratePerformanceGrid(PerformanceSettingsGridUpper);

      // Generate inputs for the lower grid (88 inputs)
      GeneratePerformanceGrid(PerformanceSettingsGridLower, false, false);

      // Generate inputs for the reinforcement grid (32 inputs)
      GeneratePerformanceGrid(ReinforcementGrid, true);
    }

    /*
     * GeneratePerformanceGrid
     * Builds a grid of TextBoxes for performance input, with special handling for reinforcement and pyrometer buttons.
     */
    private void GeneratePerformanceGrid(Grid grid, bool isReinforcement = false, bool isTop = true)
    {
      // Define the grid structure (20 columns, 8 rows)
      for (int row = 0; row < 8; row++)
      {
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      }
      for (int col = 0; col < 20; col++)
      {
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
      }

      // Add TextBoxes with specific spans
      for (int row = 0; row < 8; row++)
      {
        for (int col = 0; col < 20; col++)
        {
          if (isReinforcement && (row < 2 || row > 5 || col < 2 || col > 17))
          {
            if (col % 2 == 0) // Only add boxes spanning 2 columns
            {
              GridInput(false, row, col, grid, Visibility.Hidden);
            }
          }
          // Adjust size and spans based on the row and column
          else if ((row == 1 || row == 2 || row == 3 || row == 4 || row == 5 || row == 6) && (col == 0 || col == 1 || col == 18 || col == 19)) // Vertical inputs in columns 0, 1, 18, 19
          {
            if (row % 2 == 1) // Only add vertical inputs in the first row of the double rows
            {
              GridInput(true, row, col, grid, Visibility.Visible);
            }
          }
          else
          {
            if (isReinforcement && (row == 3 || row == 4) && (col <= 3 || col >= 16))
            {
              // Vertical boxes in the sides of reinforcement top
              if (row % 2 == 1) // Only add boxes spanning 2 columns
              {
                GridInput(true, row, col, grid, Visibility.Visible);
              }
            }
            else
            {
              // Horizontal boxes in the middle columns
              if (col % 2 == 0) // Only add boxes spanning 2 columns
              {
                GridInput(false, row, col, grid, Visibility.Visible);
              }
            }
          }

          // Add pyrometer buttons to main body and upper reinforcement area
          if (!Reinforcement && col > 1 && col < 18 || Reinforcement && col > 3 && col < 16 && row > 1 && row < 6)
          {
            AddPyrometers(grid, row, col);
          }
        }
      }
    }

    /*
     * GridInput
     * Adds a TextBox to the performance grid, with validation and event handlers.
     */
    private void GridInput(bool vertical, int row, int col, Grid grid, Visibility visibility)
    {
      var textBox = new TextBox
      {
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
        BorderThickness = new Thickness(0.5),
        Visibility = visibility,
        Text = "0", // Default value
      };

      if (vertical)
      {
        textBox.Width = 25;
        textBox.Height = 40;
        Grid.SetRowSpan(textBox, 2); // Vertical span
      }
      else
      {
        textBox.Width = 50;
        textBox.Height = 20;
        Grid.SetColumnSpan(textBox, 2); // Horizontal span
      }

      // Attach events using the helper method
      AttachTextBoxEvents(textBox);

      Grid.SetRow(textBox, row);
      Grid.SetColumn(textBox, col);
      grid.Children.Add(textBox);
    }

    /*
    * AttachTextBoxEvents
    * Attaches validation and focus events to a performance settings TextBox.
    */
    private void AttachTextBoxEvents(TextBox textBox)
    {
      // Attach the TextChanged event for validation
      textBox.TextChanged += (sender, e) =>
      {
        if (sender is TextBox tb)
        {
          // Allow empty fields
          if (string.IsNullOrWhiteSpace(tb.Text))
          {
            return;
          }

          // Try to parse the input
          if (!int.TryParse(tb.Text, out int value) || value < 0 || value > 100)
          {
            // Show a MessageBox to inform the user
            MessageBox.Show("Please enter a value between 0 and 100.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);

            // Reset the value to the default (0)
            tb.Text = "0";
            tb.CaretIndex = tb.Text.Length; // Move the caret to the end
          }
        }
      };

      // Attach the GotFocus event to clear the default value
      textBox.GotFocus += (sender, e) =>
      {
        if (sender is TextBox tb)
        {
          tb.SelectAll(); // Highlight the current value when the user clicks into the field
        }
      };

      // Attach the PreviewMouseDown event to handle mouse clicks
      textBox.PreviewMouseDown += (sender, e) =>
      {
        if (sender is TextBox tb && !tb.IsFocused)
        {
          e.Handled = true; // Prevent the default focus behavior
          tb.Focus(); // Set focus to the TextBox
        }
      };
    }

    /*
     * AddPyrometers
     * Adds invisible buttons to the grid for pyrometer placement, handling selection and focus logic.
     */
    private void AddPyrometers(Grid grid, int row, int col)
    {
      var pyrometerBtn = new Button
      {
        Opacity = 0, // Invisible by default
        Background = new SolidColorBrush(Colors.Red), // Red color for visibility
        Width = 25,
        Height = 20,
        Tag = row * 20 + col, // Unique index for each position
        IsTabStop = false
      };

      // Position the button in the grid cell
      Grid.SetRow(pyrometerBtn, row);
      Grid.SetColumn(pyrometerBtn, col);

      pyrometerBtn.MouseRightButtonDown += (s, e) =>
      {
        int idx = (int)pyrometerBtn.Tag;
        if (grid == PerformanceSettingsGridUpper)
        {
          if (PyrometerPositionMainTop == idx)
          {
            // Deselect
            pyrometerBtn.Opacity = 0;
            PyrometerPositionMainTop = -1;
          }
          else
          {
            // Select current and deselect previous
            foreach (var child in grid.Children)
              if (child is Button btn && btn != pyrometerBtn)
                btn.Opacity = 0;
            pyrometerBtn.Opacity = 0.5;
            PyrometerPositionMainTop = idx;
          }
        }
        else if (grid == PerformanceSettingsGridLower)
        {
          if (PyrometerPositionMainBottom == idx)
          {
            pyrometerBtn.Opacity = 0;
            PyrometerPositionMainBottom = -1;
          }
          else
          {
            foreach (var child in grid.Children)
              if (child is Button btn && btn != pyrometerBtn)
                btn.Opacity = 0;
            pyrometerBtn.Opacity = 0.5;
            PyrometerPositionMainBottom = idx;
          }
        }
        else if (grid == ReinforcementGrid)
        {
          if (PyrometerPositionReinforcement == idx)
          {
            pyrometerBtn.Opacity = 0;
            PyrometerPositionReinforcement = -1;
          }
          else
          {
            foreach (var child in grid.Children)
              if (child is Button btn && btn != pyrometerBtn)
                btn.Opacity = 0;
            pyrometerBtn.Opacity = 0.5;
            PyrometerPositionReinforcement = idx;
          }
        }
        e.Handled = true;
      };

      // Left click on the button focuses the corresponding TextBox
      pyrometerBtn.PreviewMouseLeftButtonDown += (s, e) =>
      {
        // Find the parent grid cell
        var btn = (Button)s;
        var parent = VisualTreeHelper.GetParent(btn) as Grid;
        if (parent != null)
        {
          int row = Grid.GetRow(btn);
          int col = Grid.GetColumn(btn);
          // Find the TextBox in the same cell
          foreach (var child in parent.Children)
          {
            if (child is TextBox tb && Grid.GetRow(tb) == row && (Grid.GetColumn(tb) == col || Grid.GetColumn(tb) == col - 1))
            {
              tb.Focus();
              tb.SelectAll();
              break;
            }
          }
        }
        e.Handled = true;
      };

      grid.Children.Add(pyrometerBtn);
    }


    /*
     * SelectFileChannels1To10_Click / SelectFileChannels11To20_Click
     * Opens a file dialog to select a GBD file for channels 1-10 or 11-20.
     */
    private void SelectFileChannels1To10_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog openFileDialog = new OpenFileDialog
      {
        Filter = "GBD Files (*.GBD)|*.GBD|All Files (*.*)|*.*"
      };

      if (openFileDialog.ShowDialog() == true)
      {
        Channels1To10FilePath = openFileDialog.FileName;
        Channels1To10FilePathText.Text = $"{Path.GetFileName(Channels1To10FilePath)}"; // Show only the filename
      }
    }

    private void SelectFileChannels11To20_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog openFileDialog = new OpenFileDialog
      {
        Filter = "GBD Files (*.GBD)|*.GBD|All Files (*.*)|*.*"
      };

      if (openFileDialog.ShowDialog() == true)
      {
        Channels11To20FilePath = openFileDialog.FileName;
        Channels11To20FilePathText.Text = $"{Path.GetFileName(Channels11To20FilePath)}"; // Show only the filename
      }
    }

    /*
     * CollectPerformanceSettings
     * Collects all performance settings from the input grids into a list.
     */
    private void CollectPerformanceSettings()
    {
      PerformanceSettings.Clear();

      // List of all grids to iterate through
      var grids = new List<Panel> { PerformanceSettingsGridUpper, PerformanceSettingsGridLower, ReinforcementGrid };

      // Iterate through each grid and collect values
      foreach (var grid in grids)
      {
        foreach (var child in grid.Children)
        {
          if (child is TextBox textBox && textBox.Visibility == Visibility.Visible)
          {
            if (int.TryParse(textBox.Text, out int value))
            {
              PerformanceSettings.Add(value);
            }
            else
            {
              PerformanceSettings.Add(0); // Default to 0 for invalid inputs
            }
          }
        }
      }
    }

    /*
     * FetchLastUsed_Click
     * Loads the last used performance settings and pyrometer positions from the database for the current model.
     * Updates UI fields and highlights pyrometer buttons.
     */
    private void FetchLastUsed_Click(object sender, RoutedEventArgs e)
    {
      if (Settings.CurrentDxfModel == null)
      {
        MessageBox.Show("Please select or create a DXF model before importing measurements.", "No Model Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      var dbController = new DatabaseController();

      // Fetch last used values for each canvas type
      var lastMainTopSettings = dbController.GetLastUsedPerformanceSettings("MainTop", Settings.CurrentDxfModel.Id);
      var lastMainBottomSettings = dbController.GetLastUsedPerformanceSettings("MainBottom", Settings.CurrentDxfModel.Id);
      var lastReinfTopSettings = dbController.GetLastUsedPerformanceSettings("ReinfTop", Settings.CurrentDxfModel.Id);

      // Fetch last used pyrometer positions and numbers
      var (lastMainTopPosition, lastMainTopNumber) = dbController.GetLastUsedPyrometerPosition("MainTop", Settings.CurrentDxfModel.Id);
      var (lastMainBottomPosition, lastMainBottomNumber) = dbController.GetLastUsedPyrometerPosition("MainBottom", Settings.CurrentDxfModel.Id);
      var (lastReinfTopPosition, lastReinfTopNumber) = dbController.GetLastUsedPyrometerPosition("ReinfTop", Settings.CurrentDxfModel.Id);

      PyrometerPositionMainTop = lastMainTopPosition;
      PyrometerPositionMainBottom = lastMainBottomPosition;
      PyrometerPositionReinforcement = lastReinfTopPosition;

      // Populate pyrometer number input boxes
      PyrometerNumberUpperTextBox.Text = lastMainTopNumber != -1 ? lastMainTopNumber.ToString() : "";
      PyrometerNumberLowerTextBox.Text = lastMainBottomNumber != -1 ? lastMainBottomNumber.ToString() : "";
      PyrometerNumberReinforcementTextBox.Text = lastReinfTopNumber != -1 ? lastReinfTopNumber.ToString() : "";

      // Populate the upper grid (MainTop)
      int index = 0;
      foreach (var child in PerformanceSettingsGridUpper.Children)
      {
        if (child is TextBox textBox && index < lastMainTopSettings.Count)
        {
          textBox.Text = lastMainTopSettings[index].ToString();
          index++;
        }
      }

      // Populate the lower grid (MainBottom)
      index = 0;
      foreach (var child in PerformanceSettingsGridLower.Children)
      {
        if (child is TextBox textBox && index < lastMainBottomSettings.Count)
        {
          textBox.Text = lastMainBottomSettings[index].ToString();
          index++;
        }
      }

      // Populate the reinforcement grid (ReinfTop)
      index = 0;
      foreach (var child in ReinforcementGrid.Children)
      {
        if (child is TextBox textBox && index < lastReinfTopSettings.Count && textBox.Visibility == Visibility.Visible)
        {
          textBox.Text = lastReinfTopSettings[index].ToString();
          index++;
        }
      }

      // Mark pyrometer positions in the UI and assign to current import
      MarkPyrometerButton(PerformanceSettingsGridUpper, PyrometerPositionMainTop);
      MarkPyrometerButton(PerformanceSettingsGridLower, PyrometerPositionMainBottom);
      MarkPyrometerButton(ReinforcementGrid, PyrometerPositionReinforcement);


      // Optionally show a message if no previous settings were found
      if (lastMainTopSettings.Count == 0 && lastMainBottomSettings.Count == 0 && lastReinfTopSettings.Count == 0)
      {
        MessageBox.Show("No previous performance settings found for this model.", "No Data Found", MessageBoxButton.OK, MessageBoxImage.Information);
      }
    }

    /*
     * MarkPyrometerButton
     * Highlights the selected pyrometer button in the grid.
     */
    private void MarkPyrometerButton(Grid grid, int pyrometerPosition)
    {
      foreach (var child in grid.Children)
      {
        if (child is Button btn && btn.Tag is int tag)
        {
          btn.Opacity = (tag == pyrometerPosition && pyrometerPosition != -1) ? 0.5 : 0;
        }
      }
    }

    /*
     * StartImport_Click
     * Validates input, deletes previous measurements/assignments, imports new measurements, and creates new canvas assignments.
     * Updates application state and triggers redraws.
     */
    private void StartImport_Click(object sender, RoutedEventArgs e)
    {
      if (Settings.CurrentDxfModel == null)
      {
        MessageBox.Show("Please select or create a DXF model before importing measurements.", "No Model Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      if (string.IsNullOrWhiteSpace(Channels1To10FilePath) && string.IsNullOrWhiteSpace(Channels11To20FilePath))
      {
        MessageBox.Show("Please select at least one GBD file to import measurements from.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      if (Settings.Measurements11To20 != null || Settings.Measurements1To10 != null)
      {
        var result = MessageBox.Show("Importing new measurements will overwrite existing ones. Do you want to continue?", "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
          DialogResult = false;
          return; // Cancel import
        }
      }

      // Delete existing measurements and assignments for overwrite
      if (Settings.Measurements1To10 != null)
        dbController.DeleteMeasurementById(Settings.Measurements1To10.Id);
      if (Settings.Measurements11To20 != null)
        dbController.DeleteMeasurementById(Settings.Measurements11To20.Id);

      if (Settings.CanvasAssignments != null)
      {
        foreach (var assignment in Settings.CanvasAssignments.Values)
          dbController.DeleteCanvasAssignmentById(assignment.Id);
      }

      var gbdImportController = new GbdImportController(dbController);

      if (!string.IsNullOrWhiteSpace(Channels1To10FilePath))
      {
        Settings.Measurements1To10 = gbdImportController.ImportGbdFile(Channels1To10FilePath, "1To10");
        // Settings.Measurements1To10 is now set by the importer
      }

      if (!string.IsNullOrWhiteSpace(Channels11To20FilePath))
      {
        Settings.Measurements11To20 = gbdImportController.ImportGbdFile(Channels11To20FilePath, "11To20");
        // Settings.Measurements11To20 is now set by the importer
      }

      // 1. Deserialize measurement entries for easier access later
      if (Settings.Measurements1To10 != null)
      {
        Settings.Measurements1To10.CachedEntries =
            System.Text.Json.JsonSerializer.Deserialize<List<Services.TemperatureMappingService.MeasurementEntry>>(
                Settings.Measurements1To10.Data);
      }
      if (Settings.Measurements11To20 != null)
      {
        Settings.Measurements11To20.CachedEntries =
            System.Text.Json.JsonSerializer.Deserialize<List<Services.TemperatureMappingService.MeasurementEntry>>(
                Settings.Measurements11To20.Data);
      }

      // 2. Prepare performance settings for each canvas
      CollectPerformanceSettings();
      var mainTopPerformance = PerformanceSettings.Take(80).ToList();
      var mainBottomPerformance = PerformanceSettings.Skip(80).Take(80).ToList();
      var reinfTopPerformance = PerformanceSettings.Skip(160).Take(32).ToList();
      var reinfBottomPerformance = new List<int>();

      // 3. Get thermoelements for each canvas and activate them
      foreach (var kvp in Settings.ThermoelementsByCanvas)
      {
        foreach (var te in kvp.Value)
        {
          te.IsActive = true;
          dbController.UpdateThermoelementActiveState(te.Id, te.IsActive);
        }
      }
      var mainTopThermo = Settings.ThermoelementsByCanvas.ContainsKey("MainTop") ? Settings.ThermoelementsByCanvas["MainTop"] : new List<Thermoelement>();
      var mainBottomThermo = Settings.ThermoelementsByCanvas.ContainsKey("MainBottom") ? Settings.ThermoelementsByCanvas["MainBottom"] : new List<Thermoelement>();
      var reinfTopThermo = Settings.ThermoelementsByCanvas.ContainsKey("ReinfTop") ? Settings.ThermoelementsByCanvas["ReinfTop"] : new List<Thermoelement>();
      var reinfBottomThermo = Settings.ThermoelementsByCanvas.ContainsKey("ReinfBottom") ? Settings.ThermoelementsByCanvas["ReinfBottom"] : new List<Thermoelement>();

      // 4. Create and insert CanvasAssignments
      var assignments = new Dictionary<string, CanvasAssignment>();

      assignments["MainTop"] = new CanvasAssignment
      {
        DXFModelId = Settings.CurrentDxfModel.Id,
        Type = "MainTop",
        Measurement1To10Id = Settings.Measurements1To10?.Id ?? -1,
        Measurement11To20Id = Settings.Measurements11To20?.Id ?? -1,
        PyrometerPosition = PyrometerPositionMainTop,
        PyrometerNumber = PyrometerNumberUpper,
        PerformanceSettings = mainTopPerformance,
        Thermoelements = mainTopThermo.Select(te => te.Id).ToList()
      };
      assignments["MainTop"].Id = dbController.InsertCanvasAssignment(assignments["MainTop"]);

      assignments["MainBottom"] = new CanvasAssignment
      {
        DXFModelId = Settings.CurrentDxfModel.Id,
        Type = "MainBottom",
        Measurement1To10Id = Settings.Measurements1To10?.Id ?? -1,
        Measurement11To20Id = Settings.Measurements11To20?.Id ?? -1,
        PyrometerPosition = PyrometerPositionMainBottom,
        PyrometerNumber = PyrometerNumberLower,
        PerformanceSettings = mainBottomPerformance,
        Thermoelements = mainBottomThermo.Select(te => te.Id).ToList()
      };
      assignments["MainBottom"].Id = dbController.InsertCanvasAssignment(assignments["MainBottom"]);

      assignments["ReinfTop"] = new CanvasAssignment
      {
        DXFModelId = Settings.CurrentDxfModel.Id,
        Type = "ReinfTop",
        Measurement1To10Id = Settings.Measurements1To10?.Id ?? -1,
        Measurement11To20Id = Settings.Measurements11To20?.Id ?? -1,
        PyrometerPosition = PyrometerPositionReinforcement,
        PyrometerNumber = PyrometerNumberReinforcement,
        PerformanceSettings = reinfTopPerformance,
        Thermoelements = reinfTopThermo.Select(te => te.Id).ToList()
      };
      assignments["ReinfTop"].Id = dbController.InsertCanvasAssignment(assignments["ReinfTop"]);

      assignments["ReinfBottom"] = new CanvasAssignment
      {
        DXFModelId = Settings.CurrentDxfModel.Id,
        Type = "ReinfBottom",
        Measurement1To10Id = Settings.Measurements1To10?.Id ?? -1,
        Measurement11To20Id = Settings.Measurements11To20?.Id ?? -1,
        PyrometerPosition = -1,
        PyrometerNumber = -1,
        PerformanceSettings = reinfBottomPerformance,
        Thermoelements = reinfBottomThermo.Select(te => te.Id).ToList()
      };
      assignments["ReinfBottom"].Id = dbController.InsertCanvasAssignment(assignments["ReinfBottom"]);

      // 5. Store assignments in Settings
      Settings.CanvasAssignments = assignments;

      // Redraw the bottom toolbar to reflect new measurements
      (Application.Current.MainWindow as MainWindow)?.BottomToolbar?.UpdateTimelineUI();

      // Redraw the main window to reflect new measurements and assignments
      (Application.Current.MainWindow as MainWindow)?.RedrawAll();

      DialogResult = true;
    }

    /*
     * OnPropertyChanged
     * Notifies the UI of property changes for data binding.
     */
    protected void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /*
     * OnClosing
     * Handles window closing event, treating close as cancel if no result is set.
     */
    protected override void OnClosing(CancelEventArgs e)
    {
      if (!DialogResult.HasValue)
      {
        DialogResult = false; // Treat closing the window as a cancel action
      }

      base.OnClosing(e);
    }
  }
}
