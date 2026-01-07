using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace fcb_thermo_app.Views
{
  /*
   * ExportWindow
   * Dialog window for exporting the main view or individual canvases as JPEG images.
   * Handles user selection, file export, and image rendering.
   */
  public partial class ExportWindow : Window
  {
    private readonly MainWindow _mainWindow;
    /*
     * ExportWindow(MainWindow mainwindow)
     * Initializes the export dialog and stores a reference to the main window.
     */
    public ExportWindow(MainWindow mainwindow)
    {
      InitializeComponent();
      _mainWindow = mainwindow;
    }

    /*
     * ExportButton_Click
     * Handles the export process based on user selection, including folder selection and file writing.
     */
    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
      // Get model and measurement names from Settings
      var model = Settings.CurrentDxfModel;
      string modelName = model?.Name ?? "Model";

      // Ask for a target folder
      var folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
      var result = folderDialog.ShowDialog();
      if (result != true || string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
        return;

      string targetDir = folderDialog.SelectedPath;
      bool exportedAny = false;

      // Export main view if checked
      if (MainViewCheckBox.IsChecked == true)
      {
        string fileName = $"{modelName}_MainView.jpg";
        string filePath = Path.Combine(targetDir, fileName);
        ExportMainViewAsJpeg(_mainWindow.MainContentGrid, filePath);
        exportedAny = true;
      }

      // Export selected canvases
      foreach (ListBoxItem item in CanvasList.Items)
      {
        var checkBox = item.Content as CheckBox;
        if (checkBox != null && checkBox.IsChecked == true)
        {
          Canvas? canvas = null;
          string canvasLabel = "";
          switch (checkBox.Tag as string)
          {
            case "TopViewCanvas":
              canvas = _mainWindow.TopViewCanvas;
              canvasLabel = "TopView";
              break;
            case "TopViewCanvasReinforcement":
              canvas = _mainWindow.TopViewCanvasReinforcement;
              canvasLabel = "TopViewReinforcement";
              break;
            case "BottomViewCanvas":
              canvas = _mainWindow.BottomViewCanvas;
              canvasLabel = "BottomView";
              break;
            case "BottomViewCanvasReinforcement":
              canvas = _mainWindow.BottomViewCanvasReinforcement;
              canvasLabel = "BottomViewReinforcement";
              break;
          }

          if (canvas is not null)
          {
            string fileName = $"{modelName}_{canvasLabel}.jpg";
            string filePath = Path.Combine(targetDir, fileName);
            ExportCanvasAsJpeg(canvas, filePath);
            exportedAny = true;
          }
        }
      }

      if (exportedAny)
        MessageBox.Show("Export completed successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
      else
        MessageBox.Show("No canvases selected for export.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);

      Close();
    }

    /*
     * ExportCanvasAsJpeg
     * Exports a single canvas as a JPEG image to the specified file path.
     */
    public void ExportCanvasAsJpeg(Canvas canvas, string filePath)
    {
      var size = new Size(canvas.ActualWidth, canvas.ActualHeight);
      canvas.Measure(size);
      canvas.Arrange(new Rect(size));

      var rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
      rtb.Render(canvas);

      var encoder = new JpegBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(rtb));

      using (var stream = new FileStream(filePath, FileMode.Create))
      {
        encoder.Save(stream);
      }
    }

    /*
     * ExportMainViewAsJpeg
     * Exports the main content grid as a JPEG image to the specified file path.
     */
    public void ExportMainViewAsJpeg(Grid mainContentGrid, string filePath)
    {
      var size = new Size(mainContentGrid.ActualWidth, mainContentGrid.ActualHeight);
      mainContentGrid.Measure(size);
      mainContentGrid.Arrange(new Rect(size));

      var rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
      rtb.Render(mainContentGrid);

      var encoder = new JpegBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(rtb));

      using (var stream = new FileStream(filePath, FileMode.Create))
      {
        encoder.Save(stream);
      }
    }
  }
}