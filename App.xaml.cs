using System.Windows;
using System.Runtime.InteropServices;

using fcb_thermo_app.Controllers;

namespace fcb_thermo_app;

/// <summary>
/// Main application class for the Oven Thermal Analytics Dashboard.
/// Handles application startup, initializes the MySQL database, and manages global exception handling.
/// </summary>
public partial class App : Application
{
  [DllImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static extern bool AllocConsole();
  protected override void OnStartup(StartupEventArgs e)
  {
    try
    {
      base.OnStartup(e);

      // AllocConsole(); // Uncomment this line to enable console for debugging

      // Initialize the MySQL database
      // To view the database, use a MySQL client like MySQL Workbench and connect to the database.
      // For now this uses hardcoded connection settings, later these should be configurable.
      // Currently: localhost:3306, user: root, password: password, database: fcb_thermo_app
      var dbController = new DatabaseController();
      dbController.InitializeDatabase();
    }
    catch (Exception ex)
    {
      MessageBox.Show("Error during application startup: " + ex.Message, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
      Current.Shutdown();
    }
  }
}
