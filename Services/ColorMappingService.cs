using System.Globalization;
using System.Windows.Media;

namespace fcb_thermo_app.Services
{
  /*
   * ColorMappingService
   * Provides methods to map temperature and performance values to colors using a gradient.
   */
  public class ColorMappingService
  {
    /*
     * GetTemperatureColor
     * Maps a temperature value string (e.g., "23.5°C") to a color based on the provided min/max range.
     * Returns gray if the value is invalid.
     */
    public static Color GetTemperatureColor(string value, double min, double max)
    {
      if (!value.EndsWith("°C"))
        return Colors.Gray;

      string numberPart = value.Replace("°C", "").Trim();
      if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double tempValue))
        return Colors.Gray;

      return GetGradientColor(tempValue, min, max);
    }

    /*
      * GetPerformanceColor
      * Maps a performance value string (e.g., "85%") to a color using a 0-100% scale.
      * Returns gray if the value is invalid.
    */
    public static Color GetPerformanceColor(string value)
    {
      if (!value.EndsWith("%"))
        return Colors.Gray;

      string numberPart = value.Replace("%", "").Trim();
      if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double perfValue))
        return Colors.Gray;

      // For performance, use 0-100% scale
      return GetGradientColor(perfValue, 0, 100);
    }

    /*
     * GradientStops
     * Defines the color gradient stops for mapping values to colors.
     * Each tuple contains an offset (0-1) and a Color, forming a blue-to-red gradient.
     */
    private static readonly (double Offset, Color Color)[] GradientStops = new[]
    {
      (0.0, Color.FromRgb(0, 0, 255)),      // Blue
      (0.25, Color.FromRgb(0, 255, 255)),   // Cyan
      (0.5, Color.FromRgb(0, 255, 0)),      // Green
      (0.75, Color.FromRgb(255, 255, 0)),   // Yellow
      (1.0, Color.FromRgb(255, 0, 0)),      // Red
    };

    /*
     * InterpolateGradient
     * Interpolates between defined gradient stops to return a color for a given percent (0-1).
     * Used internally for smooth color transitions in the gradient.
     *
     * percent: Value between 0 and 1 representing the position in the gradient.
     * Returns: Interpolated Color.
     */
    private static Color InterpolateGradient(double percent)
    {
      if (percent <= 0) return GradientStops[0].Color;
      if (percent >= 1) return GradientStops[^1].Color;

      for (int i = 0; i < GradientStops.Length - 1; i++)
      {
        var (offset1, color1) = GradientStops[i];
        var (offset2, color2) = GradientStops[i + 1];
        if (percent >= offset1 && percent <= offset2)
        {
          double t = (percent - offset1) / (offset2 - offset1);
          byte r = (byte)(color1.R + (color2.R - color1.R) * t);
          byte g = (byte)(color1.G + (color2.G - color1.G) * t);
          byte b = (byte)(color1.B + (color2.B - color1.B) * t);
          return Color.FromRgb(r, g, b);
        }
      }
      return Colors.Gray; // fallback
    }

    /*
     * GetGradientColor
     * Maps a value within a min/max range to a color using the defined gradient stops.
     * Used by temperature and performance color mapping methods.
     *
     * value: The value to map.
     * min, max: The range for normalization.
     * Returns: Color corresponding to the value's position in the range.
     */
    private static Color GetGradientColor(double value, double min, double max)
    {
      if (max <= min || min == -1 || max == -1) return Colors.Gray;
      value = Math.Max(min, Math.Min(max, value));
      double percent = (value - min) / (max - min);
      return InterpolateGradient(percent);
    }
  }
}
