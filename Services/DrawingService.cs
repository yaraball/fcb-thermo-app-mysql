using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using fcb_thermo_app.Controllers;
using fcb_thermo_app.Models;
using fcb_thermo_app.Views;

namespace fcb_thermo_app.Services
{
  /*
   * DrawingService
   * Provides methods to render the DXF model, grid, thermoelements, and performance overlays onto a WPF Canvas.
   * Handles drawing, scaling, and interactive editing of thermoelements and overlays.
   */
  public class DrawingService
  {
    private readonly DatabaseController databaseController = new();
    private readonly DxfController dxfController;
    private readonly TemperatureMappingService temperatureMappingService = new();

    /*
     * Constructor for DrawingService.
     * Initializes the DxfController with the shared DatabaseController instance.
     */
    public DrawingService()
    {
      dxfController = new DxfController(databaseController);
    }

    /*
     * DrawView
     * Renders the entire view for a given canvas, including the grid, DXF model, pyrometer positions, thermoelements, and overlays.
     * Handles scaling, layer visibility, and interactive elements.
     *
     * Parameters:
     *   canvas         - The WPF Canvas to draw on.
     *   canvasType     - String identifier for the canvas type (e.g., "MainTop", "ReinfBottom").
     *   thermoelements - List of Thermoelement objects to render.
     *   fw             - Frame width in pixels.
     *   fh             - Frame height in pixels.
     */
    public void DrawView(Canvas canvas, string canvasType, List<Thermoelement> thermoelements, double fw, double fh)
    {
      // Calculate scaling factor based on canvas size
      double scaleFactor = fw / 1600.0;

      canvas.Children.Clear();

      DrawGridInFrame(canvas, canvasType, 0, 0, fw, fh, scaleFactor);

      // Create the rectangle
      var frame = new Rectangle
      {
        Width = fw,
        Height = fh,
        Stroke = new SolidColorBrush((Color)Application.Current.Resources["PrimaryBlue"]),
        StrokeThickness = 2 * scaleFactor,
      };

      // Position the rectangle
      Canvas.SetLeft(frame, 0);
      Canvas.SetTop(frame, 0);
      canvas.Children.Add(frame);

      // Draw the Pyrometer Positions if the layer is selcted
      if (Settings.LayerSettings["ShowPyrometerPositions"])
      {
        (Geometry geometry, Rect Bounds) = dxfController.GetDxfGeometry(Settings.PyrometerPositions, canvasType.Contains("Reinf"));
        DrawDXF(canvas, geometry, Bounds, fw, fh);
      }

      // Draw the DXF Model environment if available
      if (Settings.CurrentDxfModel is DXFModel dxf)
      {
        (Geometry geometry, Rect Bounds) = dxfController.GetDxfGeometry(dxf, canvasType.Contains("Reinf"));
        DrawDXF(canvas, geometry, Bounds, fw, fh);
      }

      // Draw the Pyrometer is the layer is selected
      if (Settings.LayerSettings["ShowPyrometer"] && Settings.CanvasAssignments.Count == 4)
      {
        int pyrometer = Settings.CanvasAssignments[canvasType].PyrometerPosition;
        if (pyrometer != -1)
        {
          int cols = 20;
          int rows = 8;
          double cellWidth = fw / cols;
          double cellHeight = fh / rows;

          int row = pyrometer / cols;
          int col = pyrometer % cols;

          double centerX = col * cellWidth + cellWidth / 2;
          double offset = pyrometer <= 80 ? 0.69 : 0.25; // Offset to correct pyrometer hole position vertically
          double centerY = row * cellHeight + cellHeight * offset;
          double size = 24 * scaleFactor;
          var triangle = new Polygon
          {
            Points = new PointCollection
              {
                  new Point(centerX, centerY - size / 2),
                  new Point(centerX - size / 2, centerY + size / 2),
                  new Point(centerX + size / 2, centerY + size / 2)
              },
            Fill = new SolidColorBrush(Color.FromRgb(255, 105, 180)), // Pink
            Stroke = Brushes.DeepPink,
            StrokeThickness = 2 * scaleFactor,
            Opacity = 0.85
          };
          canvas.Children.Add(triangle);

          // Add tooltip
          int pyrometerNumber = Settings.CanvasAssignments[canvasType].PyrometerNumber;
          triangle.ToolTip = new ToolTip
          {
            Style = (Style)Application.Current.Resources["RoundedTooltipStyle"],
            Content = $"Pyrometer Number: {(pyrometerNumber == -1 ? "not set" : pyrometerNumber.ToString())}"
          };
        }
      }

      // Add ThermoElements
      AddThermoElements(canvas, canvasType, thermoelements, fw, fh, scaleFactor);
    }

    /*
     * DrawDXF
     * Draws a DXF geometry onto the canvas, scaling and centering it within the given frame.
     *
     * Parameters:
     *   canvas   - The WPF Canvas to draw on.
     *   envGeom  - The Geometry object representing the DXF outline.
     *   bounds   - The bounding rectangle of the geometry.
     *   fw       - Frame width in pixels.
     *   fh       - Frame height in pixels.
     */
    private void DrawDXF(Canvas canvas, Geometry envGeom, Rect bounds, double fw, double fh)
    {
      double scaleX = fw / bounds.Width;
      double scaleY = fh / bounds.Height;
      double scale = Math.Min(scaleX, scaleY);
      double offsetX = (fw - bounds.Width * scale) / 2.0 - bounds.X * scale;
      double offsetY = (fh - bounds.Height * scale) / 2.0 - bounds.Y * scale;

      var env = new Path
      {
        Data = envGeom,
        Stroke = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
        StrokeThickness = 4,
        RenderTransform = new TransformGroup
        {
          Children = new TransformCollection
                    {
                        new ScaleTransform(scale, scale),
                        new TranslateTransform(offsetX, offsetY)
                    }
        }
      };
      canvas.Children.Add(env);
    }

    /*
     * AddThermoElements
     * Draws thermoelement markers (ellipses), temperature labels, and tooltips on the canvas.
     * Handles interactive dragging and editing of thermoelements, updating both UI and database.
     *
     * Parameters:
     *   canvas         - The WPF Canvas to draw on.
     *   canvasType     - String identifier for the canvas type.
     *   thermoelements - List of Thermoelement objects to render.
     *   fw             - Frame width in pixels.
     *   fh             - Frame height in pixels.
     *   scaleFactor    - Scaling factor for element sizes.
     */
    private void AddThermoElements(Canvas canvas, string canvasType, List<Thermoelement> thermoelements, double fw, double fh, double scaleFactor)
    {
      System.Diagnostics.Debug.WriteLine($"[ADDTHERMOELEMENTS] CanvasType: {canvasType}, Drawing {thermoelements.Count} thermoelements");
      foreach (var te in thermoelements)
      {
        System.Diagnostics.Debug.WriteLine($"[ADDTHERMOELEMENTS]   TE Ch{te.Channel}: RelX={te.RelativeX:F3}, RelY={te.RelativeY:F3}, Note={te.Note}");
        // Calculate absolute positions based on relative positions
        double x = te.RelativeX * fw;
        double y = te.RelativeY * fh;

        // Get the temperature for color mapping
        string tempText = temperatureMappingService.GetTemperatureTextForThermoelement(te, canvasType);

        // DOT (Ellipse) with heatmap if the layer is activated
        var dot = new Ellipse
        {
          Width = 40 * scaleFactor,
          Height = 40 * scaleFactor,
          Fill = Settings.LayerSettings["ShowThermoelementHeatmap"] ? new SolidColorBrush(ColorMappingService.GetTemperatureColor(tempText, Settings.ColorScaleMin, Settings.ColorScaleMax)) : Brushes.Transparent,
          Stroke = te.IsActive ? Brushes.White : Brushes.Gray,
          StrokeThickness = Settings.LayerSettings["ShowThermoelementHeatmap"]
              ? (te.IsActive ? 2 * scaleFactor : 1 * scaleFactor)
              : 0,
          Opacity = te.IsActive ? 0.95 : 0.35,
          Tag = te // Associate the Thermoelement with the Ellipse
        };
        Canvas.SetLeft(dot, x - 20 * scaleFactor); // Center the ellipse on the coordinates
        Canvas.SetTop(dot, y - 20 * scaleFactor);
        canvas.Children.Add(dot);

        // Celsius label if the layer is selected
        var label = new TextBlock
        {
          Text = tempText,
          FontSize = 20 * scaleFactor,
          FontWeight = FontWeights.Bold,
          Foreground = te.IsActive ? Brushes.Black : Brushes.Gray,
          Background = Brushes.Transparent,
          Visibility = Settings.LayerSettings["ShowThermoelementValues"] ? Visibility.Visible : Visibility.Hidden,
          Tag = te // Associate the Thermoelement with the TextBlock
        };
        Canvas.SetLeft(label, x - 20 * scaleFactor); // Center the label below the ellipse
        Canvas.SetTop(label, y + 20 * scaleFactor);
        canvas.Children.Add(label);

        // BURNOUT or N/A are marked with X 
        TextBlock xText = new TextBlock
        {
          Text = tempText.EndsWith("°C") ? "" : "X",
          Foreground = Brushes.Red,
          FontWeight = FontWeights.Bold,
          FontSize = 40 * scaleFactor,
          IsHitTestVisible = false,
          Opacity = Settings.LayerSettings["ShowThermoelementHeatmap"] ? 1 : 0,
          Tag = te // Associate the Thermoelement with the TextBlock
        };
        Canvas.SetLeft(xText, x - 13 * scaleFactor); // Adjust for centering
        Canvas.SetTop(xText, y - 30 * scaleFactor);
        canvas.Children.Add(xText);

        // Build the tooltip string with all relevant attributes
        dot.ToolTip = new ToolTip
        {
          Style = (Style)Application.Current.Resources["RoundedTooltipStyle"],
          Content = $"Channel: {te.Channel}\nIsActive: {te.IsActive}\nPosition: ({(int)(te.RelativeX * 5000)}, {(int)(te.RelativeY * 2000)})\nNote: {te.Note}"
        };

        // Add dragging logic directly to the ellipse
        bool isDragging = false;
        Point startMouse = default;

        dot.MouseLeftButtonDown += (s, e) =>
        {
          isDragging = true;
          startMouse = e.GetPosition(canvas);
          dot.CaptureMouse(); // Capture the mouse to track movement outside the ellipse
          e.Handled = true;
        };

        dot.MouseMove += (s, e) =>
        {
          if (!isDragging) return;

          var currentMouse = e.GetPosition(canvas);
          double offsetX = currentMouse.X - startMouse.X;
          double offsetY = currentMouse.Y - startMouse.Y;

          // Update the relative position of the Thermoelement
          te.RelativeX = Math.Max(0, Math.Min((x + offsetX) / fw, 1)); // Clamp between 0 and 1
          te.RelativeY = Math.Max(0, Math.Min((y + offsetY) / fh, 1)); // Clamp between 0 and 1

          // Recalculate absolute positions
          x = te.RelativeX * fw;
          y = te.RelativeY * fh;

          // Update the positions of the ellipse and label
          Canvas.SetLeft(dot, x - 20 * scaleFactor);
          Canvas.SetTop(dot, y - 20 * scaleFactor);
          Canvas.SetLeft(label, x - 20 * scaleFactor);
          Canvas.SetTop(label, y + 20 * scaleFactor);
          Canvas.SetLeft(xText, x - 13 * scaleFactor);
          Canvas.SetTop(xText, y - 30 * scaleFactor);

          // Update the starting mouse position for smooth dragging
          startMouse = currentMouse;

          e.Handled = true;
        };

        dot.MouseLeftButtonUp += (s, e) =>
        {
          isDragging = false;
          dot.ReleaseMouseCapture(); // Release the mouse capture

          // Save the updated Thermoelement position
          databaseController.UpdateThermoelementPosition(te.Id, te.RelativeX, te.RelativeY);
          // Update the in-memory settings for the correct canvas assignment
          if (Settings.ThermoelementsByCanvas.TryGetValue(canvasType, out var thermoelements))
          {
            var teObj = thermoelements?.FirstOrDefault(x => x.Id == te.Id);
            if (teObj != null)
            {
              teObj.RelativeX = te.RelativeX;
              teObj.RelativeY = te.RelativeY;
            }
          }

          // Update the tooltip with the new position
          dot.ToolTip = new ToolTip
          {
            Style = (Style)Application.Current.Resources["RoundedTooltipStyle"],
            Content = $"Channel: {te.Channel}\nIsActive: {te.IsActive}\nPosition: ({(int)(te.RelativeX * 5000)}, {(int)(te.RelativeY * 2000)})\nNote: {te.Note}"
          };
          e.Handled = true;
        };

        // Add click event to open edit window for thermoelements
        dot.MouseRightButtonDown += (s, e) =>
        {
          bool wasActive = te.IsActive;

          // When opening the edit window
          var editWindow = new ThermoelementEditWindow(te.Channel, te.IsActive, te.Note) { Owner = Application.Current.MainWindow };

          if (editWindow.ShowDialog() == true)
          {
            te.Channel = editWindow.ChannelId;
            te.IsActive = editWindow.IsActive;
            te.Note = editWindow.Note;

            // Update database and in-memory settings as needed
            databaseController.UpdateThermoelement(te.Id, te.Channel, te.IsActive, te.Note);

            // Update the in-memory settings for the correct canvas assignment
            if (Settings.ThermoelementsByCanvas.TryGetValue(canvasType, out var thermoelements))
            {
              var teObj = thermoelements?.FirstOrDefault(x => x.Id == te.Id);
              if (teObj != null)
              {
                teObj.Channel = te.Channel;
                teObj.IsActive = te.IsActive;
                teObj.Note = te.Note;
              }
            }

            // Update tooltip
            dot.ToolTip = new ToolTip
            {
              Style = (Style)Application.Current.Resources["RoundedTooltipStyle"],
              Content = $"Channel: {te.Channel}\nIsActive: {te.IsActive}\nPosition: ({(int)(te.RelativeX * 5000)}, {(int)(te.RelativeY * 2000)})\nNote: {te.Note}"
            };

            // Update visual appearance based on IsActive state changed
            if (te.IsActive && wasActive == false || !te.IsActive && wasActive == true)
            {
              // Update the ellipse appearance
              dot.Stroke = te.IsActive ? Brushes.White : Brushes.Gray;
              dot.StrokeThickness = te.IsActive ? 2 * scaleFactor : 1 * scaleFactor;
              dot.Opacity = te.IsActive ? 0.95 : 0.35;

              // Update the label appearance
              label.Foreground = te.IsActive ? Brushes.Black : Brushes.Gray;

              // Update the stats
              (Application.Current.MainWindow as MainWindow)?.UpdateStats();
            }
          }
          e.Handled = true;
        };
      }
    }

    /*
     * DrawGridInFrame
     * Draws the performance grid overlay within the specified frame on the canvas.
     * Handles different grid layouts for main and reinforcement canvases, and adds performance value overlays.
     *
     * Parameters:
     *   canvas      - The WPF Canvas to draw on.
     *   canvasType  - String identifier for the canvas type.
     *   left        - Left offset for the grid.
     *   top         - Top offset for the grid.
     *   width       - Width of the grid area.
     *   height      - Height of the grid area.
     *   scaleFactor - Scaling factor for grid and text sizes.
     */
    private void DrawGridInFrame(Canvas canvas, string canvasType, double left, double top, double width, double height, double scaleFactor)
    {
      // Clear the canvas before drawing
      canvas.Children.Clear();

      // If it's reinforcement and bottom, skip drawing the grid
      if (canvasType == "ReinfBottom")
      {
        return;
      }

      // Create a Grid to hold the cells
      var grid = new Grid
      {
        Width = width,
        Height = height
      };

      // Define the grid structure (20 columns, 8 rows)
      int rows = 8;
      int cols = 20;

      // Calculate the size of each cell
      double cellWidth = width / cols;
      double cellHeight = height / rows;

      // Add row and column definitions to the grid
      for (int row = 0; row < rows; row++)
      {
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(cellHeight) });
      }
      for (int col = 0; col < cols; col++)
      {
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cellWidth) });
      }

      // Add cells to the grid
      for (int row = 0; row < rows; row++)
      {
        for (int col = 0; col < cols; col++)
        {
          // If it's reinforcement and top, only draw the middle 8x4 cells
          if (canvasType == "ReinfTop" && (row < 2 || row > 5 || col < 2 || col > 17))
          {
            continue;
          }

          // Vertical boxes in columns 0, 1, 18, 19 for rows 1-6 of MainBody
          if ((row == 1 || row == 2 || row == 3 || row == 4 || row == 5 || row == 6) && (col == 0 || col == 1 || col == 18 || col == 19))
          {
            if (row % 2 == 1) // Only add vertical boxes in the first row of the double rows
            {
              AddGridBox(true, row, col, canvasType, grid, cellWidth, cellHeight, scaleFactor);
            }
          }

          else
          {
            if (canvasType == "ReinfTop" && (row == 3 || row == 4) && (col <= 3 || col >= 16))
            {
              // Vertical boxes in the sides of reinforcement top
              if (row % 2 == 1) // Only add boxes spanning 2 columns
              {
                AddGridBox(true, row, col, canvasType, grid, cellWidth, cellHeight, scaleFactor);
              }
            }
            else
            {
              // Horizontal boxes in the middle columns
              if (col % 2 == 0) // Only add boxes spanning 2 columns
              {
                AddGridBox(false, row, col, canvasType, grid, cellWidth, cellHeight, scaleFactor);
              }
            }
          }
        }
      }

      // Position the grid on the canvas
      Canvas.SetLeft(grid, left);
      Canvas.SetTop(grid, top);

      // Add the grid to the canvas
      canvas.Children.Add(grid);
    }

    /*
     * AddGridBox
     * Adds a single grid box (cell) to the performance grid, with optional spanning for vertical/horizontal orientation.
     * Sets background color based on performance heatmap and displays the performance value.
     *
     * Parameters:
     *   vertical     - True for vertical spanning (2 rows), false for horizontal spanning (2 columns).
     *   row, col     - Grid position for the box.
     *   canvasType   - String identifier for the canvas type.
     *   grid         - The Grid control to add the box to.
     *   cellWidth    - Width of a single grid cell.
     *   cellHeight   - Height of a single grid cell.
     *   scaleFactor  - Scaling factor for box and text sizes.
     */
    private void AddGridBox(bool vertical, int row, int col, string canvasType, Grid grid, double cellWidth, double cellHeight, double scaleFactor)
    {
      string performanceValue = PerformanceMappingService.GetPerformanceValue(canvasType, row, col);
      var bgBrush = Settings.LayerSettings["ShowPerformanceHeatmap"] ? new SolidColorBrush(ColorMappingService.GetPerformanceColor(performanceValue)) { Opacity = 0.2 } : Brushes.Transparent;

      // Create a Border for the box
      var box = new Border
      {
        BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), // Light gray border
        BorderThickness = Settings.LayerSettings["ShowPerformanceGrid"] ? new Thickness(scaleFactor) : new Thickness(0),
        Background = bgBrush
      };

      // Add a TextBlock inside the Border for displaying text
      var textBlock = new TextBlock
      {
        Text = performanceValue,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        FontSize = 20 * scaleFactor,
        Foreground = Settings.LayerSettings["ShowPerformanceValues"] ? Brushes.Black : Brushes.Transparent
      };

      box.Child = textBlock;

      if (vertical)
      {
        // Vertical box: spans 2 rows
        Grid.SetRowSpan(box, 2);
        box.Width = cellWidth; // Width of one cell
        box.Height = cellHeight * 2; // Height of two cells
      }
      else
      {
        // Horizontal box: spans 2 columns
        Grid.SetColumnSpan(box, 2);
        box.Width = cellWidth * 2; // Width of two cells
        box.Height = cellHeight; // Height of one cell
      }

      // Position the box in the grid
      Grid.SetRow(box, row);
      Grid.SetColumn(box, col);

      // Add the box to the grid
      grid.Children.Add(box);
    }
  }
}
