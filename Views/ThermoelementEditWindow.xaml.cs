using System.Windows;

namespace fcb_thermo_app.Views
{
  /*
   * ThermoelementEditWindow
   * Dialog window for editing a single thermoelement's channel, active state, and note.
   * Handles input validation and saving changes.
   */
  public partial class ThermoelementEditWindow : Window
  {
    public int ChannelId { get; private set; }
    public new bool IsActive { get; private set; }
    public string Note { get; private set; } = string.Empty;

    /*
     * ThermoelementEditWindow(int channelId, bool isActive, string note)
     * Initializes the window with the provided thermoelement data.
     */
    public ThermoelementEditWindow(int channelId, bool isActive, string note)
    {
      InitializeComponent();
      ChannelIdBox.Text = channelId.ToString();
      ActiveToggle.IsChecked = isActive;
      NoteBox.Text = note;
    }


    /*
     * Save_Click
     * Validates the channel ID and saves the edited thermoelement data.
     * Closes the dialog if successful.
     */
    private void Save_Click(object sender, RoutedEventArgs e)
    {
      if (!int.TryParse(ChannelIdBox.Text.Trim(), out int channelId) || channelId < 1 || channelId > 20)
      {
        MessageBox.Show("Channel ID must be an integer between 1 and 20.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      ChannelId = channelId;
      IsActive = ActiveToggle.IsChecked == true;
      Note = NoteBox.Text.Trim();
      DialogResult = true;
      Close();
    }
  }
}
