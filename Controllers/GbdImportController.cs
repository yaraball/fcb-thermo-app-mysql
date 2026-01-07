using System.IO;
using System.Text.Json;

using fcb_thermo_app.Models;

namespace fcb_thermo_app.Controllers
{
  /*
   * GbdImportController
   * Handles importing and parsing .GBD measurement files, extracting metadata and temperature data, and storing them in the database.
   */
  public class GbdImportController
  {
    private readonly DatabaseController _databaseController;

    /*
     * Constructor
     * databaseController: DatabaseController instance for database operations.
     */
    public GbdImportController(DatabaseController databaseController)
    {
      _databaseController = databaseController;
    }

    /*
     * ImportGbdFile
     * Imports a .GBD file, extracts metadata and temperature data, and stores the measurement in the database.
     * filePath: Path to the .GBD file.
     * channels: String representing the channels used in the measurement.
     * Returns: Measurement object with its database ID set.
     * Throws: FileNotFoundException if file does not exist, Exception if format is invalid or extraction fails.
     */
    public Measurement ImportGbdFile(string filePath, string channels)
    {
      try
      {
        // Validate file existence and format
        if (!File.Exists(filePath))
        {
          throw new FileNotFoundException($"The file '{filePath}' does not exist.");
        }

        if (!filePath.EndsWith(".GBD", StringComparison.OrdinalIgnoreCase))
        {
          throw new Exception($"Invalid file format. Expected a .GBD file, but got '{Path.GetExtension(filePath)}'.");
        }

        // Extract metadata from the file
        var (startTimestamp, samplingInterval, thermoelementCount, headerSize) = ExtractMetadata(filePath);

        // Read the file
        using (var reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
          // Extract the data grouped by timestamp
          var data = ExtractDataAsJson(reader, thermoelementCount, startTimestamp, samplingInterval, headerSize);

          // Create a new Measurement entry
          var measurement = new Measurement
          {
            Filename = Path.GetFileName(filePath),
            Channels = channels,
            Data = data
          };

          if (string.IsNullOrWhiteSpace(measurement.Filename))
          {
            throw new Exception("Invalid measurement: Filename is missing.");
          }

          if (string.IsNullOrWhiteSpace(measurement.Data))
          {
            throw new Exception("Invalid measurement: Data is missing.");
          }

          // Insert the measurement into the database
          int measurementId = _databaseController.InsertMeasurement(measurement);
          measurement.Id = measurementId;
          return measurement;
        }
      }
      catch (IOException ex)
      {
        // Handle file-related errors
        throw new Exception($"File error: {ex.Message}", ex);
      }
      catch (Exception ex)
      {
        // Handle other errors
        throw new Exception($"Import failed for file '{filePath}'. Error: {ex.Message}", ex);
      }
    }

    /*
     * ExtractMetadata
     * Extracts metadata from the header of a .GBD file, including start timestamp, sampling interval, thermoelement count, and header size.
     * filePath: Path to the .GBD file.
     * Returns: Tuple (DateTime startTimestamp, double samplingInterval, int thermoelementCount, int headerSize).
     * Throws: Exception if any required metadata is missing or invalid.
     */
    public (DateTime StartTimestamp, double SamplingInterval, int ThermoelementCount, int headerSize) ExtractMetadata(string filePath)
    {
      string[] lines = File.ReadAllLines(filePath);

      DateTime startTimestamp = DateTime.MinValue;
      double samplingInterval = 0;
      int thermoelementCount = 0;
      int headerSize = 0;

      // Parse each line to extract metadata
      foreach (string line in lines)
      {
        // Extract the start timestamp from $Time Trigger
        if (line.StartsWith("  Trigger", StringComparison.OrdinalIgnoreCase))
        {
          string timestampString = line.Split('=')[1].Trim();
          if (DateTime.TryParse(timestampString, out DateTime parsedTimestamp))
          {
            startTimestamp = parsedTimestamp;
          }
        }

        // Extract the sampling interval from $Data Sample
        if (line.StartsWith("  Sample", StringComparison.OrdinalIgnoreCase))
        {
          string sampleString = line.Split('=')[1].Trim().Replace("s", "");
          if (double.TryParse(sampleString, out double parsedSample))
          {
            samplingInterval = parsedSample;
            Settings.SamplingIntervalSeconds = samplingInterval;
          }
        }

        // Extract the number of thermoelements from $Data MaxCH
        if (line.StartsWith("  MaxCH", StringComparison.OrdinalIgnoreCase))
        {
          string maxChString = line.Split('=')[1].Trim();
          if (int.TryParse(maxChString, out int parsedMaxCh))
          {
            thermoelementCount = parsedMaxCh;
          }
        }

        // Extract the header size from HeaderSiz
        if (line.StartsWith("  HeaderSiz", StringComparison.OrdinalIgnoreCase))
        {
          string headerSizeString = line.Split('=')[1].Trim();
          if (int.TryParse(headerSizeString, out int parsedHeaderSize))
          {
            headerSize = parsedHeaderSize;
          }
        }
      }

      // Validate extracted metadata
      if (startTimestamp == DateTime.MinValue)
      {
        throw new Exception("Invalid metadata: Start timestamp is missing or incorrectly formatted.");
      }

      if (samplingInterval <= 0)
      {
        throw new Exception("Invalid metadata: Sampling interval must be greater than 0.");
      }

      if (thermoelementCount <= 0)
      {
        throw new Exception("Invalid metadata: Thermoelement count must be greater than 0.");
      }

      if (headerSize <= 0)
      {
        throw new Exception("Invalid metadata: Header size must be greater than 0.");
      }

      return (startTimestamp, samplingInterval, thermoelementCount, headerSize);
    }

    /*
     * ExtractDataAsJson
     * Reads binary temperature and alarm data from a .GBD file and serializes it as JSON.
     * reader: BinaryReader positioned at the start of the data section.
     * numberOfChannels: Number of thermoelement channels.
     * startTime: Start timestamp for the measurement data.
     * samplingInterval: Sampling interval in seconds.
     * headerSize: Size of the file header in bytes.
     * Returns: JSON string representing the extracted measurement data.
     * Throws: Exception if no data rows are found.
     */
    private string ExtractDataAsJson(BinaryReader reader, int numberOfChannels, DateTime startTime, double samplingInterval, int headerSize)
    {
      var data = new List<Dictionary<string, object>>();
      int rowNumber = 0;

      // Skip the header
      reader.BaseStream.Seek(headerSize, SeekOrigin.Begin);

      // Read binary data until the end of the stream
      while (reader.BaseStream.Position + (numberOfChannels * 2) + 4 <= reader.BaseStream.Length)
      {
        var row = new Dictionary<string, object>
        {
            { "timestamp", startTime.AddSeconds(rowNumber * samplingInterval).ToString("yyyy-MM-dd HH:mm:ss.f") },
            { "temperatures", new List<double>() },
            { "alarm1", 0 },
            { "alarmOut", 0 }
        };

        // Read temperature values for each channel
        for (int i = 0; i < numberOfChannels; i++)
        {
          byte[] rawBytes = reader.ReadBytes(2);

          // Convert from big-endian to little-endian
          if (BitConverter.IsLittleEndian)
          {
            Array.Reverse(rawBytes);
          }

          // Convert the bytes to a 16-bit signed integer
          short rawTemperature = BitConverter.ToInt16(rawBytes, 0);

          // Apply scaling (divide by 10.0)
          double temperature = rawTemperature / 10.0;
          ((List<double>)row["temperatures"]).Add(temperature);
        }

        // Read Alarm1 and AlarmOut values (2 bytes each)
        byte[] alarm1Bytes = reader.ReadBytes(2);
        byte[] alarmOutBytes = reader.ReadBytes(2);

        // Convert to integers
        row["alarm1"] = BitConverter.ToInt16(alarm1Bytes, 0);
        row["alarmOut"] = BitConverter.ToInt16(alarmOutBytes, 0);

        // Add the row to the data list
        data.Add(row);
        rowNumber++;
      }

      // Validate extracted data
      if (data.Count == 0)
      {
        throw new Exception("Invalid data: No rows found.");
      }

      // Serialize the data to JSON and return it
      return JsonSerializer.Serialize(data);
    }
  }
}
