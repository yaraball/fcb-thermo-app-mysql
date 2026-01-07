using System.Text.Json;
using System.Windows;

using fcb_thermo_app.Controllers;
using fcb_thermo_app.Models;
using fcb_thermo_app.Views;

namespace fcb_thermo_app.Services
{
  /*
   * TemperatureMappingService
   * Provides methods to map, retrieve, and process temperature data for thermoelements and canvas assignments.
   */
  public class TemperatureMappingService
  {
    private readonly DatabaseController databaseController = new();

    /*
     * MeasurementEntry
     * Represents a single measurement entry with timestamp, temperature values, and alarm states.
     */
    public class MeasurementEntry
    {
      public string timestamp { get; set; } = "";
      public List<object> temperatures { get; set; } = new(); // Can be double or string ("BURNOUT" or "N/A")
      public int alarm1 { get; set; }
      public int alarmOut { get; set; }
    }

    /*
     * GetTemperatureTextForThermoelement
     * Returns the temperature text for a given thermoelement and canvas type, formatted as "xx.x°C" or "N/A".
     * Marks thermoelement as inactive if no valid data is found.
     */
    public string GetTemperatureTextForThermoelement(Thermoelement te, string canvasType)
    {
      Measurement? measurement = GetMeasurementForChannel(te.Channel);
      if (measurement == null) { markThermoelementAsInactive(te, canvasType); return "N/A"; }
      var entries = measurement.CachedEntries ??= ParseMeasurementEntries(measurement.Data);
      var entry = entries.FirstOrDefault(e => e.timestamp == GetTargetTimestamp(measurement));
      if (entry == null || entry.temperatures == null) { markThermoelementAsInactive(te, canvasType); return "N/A"; }

      int channelIndex = (te.Channel <= 10) ? te.Channel - 1 : te.Channel - 11;
      if (channelIndex < 0 || channelIndex >= entry.temperatures.Count) { markThermoelementAsInactive(te, canvasType); return "N/A"; }

      var value = entry.temperatures[channelIndex];
      if (value is JsonElement je)
      {
        if (je.ValueKind == JsonValueKind.Number)
          return $"{je.GetDouble():F1}°C";
        if (je.ValueKind == JsonValueKind.String)
        {
          var str = je.GetString();
          if (str == "BURNOUT")
          { markThermoelementAsInactive(te, canvasType); return "BURNOUT"; }
          if (double.TryParse(str, out var d))
            return $"{d:F1}°C";
        }
      }
      markThermoelementAsInactive(te, canvasType);
      return "N/A";
    }

    /*
     * markThermoelementAsInactive
     * Marks the given thermoelement as inactive and updates the database and in-memory settings.
     */
    private void markThermoelementAsInactive(Thermoelement te, string canvasType)
    {
      te.IsActive = false;
      databaseController.UpdateThermoelementActiveState(te.Id, te.IsActive);
      // Update the in-memory settings for the correct canvas assignment
      if (Settings.ThermoelementsByCanvas.TryGetValue(canvasType, out var thermoelements))
      {
        var teObj = thermoelements?.FirstOrDefault(x => x.Id == te.Id);
        if (teObj != null)
        {
          teObj.IsActive = te.IsActive;
        }
      }
      (Application.Current.MainWindow as MainWindow)?.UpdateStats();
    }

    /*
     * GetAverageTemperature
     * Calculates and returns the average temperature for a list of active thermoelements.
     * Returns "N/A" if no valid temperatures are found.
     */
    public string GetAverageTemperature(List<Thermoelement>? thermoelements)
    {
      if (thermoelements == null || thermoelements.Count == 0)
        return "N/A";

      var temps = new List<double>();
      foreach (var te in thermoelements.Where(te => te.IsActive))
      {
        var measurement = GetMeasurementForChannel(te.Channel);
        if (measurement == null) continue;

        var entries = measurement.CachedEntries ??= ParseMeasurementEntries(measurement.Data);
        var targetTimestamp = GetTargetTimestamp(measurement);
        if (targetTimestamp == null) continue;

        var entry = entries.FirstOrDefault(e => e.timestamp == targetTimestamp);
        if (entry == null || entry.temperatures == null) continue;

        int channelIndex = (te.Channel <= 10) ? te.Channel - 1 : te.Channel - 11;
        if (channelIndex < 0 || channelIndex >= entry.temperatures.Count) continue;

        var value = entry.temperatures[channelIndex];
        if (value is JsonElement je)
        {
          if (je.ValueKind == JsonValueKind.Number)
            temps.Add(je.GetDouble());
          else if (je.ValueKind == JsonValueKind.String && double.TryParse(je.GetString(), out var d))
            temps.Add(d);
        }
      }
      if (temps.Count == 0) return "N/A";
      return $"{temps.Average():F1}°C";
    }

    /*
     * GetMinMaxTemperature
     * Returns the minimum and maximum temperature for a list of active thermoelements.
     * Returns (null, null) if no valid temperatures are found.
     */
    public (double? min, double? max) GetMinMaxTemperature(List<Thermoelement>? thermoelements)
    {
      if (thermoelements == null || thermoelements.Count == 0)
        return (null, null);

      var temps = new List<double>();
      foreach (var te in thermoelements.Where(te => te.IsActive))
      {
        var measurement = GetMeasurementForChannel(te.Channel);
        // Ignore thermoelements without measurement data
        if (measurement == null) continue;

        var entries = measurement.CachedEntries ??= ParseMeasurementEntries(measurement.Data);
        var targetTimestamp = GetTargetTimestamp(measurement);
        // Ignore thermoelements without valid timestamp
        if (targetTimestamp == null) continue;

        var entry = entries.FirstOrDefault(e => e.timestamp == targetTimestamp);
        // Ignore thermoelements without valid entry
        if (entry == null || entry.temperatures == null) continue;

        int channelIndex = (te.Channel <= 10) ? te.Channel - 1 : te.Channel - 11;
        // Ignore thermoelements with invalid channel index
        if (channelIndex < 0 || channelIndex >= entry.temperatures.Count) continue;

        // Identify min and max temperatures in valid thermoelement entries
        var value = entry.temperatures[channelIndex];
        if (value is JsonElement je)
        {
          if (je.ValueKind == JsonValueKind.Number)
            temps.Add(je.GetDouble());
          else if (je.ValueKind == JsonValueKind.String && double.TryParse(je.GetString(), out var d))
            temps.Add(d);
        }
      }
      if (temps.Count == 0) return (null, null);
      return (temps.Min(), temps.Max());
    }

    /*
     * GetMeasurementForChannel
     * Returns the measurement object for a given channel ID (1-20).
     */
    private Measurement? GetMeasurementForChannel(int channelId)
    {
      if (channelId >= 1 && channelId <= 10)
        return Settings.Measurements1To10;
      if (channelId >= 11 && channelId <= 20)
        return Settings.Measurements11To20;
      return null;
    }

    /*
     * GetFirstTimestamp
     * Returns the first timestamp from a measurement's entries, or null if not available.
     */
    private DateTime? GetFirstTimestamp(Measurement measurement)
    {
      var entries = measurement.CachedEntries ??= ParseMeasurementEntries(measurement.Data);
      if (entries == null || entries.Count == 0)
        return null;
      if (DateTime.TryParse(entries[0].timestamp, out var dt))
        return dt;
      return null;
    }

    /*
     * GetTargetTimestamp
     * Returns the target timestamp for a measurement, offset by the current time offset.
     */
    public string? GetTargetTimestamp(Measurement measurement)
    {
      var start = GetFirstTimestamp(measurement);
      if (start == null) return null;
      var target = start.Value.Add(Settings.CurrentTimeOffset);
      return target.ToString("yyyy-MM-dd HH:mm:ss.0");
    }

    /*
     * ParseMeasurementEntries
     * Parses a JSON string into a list of MeasurementEntry objects.
     */
    private List<MeasurementEntry> ParseMeasurementEntries(string json)
    {
      return JsonSerializer.Deserialize<List<MeasurementEntry>>(json) ?? new List<MeasurementEntry>();
    }
  }
}
