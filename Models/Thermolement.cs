namespace fcb_thermo_app.Models
{
  /*
   * Thermoelement
   * Represents a thermoelement sensor with position, channel, and state information.
   */
  public class Thermoelement
  {
    /* Id: Primary key for the thermoelement. */
    public int Id { get; set; }

    /* RelativeX: Relative position (0 to 1) in X direction. */
    public required double RelativeX { get; set; }

    /* RelativeY: Relative position (0 to 1) in Y direction. */
    public required double RelativeY { get; set; }

    /* Channel: Channel number of the thermoelement. */
    public required int Channel { get; set; }

    /* IsActive: Indicates if the thermoelement is active. */
    public required bool IsActive { get; set; }

    /* Note: Optional note for the thermoelement. */
    public string Note { get; set; } = string.Empty;
  }
}
