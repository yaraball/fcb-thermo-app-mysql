namespace fcb_thermo_app.Models
{
  /*
   * DXFModel
   * Represents a DXF model with file content and identification properties.
   */
  public class DXFModel
  {
    /* Id: Primary key for the DXF model. */
    public int Id { get; set; }

    /* Name: Name of the DXF model. */
    public required string Name { get; set; }

    /* SerialNumber: Optional serial number for the DXF model. */
    public string? SerialNumber { get; set; }

    /* MainBodyFileContent: Blob content of the DXF file for Main Body. */
    public byte[]? MainBodyFileContent { get; set; }

    /* ReinforcementFileContent: Blob content of the DXF file for Reinforcement. */
    public byte[]? ReinforcementFileContent { get; set; }

    /* NameWithSerial: Returns the name with serial number if available. */
    public string NameWithSerial => string.IsNullOrWhiteSpace(SerialNumber) ? Name : $"{Name} ({SerialNumber})";
  }
}
