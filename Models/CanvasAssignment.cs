namespace fcb_thermo_app.Models
{
  /*
   * CanvasAssignment
   * Represents an assignment of measurements and thermoelements to a DXF model canvas.
   */
  public class CanvasAssignment
  {
    /* Id: Primary key for the canvas assignment. */
    public int Id { get; set; }

    /* DXFModelId: Foreign key to the assigned DXFModel. */
    public int DXFModelId { get; set; }

    /* Type: Canvas type ("MainTop", "MainBottom", "ReinfTop", "ReinfBottom"). */
    public required string Type { get; set; }

    /* Measurement1To10Id: Maps to Measurement.Id, default -1 if none selected. */
    public int Measurement1To10Id { get; set; }

    /* Measurement11To20Id: Maps to Measurement.Id, default -1 if none selected. */
    public int Measurement11To20Id { get; set; }

    /* PyrometerPosition: Position of the pyrometer, -1 if none selected. */
    public int PyrometerPosition { get; set; }

    /* PyrometerNumber: Number of the pyrometer, -1 for ReinfBottom. */
    public int PyrometerNumber { get; set; } = -1;

    /* PerformanceSettings: List of performance settings IDs. */
    public List<int> PerformanceSettings { get; set; } = new();

    /* Thermoelements: List of thermoelement IDs assigned to this canvas. */
    public List<int> Thermoelements { get; set; } = new();
  }
}
