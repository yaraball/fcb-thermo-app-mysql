using System.Windows;
using System.Windows.Input;

namespace fcb_thermo_app.Views
{
  /*
   * IndexInputDialog
   * Dialog window for entering or changing the index of a selected point.
   * Handles input validation, keyboard shortcuts, and dialog result.
   */
  public partial class IndexInputDialog : Window
  {
    public int SelectedIndex { get; private set; }

    /*
     * IndexInputDialog(int currentIndex)
     * Initializes the dialog with the current index value.
     */
    public IndexInputDialog(int currentIndex)
    {
      InitializeComponent();
      SelectedIndex = currentIndex;
      IndexTextBox.Text = currentIndex.ToString();
      Loaded += OnLoaded;
    }


    /*
     * OnLoaded
     * Selects all text and focuses the input box when the dialog loads.
     */
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      IndexTextBox.SelectAll();
      IndexTextBox.Focus();
    }

    /*
     * OKButton_Click
     * Validates the input and closes the dialog with a positive result if valid.
     */
    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
      if (int.TryParse(IndexTextBox.Text, out int index) && index > 0)
      {
        SelectedIndex = index;
        DialogResult = true;
        Close();
      }
      else
      {
        MessageBox.Show("Please enter a valid positive number.", "Invalid Input",
                      MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    /*
     * CancelButton_Click
     * Closes the dialog without applying changes.
     */
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    /*
     * IndexTextBox_PreviewKeyDown
     * Handles Enter (OK) and Escape (Cancel) keyboard shortcuts.
     */
    private void IndexTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        OKButton_Click(sender, e);
      }
      else if (e.Key == Key.Escape)
      {
        CancelButton_Click(sender, e);
      }
    }
  }
}
