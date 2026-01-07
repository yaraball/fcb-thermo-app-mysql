using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using fcb_thermo_app.Models;
using fcb_thermo_app.Services;
using fcb_thermo_app.Controllers;
using System.Text.Json;

namespace fcb_thermo_app.Views
{
  /*
   * ThermoelementPlacementWindow
   * Full-screen window for placing thermoelements on main body and reinforcement canvases.
   * Handles placement logic, editing, saving/loading layouts, and UI updates.
   */
  public partial class ThermoelementPlacementWindow : Window
  {
    private readonly DrawingService _drawingService = new();
    private readonly DatabaseController _dbController = new();

    // Track which view is active
    private bool _isReinforcementMode = false; // false = MainBody, true = Reinforcement

    // For placement logic
    private int _totalThermoelementsToPlace;
    private int _thermoelementsPlaced;
    private bool _isPlacing = false;
    private PlacedThermoelement? _selectedThermoelement;
    private bool _isEditingMode = false;

    // For storing points per canvas
    private readonly List<PlacedThermoelement> _mainTopThermoelements = new();
    private readonly List<PlacedThermoelement> _mainBottomThermoelements = new();
    private readonly List<PlacedThermoelement> _reinfTopThermoelements = new();
    private readonly List<PlacedThermoelement> _reinfBottomThermoelements = new();

    /*
     * ThermoelementPlacementWindow()
     * Initializes the window, loads DXF models, and sets up event handlers.
     */
    public ThermoelementPlacementWindow()
    {
      try
      {
        InitializeComponent();
        Loaded += OnWindowLoaded;
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error initializing window: {ex.Message}", "Error",
                      MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /*
     * OnWindowLoaded
     * Loads DXF models and updates the UI when the window is loaded.
     */
    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
      try
      {
        LoadDxfModels();
        UpdateUI();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error loading window: {ex.Message}", "Error",
                      MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /*
     * ShowActiveCanvasPanel
     * Shows or hides main body or reinforcement canvas panels based on the current mode.
     */
    private void ShowActiveCanvasPanel()
    {
      if (_isReinforcementMode)
      {
        MainBodyCanvasesPanel.Visibility = Visibility.Hidden;
        ReinforcementCanvasesPanel.Visibility = Visibility.Visible;

        // Copy sizes from main canvases if available
        if (MainTopCanvas.ActualWidth > 0 && MainTopCanvas.ActualHeight > 0)
        {
          ReinfTopCanvas.Width = MainTopCanvas.ActualWidth;
          ReinfTopCanvas.Height = MainTopCanvas.ActualHeight;
        }
        if (MainBottomCanvas.ActualWidth > 0 && MainBottomCanvas.ActualHeight > 0)
        {
          ReinfBottomCanvas.Width = MainBottomCanvas.ActualWidth;
          ReinfBottomCanvas.Height = MainBottomCanvas.ActualHeight;
        }
      }
      else
      {
        MainBodyCanvasesPanel.Visibility = Visibility.Visible;
        ReinforcementCanvasesPanel.Visibility = Visibility.Hidden;

        // Copy sizes from reinforcement canvases if available
        if (ReinfTopCanvas.ActualWidth > 0 && ReinfTopCanvas.ActualHeight > 0)
        {
          MainTopCanvas.Width = ReinfTopCanvas.ActualWidth;
          MainTopCanvas.Height = ReinfTopCanvas.ActualHeight;
        }
        if (ReinfBottomCanvas.ActualWidth > 0 && ReinfBottomCanvas.ActualHeight > 0)
        {
          MainBottomCanvas.Width = ReinfBottomCanvas.ActualWidth;
          MainBottomCanvas.Height = ReinfBottomCanvas.ActualHeight;
        }
      }
    }

    /*
     * LoadDxfModels
     * Draws the DXF models on the appropriate canvases for placement.
     */
    private void LoadDxfModels()
    {

      ShowActiveCanvasPanel();

      try
      {
        if (_isReinforcementMode)
        {
          _drawingService.DrawView(ReinfTopCanvas, "ReinfTop", new List<Thermoelement>(), ReinfTopCanvas.ActualWidth, ReinfTopCanvas.ActualHeight);
          _drawingService.DrawView(ReinfBottomCanvas, "ReinfBottom", new List<Thermoelement>(), ReinfBottomCanvas.ActualWidth, ReinfBottomCanvas.ActualHeight);
        }
        else
        {
          _drawingService.DrawView(MainTopCanvas, "MainTop", new List<Thermoelement>(), MainTopCanvas.ActualWidth, MainTopCanvas.ActualHeight);
          _drawingService.DrawView(MainBottomCanvas, "MainBottom", new List<Thermoelement>(), MainBottomCanvas.ActualWidth, MainBottomCanvas.ActualHeight);
        }
        StatusText.Text = "DXF models loaded. Enter thermoelement count to start placement.";
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error loading DXF models: {ex.Message}", "Error",
                      MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /*
     * Canvas_MouseMove
     * Updates the mouse coordinates display as the user moves the mouse over a canvas.
     */
    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
      try
      {
        var canvas = sender as Canvas;
        if (canvas == null || canvas.ActualWidth <= 0 || canvas.ActualHeight <= 0) return;

        var position = e.GetPosition(canvas);
        var (relativeX, relativeY) = CoordinateConverter.ConvertCanvasToRelative(
            position.X, position.Y, canvas.ActualWidth, canvas.ActualHeight);
        var (modelX, modelY) = CoordinateConverter.ConvertRelativeToModel(relativeX, relativeY);

        MouseCoordText.Text = $"X: {modelX:F3}m, Y: {modelY:F3}m | Rel: ({relativeX:F3}, {relativeY:F3})";
      }
      catch { }
    }

    /*
     * Canvas_MouseLeftButtonDown
     * Handles left-clicks on the canvas for placing or editing thermoelements.
     */
    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      try
      {
        var canvas = sender as Canvas;
        if (canvas == null) return;

        var position = e.GetPosition(canvas);

        if (_isEditingMode && _selectedThermoelement != null)
        {
          ReplaceSelectedPoint(canvas, position);
        }
        else if (_isPlacing && _thermoelementsPlaced < _totalThermoelementsToPlace)
        {
          PlacePoint(canvas, position);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error placing thermoelement: {ex.Message}", "Error",
                      MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /*
     * GetCanvasType
     * Returns the canvas type string for a given Canvas control.
     */
    private string GetCanvasType(Canvas canvas)
    {
      if (canvas == MainTopCanvas) return "MainTop";
      if (canvas == MainBottomCanvas) return "MainBottom";
      if (canvas == ReinfTopCanvas) return "ReinfTop";
      if (canvas == ReinfBottomCanvas) return "ReinfBottom";
      throw new InvalidOperationException("Unknown canvas");
    }

    /*
     * PlacePoint
     * Places a new thermoelement at the clicked position on the canvas.
     * Assigns the next available channel and updates the UI.
     */
    private void PlacePoint(Canvas canvas, Point position)
    {
      int nextChannel = GetNextAvailableChannel();

      var (relativeX, relativeY) = CoordinateConverter.ConvertCanvasToRelative(
          position.X, position.Y, canvas.ActualWidth, canvas.ActualHeight);
      var (modelX, modelY) = CoordinateConverter.ConvertRelativeToModel(relativeX, relativeY);

      var thermoelement = new PlacedThermoelement
      {
        Channel = nextChannel,
        CanvasX = position.X,
        CanvasY = position.Y,
        ModelX = modelX,
        ModelY = modelY,
        RelativeX = relativeX,
        RelativeY = relativeY,
        CanvasType = GetCanvasType(canvas)
      };
      _selectedThermoelement = thermoelement;

      GetPointListForCanvasType(GetCanvasType(canvas)).Add(thermoelement);

      _thermoelementsPlaced++;
      DrawThermoelementOnCanvas(canvas, thermoelement);
      UpdateUI();
      ClearSelection();
      UpdateSelectedPointUI();

      if (_thermoelementsPlaced >= _totalThermoelementsToPlace)
      {
        CompletePlacement();
      }
    }

    /*
     * ReplaceSelectedPoint
     * Replaces the selected thermoelement's position with a new one on the canvas.
     */
    private void ReplaceSelectedPoint(Canvas canvas, Point position)
    {
      if (_selectedThermoelement == null) return;

      int originalChannel = _selectedThermoelement.Channel;
      var list = GetPointListForCanvasType(_selectedThermoelement.CanvasType);
      list.Remove(_selectedThermoelement);

      var (relativeX, relativeY) = CoordinateConverter.ConvertCanvasToRelative(
          position.X, position.Y, canvas.ActualWidth, canvas.ActualHeight);
      var (modelX, modelY) = CoordinateConverter.ConvertRelativeToModel(relativeX, relativeY);

      var newPoint = new PlacedThermoelement
      {
        Channel = originalChannel,
        CanvasX = position.X,
        CanvasY = position.Y,
        ModelX = modelX,
        ModelY = modelY,
        RelativeX = relativeX,
        RelativeY = relativeY,
        CanvasType = GetCanvasType(canvas)
      };

      GetPointListForCanvasType(GetCanvasType(canvas)).Add(newPoint);

      RedrawallThermoelements();
      _isEditingMode = false;
      _selectedThermoelement = newPoint;
      StatusText.Text = "Thermoelement replaced. Placement continues.";
      UpdateUI();
    }

    /*
     * GetNextAvailableChannel
     * Determines the next available channel number for placement.
     */
    private int GetNextAvailableChannel()
    {
      var allThermoelements = _mainTopThermoelements.Concat(_mainBottomThermoelements)
        .Concat(_reinfTopThermoelements).Concat(_reinfBottomThermoelements);

      if (!allThermoelements.Any())
        return 1;

      int maxChannel = allThermoelements.Max(p => p.Channel);
      for (int i = 1; i <= maxChannel + 1; i++)
      {
        if (!allThermoelements.Any(p => p.Channel == i))
          return i;
      }
      return maxChannel + 1;
    }

    /*
     * DrawThermoelementOnCanvas
     * Draws a thermoelement marker and label on the specified canvas.
     */
    private void DrawThermoelementOnCanvas(Canvas canvas, PlacedThermoelement thermoelement)
    {
      var container = new Canvas();

      double pointSize = 16.0;
      double scaleFactor = 300.0 / 600.0;
      double scaledPointSize = pointSize * scaleFactor;

      var ellipse = new Ellipse
      {
        Width = scaledPointSize,
        Height = scaledPointSize,
        Fill = (thermoelement == _selectedThermoelement)
          ? (Brush)Application.Current.Resources["PinkBrush"]
          : (Brush)Application.Current.Resources["BlackBrush"],
      };
      Canvas.SetLeft(ellipse, thermoelement.CanvasX - scaledPointSize / 2);
      Canvas.SetTop(ellipse, thermoelement.CanvasY - scaledPointSize / 2);
      container.Children.Add(ellipse);

      var label = new TextBlock
      {
        Text = thermoelement.Channel.ToString(),
        Foreground = Brushes.White,
        FontSize = 8 * scaleFactor,
        Background = Brushes.Transparent,
        Padding = new Thickness(2),
      };
      int channelLength = thermoelement.Channel.ToString().Length;
      double leftOffset = thermoelement.CanvasX - scaledPointSize / 2 + (scaledPointSize - 8 * scaleFactor) / 4;
      if (channelLength >= 2)
        leftOffset -= (scaledPointSize - 8 * scaleFactor) / 3;
      Canvas.SetLeft(label, leftOffset);
      Canvas.SetTop(label, thermoelement.CanvasY - scaledPointSize / 2 - (scaledPointSize - 8 * scaleFactor) / 4);
      container.Children.Add(label);

      container.Tag = thermoelement;
      container.MouseLeftButtonDown += PointContainer_MouseLeftButtonDown;

      canvas.Children.Add(container);
    }

    /*
     * PointContainer_MouseLeftButtonDown
     * Handles selection of a thermoelement marker for editing.
     */
    private void PointContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      var container = (Canvas)sender;
      _selectedThermoelement = (PlacedThermoelement)container.Tag;

      ClearSelection();
      var ellipse = (Ellipse)container.Children[0];
      ellipse.Fill = (Brush)Application.Current.Resources["PinkBrush"];

      UpdateSelectedPointUI();
      e.Handled = true;
    }

    /*
     * ClearSelection
     * Clears the selection highlight from all thermoelement markers.
     */
    private void ClearSelection()
    {
      foreach (var canvas in GetAllCanvases())
      {
        foreach (var child in canvas.Children)
        {
          if (child is Canvas container && container.Tag is PlacedThermoelement te)
          {
            var ellipse = (Ellipse)container.Children[0];
            ellipse.Fill = (te == _selectedThermoelement)
                     ? (Brush)Application.Current.Resources["PinkBrush"]
                     : (Brush)Application.Current.Resources["BlackBrush"];
          }
        }
      }
    }

    /*
     * UpdateSelectedPointUI
     * Updates the UI to reflect the currently selected thermoelement.
     */
    private void UpdateSelectedPointUI()
    {
      if (_selectedThermoelement != null)
      {
        CurrentChannelText.Text = _selectedThermoelement.Channel.ToString();
        CurrentPositionText.Text = $"X: {_selectedThermoelement.ModelX:F3}m, Y: {_selectedThermoelement.ModelY:F3}m";
        EditThermoelementButton.IsEnabled = true;

        if (_isEditingMode)
        {
          StatusText.Text = $"Editing Mode: Click new position for thermoelement {_selectedThermoelement.Channel}";
        }
      }
      else
      {
        CurrentChannelText.Text = "—";
        CurrentPositionText.Text = "—";
        EditThermoelementButton.IsEnabled = false;
      }
    }

    /*
     * RedrawallThermoelements
     * Clears and redraws all thermoelements on all canvases.
     */
    private void RedrawallThermoelements()
    {
      // Clear all canvases
      foreach (var canvas in GetAllCanvases())
        canvas.Children.Clear();

      // Redraw DXF models
      LoadDxfModels();

      // Redraw thermoelements for each canvas
      foreach (var thermoelement in _mainTopThermoelements)
        DrawThermoelementOnCanvas(MainTopCanvas, thermoelement);
      foreach (var thermoelement in _mainBottomThermoelements)
        DrawThermoelementOnCanvas(MainBottomCanvas, thermoelement);
      foreach (var thermoelement in _reinfTopThermoelements)
        DrawThermoelementOnCanvas(ReinfTopCanvas, thermoelement);
      foreach (var thermoelement in _reinfBottomThermoelements)
        DrawThermoelementOnCanvas(ReinfBottomCanvas, thermoelement);
    }

    /*
     * StartButton_Click
     * Starts the placement process for the specified number of thermoelements.
     */
    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
      if (!int.TryParse(ThermoelementCountTextBox.Text, out _totalThermoelementsToPlace) || _totalThermoelementsToPlace <= 0)
      {
        MessageBox.Show("Please enter a valid number of thermoelements to place.", "Invalid Input",
                      MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      _isPlacing = true;
      _thermoelementsPlaced = 0;
      _mainTopThermoelements.Clear();
      _mainBottomThermoelements.Clear();
      _reinfTopThermoelements.Clear();
      _reinfBottomThermoelements.Clear();
      _isEditingMode = false;
      _selectedThermoelement = null;

      RedrawallThermoelements();
      UpdateUI();
      StatusText.Text = $"Placement started - Click on DXF models to place {_totalThermoelementsToPlace} thermoelements.";
    }

    /*
     * CompletePlacement
     * Marks placement as complete and enables confirmation.
     */
    private void CompletePlacement()
    {
      _isPlacing = false;
      ConfirmButton.IsEnabled = true;
      StatusText.Text = "Placement completed - Please confirm or reset";

      MessageBox.Show($"All {_totalThermoelementsToPlace} thermoelements have been placed. Please review and confirm.",
                    "Placement Complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /*
     * EditThermoelementButton_Click
     * Enables or cancels editing mode for the selected thermoelement.
     */
    private void EditThermoelementButton_Click(object sender, RoutedEventArgs e)
    {
      if (_selectedThermoelement == null) return;

      if (!_isEditingMode)
      {
        _isEditingMode = true;
        StatusText.Text = $"Editing Mode: Click new position for thermoelement {_selectedThermoelement.Channel}";
      }
      else
      {
        _isEditingMode = false;
        StatusText.Text = "Editing cancelled. Placement continues.";
        _selectedThermoelement = null;
        ClearSelection();
      }

      UpdateUI();
    }

    /*
     * ChangeChannelButton_Click
     * Opens a dialog to change the channel number of the selected thermoelement.
     */
    private void ChangeChannelButton_Click(object sender, RoutedEventArgs e)
    {
      if (_selectedThermoelement == null)
      {
        MessageBox.Show("Please select a thermoelement first.", "No Thermoelement Selected",
                      MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }

      var inputBox = new Window
      {
        Title = "Change Thermoelement Channel",
        Width = 300,
        Height = 150,
        WindowStyle = WindowStyle.ToolWindow,
        ResizeMode = ResizeMode.NoResize,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        Owner = this
      };

      var grid = new Grid { Margin = new Thickness(10) };
      grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

      var textBlock = new TextBlock
      {
        Text = "Enter new Channel:",
        Margin = new Thickness(0, 0, 0, 10),
        Foreground = Brushes.White
      };
      Grid.SetRow(textBlock, 0);

      var textBox = new TextBox
      {
        Text = _selectedThermoelement.Channel.ToString(),
        FontSize = 14,
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Thickness(0, 0, 0, 10)
      };
      Grid.SetRow(textBox, 1);

      var stackPanel = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Center
      };
      Grid.SetRow(stackPanel, 2);

      var okButton = new Button
      {
        Content = "OK",
        Width = 75,
        Margin = new Thickness(0, 0, 10, 0)
      };

      var cancelButton = new Button
      {
        Content = "Cancel",
        Width = 75
      };

      okButton.Click += (s, args) =>
      {
        if (int.TryParse(textBox.Text, out int newChannel) && newChannel > 0)
        {
          _selectedThermoelement.Channel = newChannel;
          RedrawallThermoelements();
          UpdateSelectedPointUI();
          UpdateUI();
          StatusText.Text = $"Channel changed to {newChannel}";
          inputBox.DialogResult = true;
        }
        else
        {
          MessageBox.Show("Please enter a valid positive number.", "Invalid Input",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
        }
      };

      cancelButton.Click += (s, args) =>
      {
        inputBox.DialogResult = false;
      };

      stackPanel.Children.Add(okButton);
      stackPanel.Children.Add(cancelButton);

      grid.Children.Add(textBlock);
      grid.Children.Add(textBox);
      grid.Children.Add(stackPanel);

      inputBox.Content = grid;
      textBox.Focus();
      textBox.SelectAll();

      inputBox.ShowDialog();
    }

    /*
     * SaveLayoutButton_Click
     * Opens a dialog to save the current thermoelement layout to the database.
     */
    private void SaveLayoutButton_Click(object sender, RoutedEventArgs e)
    {
      var allThermoelements = _mainTopThermoelements.Concat(_mainBottomThermoelements)
        .Concat(_reinfTopThermoelements).Concat(_reinfBottomThermoelements).ToList();

      if (allThermoelements.Count == 0)
      {
        MessageBox.Show("No thermoelements to save. Please place some thermoelements first.", "No Thermoelements",
                      MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }

      var inputDialog = new Window
      {
        Title = "Save Layout",
        Width = 420,
        Height = 270,
        WindowStyle = WindowStyle.ToolWindow,
        ResizeMode = ResizeMode.NoResize,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        Owner = this,
        Background = new SolidColorBrush(Color.FromRgb(22, 27, 34))
      };

      var mainPanel = new StackPanel
      {
        Orientation = Orientation.Vertical,
        Margin = new Thickness(20),
        VerticalAlignment = VerticalAlignment.Stretch
      };

      var nameLabel = new TextBlock
      {
        Text = "Layout Name:",
        Foreground = Brushes.White,
        Margin = new Thickness(0, 0, 0, 5)
      };
      mainPanel.Children.Add(nameLabel);

      var nameBox = new TextBox
      {
        FontSize = 14,
        Padding = new Thickness(5),
        Margin = new Thickness(0, 0, 0, 10)
      };
      mainPanel.Children.Add(nameBox);

      var descLabel = new TextBlock
      {
        Text = "Description (optional):",
        Foreground = Brushes.White,
        Margin = new Thickness(0, 0, 0, 5)
      };
      mainPanel.Children.Add(descLabel);

      var descBox = new TextBox
      {
        FontSize = 14,
        Padding = new Thickness(5),
        Margin = new Thickness(0, 0, 0, 15),
        Height = 60,
        TextWrapping = TextWrapping.Wrap,
        AcceptsReturn = true
      };
      mainPanel.Children.Add(descBox);

      var buttonPanel = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = new Thickness(0, 10, 0, 0)
      };

      var saveBtn = new Button
      {
        Content = "Save",
        Width = 80,
        Height = 30,
        Margin = new Thickness(0, 0, 10, 0),
        Background = new SolidColorBrush(Color.FromRgb(31, 111, 235)),
        Foreground = Brushes.White,
        BorderThickness = new Thickness(0),
        Cursor = Cursors.Hand
      };

      var cancelBtn = new Button
      {
        Content = "Cancel",
        Width = 80,
        Height = 30,
        Background = new SolidColorBrush(Color.FromRgb(88, 96, 105)),
        Foreground = Brushes.White,
        BorderThickness = new Thickness(0),
        Cursor = Cursors.Hand
      };

      saveBtn.Click += (s, args) =>
      {
        string layoutName = nameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(layoutName))
        {
          MessageBox.Show("Please enter a layout name.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        var existing = _dbController.GetPointLayoutByName(layoutName);
        if (existing != null)
        {
          var overwrite = MessageBox.Show($"A layout named '{layoutName}' already exists. Overwrite it?",
                                               "Layout Exists", MessageBoxButton.YesNo, MessageBoxImage.Question);
          if (overwrite != MessageBoxResult.Yes)
            return;

          _dbController.DeletePointLayout(existing.Id);
        }

        string pointsJson = JsonSerializer.Serialize(allThermoelements);

        var layout = new PointLayout
        {
          Name = layoutName,
          Description = descBox.Text.Trim(),
          PointsData = pointsJson,
          CreatedAt = DateTime.Now
        };

        _dbController.InsertPointLayout(layout);
        MessageBox.Show($"Layout '{layoutName}' saved successfully!", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        inputDialog.DialogResult = true;
      };

      cancelBtn.Click += (s, args) => inputDialog.DialogResult = false;

      buttonPanel.Children.Add(saveBtn);
      buttonPanel.Children.Add(cancelBtn);
      mainPanel.Children.Add(buttonPanel);

      inputDialog.Content = mainPanel;

      if (inputDialog.ShowDialog() == true)
      {
        StatusText.Text = $"Layout saved successfully!";
      }
    }

    /*
     * LoadLayoutButton_Click
     * Opens a dialog to load a saved thermoelement layout from the database.
     */
    private void LoadLayoutButton_Click(object sender, RoutedEventArgs e)
    {
      var layouts = _dbController.GetAllPointLayouts();
      if (layouts.Count == 0)
      {
        MessageBox.Show("No saved layouts found.", "No Layouts",
                      MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }

      var selectDialog = new Window
      {
        Title = "Load Layout",
        Width = 500,
        Height = 400,
        WindowStyle = WindowStyle.ToolWindow,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        Owner = this,
        Background = new SolidColorBrush(Color.FromRgb(22, 27, 34))
      };

      var grid = new Grid { Margin = new Thickness(20) };
      grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
      grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

      var listBox = new ListBox
      {
        Margin = new Thickness(0, 0, 0, 10)
      };

      foreach (var layout in layouts)
      {
        var item = new ListBoxItem
        {
          Content = $"{layout.Name} - {layout.CreatedAt:g}" + (string.IsNullOrEmpty(layout.Description) ? "" : $"\n   {layout.Description}"),
          Tag = layout,
          Padding = new Thickness(10)
        };
        listBox.Items.Add(item);
      }

      Grid.SetRow(listBox, 0);

      var buttonPanel = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right
      };
      Grid.SetRow(buttonPanel, 1);

      var loadBtn = new Button
      {
        Content = "Load",
        Width = 80,
        Height = 30,
        Margin = new Thickness(0, 0, 10, 0)
      };

      var deleteBtn = new Button
      {
        Content = "Delete",
        Width = 80,
        Height = 30,
        Margin = new Thickness(0, 0, 10, 0)
      };

      var cancelBtn = new Button
      {
        Content = "Cancel",
        Width = 80,
        Height = 30
      };

      loadBtn.Click += (s, args) =>
      {
        if (listBox.SelectedItem is ListBoxItem selected && selected.Tag is PointLayout layout)
        {
          LoadLayout(layout);
          selectDialog.DialogResult = true;
        }
        else
        {
          MessageBox.Show("Please select a layout to load.", "No Selection",
                              MessageBoxButton.OK, MessageBoxImage.Information);
        }
      };

      deleteBtn.Click += (s, args) =>
      {
        if (listBox.SelectedItem is ListBoxItem selected && selected.Tag is PointLayout layout)
        {
          var confirm = MessageBox.Show($"Delete layout '{layout.Name}'?", "Confirm Delete",
                                            MessageBoxButton.YesNo, MessageBoxImage.Question);
          if (confirm == MessageBoxResult.Yes)
          {
            _dbController.DeletePointLayout(layout.Id);
            listBox.Items.Remove(selected);
            MessageBox.Show("Layout deleted successfully.", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
          }
        }
      };

      cancelBtn.Click += (s, args) => selectDialog.DialogResult = false;

      buttonPanel.Children.Add(loadBtn);
      buttonPanel.Children.Add(deleteBtn);
      buttonPanel.Children.Add(cancelBtn);

      grid.Children.Add(listBox);
      grid.Children.Add(buttonPanel);

      selectDialog.Content = grid;
      selectDialog.ShowDialog();
    }

    /*
     * LoadLayout
     * Loads a thermoelement layout from the database and updates the UI.
     */
    private void LoadLayout(PointLayout layout)
    {
      try
      {
        var points = JsonSerializer.Deserialize<List<PlacedThermoelement>>(layout.PointsData);
        if (points == null || points.Count == 0)
        {
          MessageBox.Show("Layout contains no points.", "Invalid Layout",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        _mainTopThermoelements.Clear();
        _mainBottomThermoelements.Clear();
        _reinfTopThermoelements.Clear();
        _reinfBottomThermoelements.Clear();

        foreach (var thermoelement in points)
        {
          if (thermoelement.CanvasType == "MainTop")
            _mainTopThermoelements.Add(thermoelement);
          else if (thermoelement.CanvasType == "MainBottom")
            _mainBottomThermoelements.Add(thermoelement);
          else if (thermoelement.CanvasType == "ReinfTop")
            _reinfTopThermoelements.Add(thermoelement);
          else if (thermoelement.CanvasType == "ReinfBottom")
            _reinfBottomThermoelements.Add(thermoelement);
        }

        _thermoelementsPlaced = points.Count;
        _totalThermoelementsToPlace = points.Count;
        _isPlacing = false;
        ConfirmButton.IsEnabled = true;

        RedrawallThermoelements();
        UpdateUI();
        StatusText.Text = $"Layout '{layout.Name}' loaded - {points.Count} points";
        MessageBox.Show($"Layout '{layout.Name}' loaded successfully!\n{points.Count} points placed.",
                      "Success", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error loading layout: {ex.Message}", "Error",
                      MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /*
     * ToggleModelButton_Click
     * Switches between main body and reinforcement placement modes.
     */
    private void ToggleModelButton_Click(object sender, RoutedEventArgs e)
    {
      _isReinforcementMode = !_isReinforcementMode;
      LoadDxfModels();
      RedrawallThermoelements();
      UpdateUI();
      StatusText.Text = _isReinforcementMode ? "Switched to Reinforcement model" : "Switched to MainBody model";
    }

    /*
     * ConfirmButton_Click
     * Validates placement, checks for duplicate channels, saves thermoelements to the database, and closes the window.
     */
    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        // Collect all points for duplicate channel check
        var allThermoelements = _mainTopThermoelements.Concat(_mainBottomThermoelements)
          .Concat(_reinfTopThermoelements).Concat(_reinfBottomThermoelements).ToList();

        if (_thermoelementsPlaced != _totalThermoelementsToPlace)
        {
          MessageBox.Show("Please place all points before confirming.", "Incomplete Placement",
                         MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        var duplicateGroups = allThermoelements
            .GroupBy(p => p.Channel)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateGroups.Any())
        {
          string duplicateMessage = "Duplicate indices found:\n\n";
          foreach (var group in duplicateGroups)
          {
            var locations = group.Select(p =>
              $"{p.CanvasType} at ({p.ModelX:F3}m, {p.ModelY:F3}m)");
            duplicateMessage += $"Channel {group.Key} used at:\n";
            duplicateMessage += string.Join("\n", locations.Select(l => $"  • {l}")) + "\n\n";
          }
          duplicateMessage += "Please fix duplicate channels before confirming.";
          MessageBox.Show(duplicateMessage, "Duplicate Channels Found",
                        MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        // Helper for adding and DB insert
        void AddAndInsert(List<PlacedThermoelement> points, string key)
        {
          foreach (var thermoelement in points.OrderBy(p => p.Channel))
          {
            var te = new Thermoelement
            {
              Id = 0,
              RelativeX = thermoelement.RelativeX,
              RelativeY = thermoelement.RelativeY,
              Channel = thermoelement.Channel,
              IsActive = true,
              Note = ""
            };
            te.Id = _dbController.InsertThermoelement(te);
            Settings.ThermoelementsByCanvas[key].Add(te);
          }
        }

        // Clear the Thermoelement settings first
        Settings.ThermoelementsByCanvas.Clear();
        Settings.ThermoelementsByCanvas["MainTop"] = new List<Thermoelement>();
        Settings.ThermoelementsByCanvas["MainBottom"] = new List<Thermoelement>();
        Settings.ThermoelementsByCanvas["ReinfTop"] = new List<Thermoelement>();
        Settings.ThermoelementsByCanvas["ReinfBottom"] = new List<Thermoelement>();

        AddAndInsert(_mainTopThermoelements, "MainTop");
        AddAndInsert(_mainBottomThermoelements, "MainBottom");
        AddAndInsert(_reinfTopThermoelements, "ReinfTop");
        AddAndInsert(_reinfBottomThermoelements, "ReinfBottom");

        DialogResult = true;
        Close();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error during confirmation: {ex.Message}", "Error",
                      MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /*
     * ResetButton_Click
     * Resets all placed thermoelements and clears the UI.
     */
    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
      var result = MessageBox.Show("Are you sure you want to reset all points?", "Reset Placement",
                                 MessageBoxButton.YesNo, MessageBoxImage.Question);

      if (result == MessageBoxResult.Yes)
      {
        _isPlacing = false;
        _thermoelementsPlaced = 0;
        _totalThermoelementsToPlace = 0;
        _selectedThermoelement = null;
        _isEditingMode = false;

        _mainTopThermoelements.Clear();
        _mainBottomThermoelements.Clear();
        _reinfTopThermoelements.Clear();
        _reinfBottomThermoelements.Clear();

        RedrawallThermoelements();
        UpdateUI();

        ConfirmButton.IsEnabled = false;
        StatusText.Text = "Reset complete - Ready for new placement";
      }
    }

    /*
     * CloseButton_Click
     * Handles closing the window, prompting if there are unsaved changes.
     */
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
      if (_isPlacing || _thermoelementsPlaced > 0)
      {
        var result = MessageBox.Show("There are unsaved changes. Are you sure you want to close?",
                                   "Unsaved Changes",
                                   MessageBoxButton.YesNo,
                                   MessageBoxImage.Question);
        if (result == MessageBoxResult.No)
          return;
      }

      DialogResult = false;
      Close();
    }

    /*
     * UpdateUI
     * Updates all UI elements to reflect the current placement state.
     */
    private void UpdateUI()
    {
      try
      {
        ThermoelementsPlacedText.Text = $"Thermoelements placed: {_thermoelementsPlaced}/{_totalThermoelementsToPlace}";
        PlacementProgress.Maximum = _totalThermoelementsToPlace;
        PlacementProgress.Value = _thermoelementsPlaced;

        ToggleModelButton.Content = _isReinforcementMode ? "🔧 REINFORCEMENT MODEL" : "📐 MAINBODY MODEL";

        var allThermoelements = _mainTopThermoelements.Concat(_mainBottomThermoelements)
          .Concat(_reinfTopThermoelements).Concat(_reinfBottomThermoelements);
        if (allThermoelements.Any())
        {
          var channels = allThermoelements.Select(p => p.Channel).OrderBy(i => i).ToList();
          var usedChannels = string.Join(", ", channels);

          ChannelInfoText.Text = $"Used channels: {usedChannels}";
          ChannelInfoText.Foreground = Brushes.LightGreen;
        }
        else
        {
          ChannelInfoText.Text = "No channels placed";
          ChannelInfoText.Foreground = Brushes.Gray;
        }

        if (!_isPlacing && _thermoelementsPlaced == 0)
        {
          ThermoelementCountTextBox.IsEnabled = true;
          StartButton.IsEnabled = true;
          ToggleModelButton.IsEnabled = true;
          StatusText.Text = "Ready - Enter number of points and start placement";
        }
        else if (_isPlacing)
        {
          ThermoelementCountTextBox.IsEnabled = false;
          StartButton.IsEnabled = false;
          ToggleModelButton.IsEnabled = true;
          StatusText.Text = $"Placing points... ({_thermoelementsPlaced}/{_totalThermoelementsToPlace})";
        }
        else if (_thermoelementsPlaced >= _totalThermoelementsToPlace)
        {
          ToggleModelButton.IsEnabled = true;
          StatusText.Text = "Placement completed - You can still switch views to review";
        }

        ChangeChannelButton.IsEnabled = _selectedThermoelement != null;
        EditThermoelementButton.IsEnabled = _selectedThermoelement != null;
        ConfirmButton.IsEnabled = _thermoelementsPlaced >= _totalThermoelementsToPlace;

        UpdateSelectedPointUI();
      }
      catch { }
    }

    /*
     * GetPointListForCanvasType
     * Returns the list of placed thermoelements for the specified canvas type.
     */
    private List<PlacedThermoelement> GetPointListForCanvasType(string canvasType)
    {
      return canvasType switch
      {
        "MainTop" => _mainTopThermoelements,
        "MainBottom" => _mainBottomThermoelements,
        "ReinfTop" => _reinfTopThermoelements,
        "ReinfBottom" => _reinfBottomThermoelements,
        _ => throw new InvalidOperationException("Unknown canvas type")
      };
    }

    /*
     * GetAllCanvases
     * Returns an enumerable of all canvas controls in the window.
     */
    private IEnumerable<Canvas> GetAllCanvases()
    {
      yield return MainTopCanvas;
      yield return MainBottomCanvas;
      yield return ReinfTopCanvas;
      yield return ReinfBottomCanvas;
    }

    /*
     * PlacedThermoelement
     * Data class representing a placed thermoelement with channel, position, and canvas type.
     */

    public class PlacedThermoelement
    {
      public int Channel { get; set; }
      public double CanvasX { get; set; }
      public double CanvasY { get; set; }
      public double ModelX { get; set; }
      public double ModelY { get; set; }
      public double RelativeX { get; set; }
      public double RelativeY { get; set; }
      public required string CanvasType { get; set; }
    }

    /*
     * CoordinateConverter
     * Static helper class for converting between canvas, relative, and model coordinates.
     */
    public static class CoordinateConverter
    {
      public static (double relativeX, double relativeY) ConvertCanvasToRelative(
          double canvasX, double canvasY, double canvasWidth, double canvasHeight)
      {
        double relativeX = canvasX / canvasWidth;
        double relativeY = canvasY / canvasHeight;
        relativeX = Math.Max(0, Math.Min(1, relativeX));
        relativeY = Math.Max(0, Math.Min(1, relativeY));
        return (relativeX, relativeY);
      }

      public static (double modelX, double modelY) ConvertRelativeToModel(
          double relativeX, double relativeY)
      {
        double modelX = relativeX * 5.0;
        double modelY = relativeY * 2.0;
        return (modelX, modelY);
      }

      public static (double canvasX, double canvasY) ConvertRelativeToCanvas(
          double relativeX, double relativeY, double canvasWidth, double canvasHeight)
      {
        double canvasX = relativeX * canvasWidth;
        double canvasY = relativeY * canvasHeight;
        return (canvasX, canvasY);
      }
    }
  }
}
