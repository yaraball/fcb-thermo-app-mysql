namespace fcb_thermo_app.Services
{
  /*
   * PerformanceMappingService
   * Provides methods to retrieve and map performance values for different canvas types and positions.
   */
  public class PerformanceMappingService
  {
    /*
     * GetPerformanceValue
     * Returns the performance value (as a percentage string) for a given canvas type and grid position.
     * Returns "N/A" if not available, or "" for Reinforcement canvases.
     *
     * canvasType: Type of the canvas (e.g., "MainTop").
     * row, col: Grid position.
     */
    public static string GetPerformanceValue(string canvasType, int row, int col)
    {
      // Get the CanvasAssignment for the given type
      if (!Settings.CanvasAssignments.TryGetValue(canvasType, out var assignment) || assignment == null)
        return "N/A";

      // For Reinforcement canvases, return empty string
      if (canvasType == "ReinfBottom")
        return "";

      int index = getIndex(canvasType, row, col);

      if (assignment.PerformanceSettings != null && index >= 0 && index < assignment.PerformanceSettings.Count)
      {
        return $"{assignment.PerformanceSettings[index]}%";
      }

      return "N/A";
    }

    /*
       * getIndex
       * Calculates the index in the performance settings list for a given canvas type and grid position.
       * Handles special cases for Main and Reinforcement canvases.
       *
       * canvasType: Type of the canvas.
       * row, col: Grid position.
       * Returns: Index in the performance settings list.
       */
    private static int getIndex(string canvasType, int row, int col)
    {
      // Adjust index for vertical boxes in MainBody
      if (canvasType.Contains("Main"))
      {
        if (row == 0 || col == 0) return (row * 20 + col) / 2; // Directly translate to top row and left column
        if (col == 19) return (row * 20 + col) / 2 + 2; // Adjust rightmost column by 2 positions
        if (row == 7) return (row * 20 + col) / 2; // Keep the last row direct again

        return (row * 20 + col) / 2 + 1; // Adjust all other boxes by 1 position
      }
      if (canvasType == "ReinfTop")
      {
        if (row == 2 || row == 5) return (row - 2) * 8 + (col - 2) / 2; // First and last row adjusted for missing boxes
        if (col == 2 || col == 3) return col + 6; // First two verticals adjusted for already used boxes
        if (col == 16 || col == 17) return col; // Last two verticals adjusted for already used boxes
        return (row - 2) * 8 + col / 2; // Middle rows adjusted for missing boxes
      }
      return row * 10 + col; // Direct mapping for other canvas types
    }
  }
}
