using netDxf;
using System.IO;
using System.Windows;
using System.Windows.Media;

using fcb_thermo_app.Models;

namespace fcb_thermo_app.Controllers
{
  /*
   * DxfController
   * Handles DXF model creation, validation, and geometry extraction for visualization.
   * Uses DatabaseController for storage and retrieval of DXF models.
   */
  public class DxfController
  {
    private readonly DatabaseController _dbController;

    /*
     * Constructor
     * dbController: DatabaseController instance for database operations.
     */
    public DxfController(DatabaseController dbController)
    {
      _dbController = dbController;
    }

    /*
     * CreateNewDxfModel
     * Creates a new DXF model entry in the database after validating uniqueness and file content.
     * name: Name of the DXF model.
     * serialNumber: Serial number (optional).
     * mainBodyFilePath: Path to main body DXF file (optional).
     * reinforcementFilePath: Path to reinforcement DXF file (optional).
     * Throws Exception if a duplicate exists or file validation fails.
     */
    public void CreateNewDxfModel(string name, string? serialNumber, string? mainBodyFilePath, string? reinforcementFilePath)
    {
      // Validate uniqueness of name and serial number
      var existingModels = _dbController.GetAllDXFModels();
      foreach (var existing in existingModels)
      {
        if (existing.Name == name && existing.SerialNumber == serialNumber)
        {
          throw new Exception($"A model with the same name and serial number already exists. Please choose a different combination.");
        }
      }

      // Read and validate DXF file content
      byte[]? mainBodyFileContent = ReadAndValidateDxfFile(mainBodyFilePath, "Main Body");
      byte[]? reinforcementFileContent = ReadAndValidateDxfFile(reinforcementFilePath, "Reinforcement");

      // Create and save the new model
      var newModel = new DXFModel
      {
        Name = name,
        SerialNumber = serialNumber,
        MainBodyFileContent = mainBodyFileContent,
        ReinforcementFileContent = reinforcementFileContent
      };

      _dbController.InsertDXFModel(newModel);
    }

    /*
     * ReadAndValidateDxfFile
     * Reads and validates a DXF file, returning its content as a byte array if valid.
     * filePath: Path to the DXF file.
     * fileType: Description for error messages.
     * Returns: Byte array of file content, or null if path is empty.
     * Throws Exception if file is invalid or cannot be read.
     */
    private byte[]? ReadAndValidateDxfFile(string? filePath, string fileType)
    {
      // Return null if no file path is provided
      if (string.IsNullOrWhiteSpace(filePath)) return null;

      try
      {
        byte[] fileContent = File.ReadAllBytes(filePath);

        // Validate the DXF file
        using (var stream = new MemoryStream(fileContent))
        {
          var dxf = DxfDocument.Load(stream);
        }

        return fileContent;
      }
      catch (Exception ex)
      {
        throw new Exception($"Invalid {fileType} DXF file: {ex.Message}");
      }
    }

    /*
     * GetDxfGeometry
     * Extracts geometry and bounds from a DXF model for visualization.
     * model: DXFModel to extract geometry from.
     * isReinforcement: Use reinforcement file content if true, else main body.
     * Returns: Tuple (Geometry, Rect) for drawing and bounds.
     */
    public (Geometry Geometry, Rect Bounds) GetDxfGeometry(DXFModel model, bool isReinforcement)
    {
      // Check if the model or the required file content is null
      byte[]? fileContent = isReinforcement ? model.ReinforcementFileContent : model.MainBodyFileContent;
      if (model == null || fileContent == null)
      {
        // Return empty geometry and bounds if no file content
        return (Geometry.Empty, Rect.Empty);
      }

      var geometryGroup = new GeometryGroup();

      using (var ms = new MemoryStream(fileContent))
      {
        var dxf = DxfDocument.Load(ms);

        // Add entity 'lines' to geometry
        foreach (var line in dxf.Entities.Lines)
        {
          geometryGroup.Children.Add(new LineGeometry(
              new Point(line.StartPoint.X, line.StartPoint.Y),
              new Point(line.EndPoint.X, line.EndPoint.Y)
          ));
        }

        // Add entity 'circles' to geometry
        foreach (var circle in dxf.Entities.Circles)
        {
          geometryGroup.Children.Add(new EllipseGeometry(
              new Point(circle.Center.X, circle.Center.Y),
              circle.Radius,
              circle.Radius
          ));
        }

        // Add entity 'polylines' to geometry
        foreach (var poly in dxf.Entities.Polylines2D)
        {
          if (poly.Vertexes.Count < 2) continue;
          var pathGeo = new PathGeometry();
          var pathFig = new PathFigure { StartPoint = new Point(poly.Vertexes[0].Position.X, poly.Vertexes[0].Position.Y) };

          for (int i = 1; i < poly.Vertexes.Count; i++)
          {
            pathFig.Segments.Add(new LineSegment(
                new Point(poly.Vertexes[i].Position.X, poly.Vertexes[i].Position.Y), true));
          }
          if (poly.IsClosed) pathFig.IsClosed = true;
          pathGeo.Figures.Add(pathFig);
          geometryGroup.Children.Add(pathGeo);
        }

        // Add entity 'arcs' to geometry
        foreach (var arc in dxf.Entities.Arcs)
        {
          double startAngleRad = arc.StartAngle * Math.PI / 180.0;
          double endAngleRad = arc.EndAngle * Math.PI / 180.0;

          Point startPoint = new Point(
              arc.Center.X + arc.Radius * Math.Cos(startAngleRad),
              arc.Center.Y + arc.Radius * Math.Sin(startAngleRad));
          Point endPoint = new Point(
              arc.Center.X + arc.Radius * Math.Cos(endAngleRad),
              arc.Center.Y + arc.Radius * Math.Sin(endAngleRad));

          var pathGeo = new PathGeometry();
          var pathFig = new PathFigure { StartPoint = startPoint };

          double angleDiff = arc.EndAngle - arc.StartAngle;
          if (angleDiff < 0) angleDiff += 360;
          bool isLargeArc = angleDiff > 180;

          pathFig.Segments.Add(new ArcSegment(
              endPoint, new Size(arc.Radius, arc.Radius), 0, isLargeArc, SweepDirection.Counterclockwise, true));

          pathGeo.Figures.Add(pathFig);
          geometryGroup.Children.Add(pathGeo);
        }
      }

      if (geometryGroup.Children.Count == 0) return (Geometry.Empty, Rect.Empty);

      var transformGroup = new TransformGroup();
      transformGroup.Children.Add(new ScaleTransform(1, -1));
      geometryGroup.Transform = transformGroup;

      return (geometryGroup, geometryGroup.GetFlattenedPathGeometry().Bounds);
    }

    /*
     * GetAllDXFModels
     * Returns all DXF models from the database. Used for dropdowns or lists.
     * Returns: List<DXFModel>
     */
    public List<DXFModel> GetAllDXFModels()
    {
      return _dbController.GetAllDXFModels();
    }

    /*
     * GetDxfModelByNameAndSerial
     * Retrieves a DXF model by its name and serial number.
     * name: Name of the DXF model.
     * serialNumber: Serial number (optional).
     * Returns: DXFModel if found, otherwise null.
     */
    public DXFModel? GetDxfModelByNameAndSerial(string name, string? serialNumber)
    {
      return _dbController.GetDxfModelByNameAndSerial(name, serialNumber);
    }
  }
}
