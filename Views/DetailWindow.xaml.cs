using System.Windows;
using System.Windows.Controls;
using fcb_thermo_app.Models;
using fcb_thermo_app.Services;

namespace fcb_thermo_app.Views
{
  /*
   * DetailWindow
   * Window for displaying a detailed view of a single canvas.
   * Handles dynamic title, resizing, and canvas redraw.
   */
  public partial class DetailWindow : Window
  {
    /*
     * DetailWindow(string canvasType, List<Thermoelement> thermoElements)
     * Initializes the detail window, sets the title, and sets up event handlers for redraw.
     */
    public DetailWindow(string canvasType, List<Thermoelement> thermoElements)
    {
      InitializeComponent();

      // Set the title dynamically
      DetailTitle.Text = GetTitle(canvasType);

      Loaded += (_, __) => Redraw(DetailCanvas, canvasType, thermoElements);
      SizeChanged += (_, __) => Redraw(DetailCanvas, canvasType, thermoElements);
      Closed += (_, __) => (Application.Current.MainWindow as MainWindow)?.RedrawAll();
    }

    /*
     * GetTitle
     * Helper method to determine the window title based on the canvas type.
     */
    private string GetTitle(string canvasType)
    {
      if (canvasType == "MainTop")
        return "Main Body Top";
      else if (canvasType == "ReinfTop")
        return "Reinforcement Top";
      else if (canvasType == "MainBottom")
        return "Main Body Bottom";
      else
        return "Reinforcement Bottom";
    }

    /*
     * Redraw
     * Redraws the detail canvas with the provided thermoelements and canvas type.
     */
    private void Redraw(Canvas DetailCanvas, string canvasType, List<Thermoelement> thermoElements)
    {
      DrawingService drawingService = new();

      double canvasHeight = ActualWidth - 40;
      double canvasWidth = canvasHeight * (5.0 / 2.0);

      DetailCanvas.Width = canvasWidth;
      DetailCanvas.Height = canvasHeight;

      drawingService.DrawView(DetailCanvas, canvasType, thermoElements, canvasWidth, canvasHeight);
    }
  }
}
