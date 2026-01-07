using System.IO;
using System.Windows;
using fcb_thermo_app.Controllers;
using fcb_thermo_app.Models;
using Microsoft.Win32;

namespace fcb_thermo_app.Views
{
  /*
   * ModelImportWindow
   * Dialog window for creating a new DXF model.
   * Handles model name/serial input, DXF file selection, and model creation logic.
   */
  public partial class ModelImportWindow : Window
  {
    private readonly DxfController _dxfController;
    public string ModelName => ModelNameText.Text.Trim(); // Fetch the model name from the TextBox
    public string? SerialNumber => string.IsNullOrWhiteSpace(SerialNumberText.Text) ? null : SerialNumberText.Text.Trim(); // Fetch the serial number from the TextBox
    public string? MainBodyFilePath { get; private set; }
    public string? ReinforcementFilePath { get; private set; }

    /*
     * ModelImportWindow()
     * Initializes the window, sets up data context, and prepares the DxfController.
     */
    public ModelImportWindow()
    {
      InitializeComponent();
      DataContext = this;
      _dxfController = new DxfController(new DatabaseController());
    }

    /*
     * SelectMainBodyFile_Click / SelectReinforcementFile_Click
     * Opens a file dialog to select the main body or reinforcement DXF file.
     * Updates the displayed file path.
     */
    private void SelectMainBodyFile_Click(object sender, RoutedEventArgs e)
    {
      var openFileDialog = new OpenFileDialog
      {
        Filter = "DXF Files (*.dxf)|*.dxf",
        Title = "Select Main Body DXF File"
      };

      if (openFileDialog.ShowDialog() == true)
      {
        MainBodyFilePath = openFileDialog.FileName;
        MainBodyFilePathText.Text = $"{Path.GetFileName(MainBodyFilePath)}";
      }
    }

    private void SelectReinforcementFile_Click(object sender, RoutedEventArgs e)
    {
      var openFileDialog = new OpenFileDialog
      {
        Filter = "DXF Files (*.dxf)|*.dxf",
        Title = "Select Reinforcement DXF File"
      };

      if (openFileDialog.ShowDialog() == true)
      {
        ReinforcementFilePath = openFileDialog.FileName;
        ReinforcementFilePathText.Text = $"{Path.GetFileName(ReinforcementFilePath)}";
      }
    }

    /*
     * CancelButton_Click
     * Cancels model creation and closes the dialog.
     */
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    /*
     * CreateButton_Click
     * Validates input, creates a new DXF model using DxfController, and updates application state.
     * Handles errors and displays success or error messages.
     */
    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
      // Validate model name
      if (string.IsNullOrWhiteSpace(ModelName))
      {
        MessageBox.Show("Model name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      // Validate that at least one DXF file is selected
      if (string.IsNullOrWhiteSpace(MainBodyFilePath) && string.IsNullOrWhiteSpace(ReinforcementFilePath))
      {
        MessageBox.Show("At least one DXF file must be selected.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      try
      {
        // Pass the file paths to the DxfController for processing
        _dxfController.CreateNewDxfModel(
            ModelName,
            SerialNumber,
            MainBodyFilePath,
            ReinforcementFilePath
        );

        var model = _dxfController.GetDxfModelByNameAndSerial(ModelName, SerialNumber);
        if (model != null) { Settings.CurrentDxfModel = model; (Application.Current.MainWindow as MainWindow)?.RedrawAll(); }

        MessageBox.Show("DXF model created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
        Close();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}
