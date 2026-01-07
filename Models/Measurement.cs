namespace fcb_thermo_app.Models
{
  /*
   * Measurement
   * Represents a measurement entry with file, channel, and data information.
   */
  public class Measurement
  {
    /* Id: Primary key for the measurement. */
    public int Id { get; set; }

    /* Filename: Filename for identification. */
    public required string Filename { get; set; }

    /* Channels: Channels included in the measurement. */
    public required string Channels { get; set; }

    /* Data: JSON string containing timestamps, temperatures, alarms. */
    public required string Data { get; set; }

    /* CachedEntries: Optional cached parsed entries for fast access. */
    public List<Services.TemperatureMappingService.MeasurementEntry>? CachedEntries { get; set; }
  }
}
