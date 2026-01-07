using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Text.Json;

using fcb_thermo_app.Models;

namespace fcb_thermo_app.Controllers
{
  /*
   * DatabaseController
   * Handles all database operations, including initialization, CRUD for models, measurements, assignments, thermoelements, and point layouts.
   */
  public class DatabaseController
  {
    private string connectionString;

    /*
     * Constructor
     * Loads the connection string from appsettings.json.
     */
    public DatabaseController()
    {
      // Load connection string from configuration file
      var config = new ConfigurationBuilder()
          .SetBasePath(AppContext.BaseDirectory)
          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
          .Build();

      connectionString = config.GetConnectionString("MySql") ?? throw new Exception("MySQL connection string not found in configuration.");
    }

    /*
     * GetConnection
     * Returns a new MySqlConnection using the loaded connection string.
     * Throws: Exception if connection cannot be created.
     */
    public MySqlConnection GetConnection()
    {
      try
      {
        return new MySqlConnection(connectionString);
      }
      catch (MySqlException ex)
      {
        throw new Exception("Failed to create a database connection. Please check the database and its configuration.", ex);
      }
    }

    /*
     * InitializeDatabase
     * Creates all required tables in the database if they do not exist.
     */
    public void InitializeDatabase()
    {
      using (var connection = GetConnection())
      {
        connection.Open();

        // DXFModels table
        string createDXFModelsTable = @"
          CREATE TABLE IF NOT EXISTS DXFModels (
            Id INT PRIMARY KEY AUTO_INCREMENT,
            Name VARCHAR(255) NOT NULL,
            SerialNumber VARCHAR(255),
            MainBodyFileContent LONGBLOB,
            ReinforcementFileContent LONGBLOB,
            UNIQUE(Name, SerialNumber)
          );";
        new MySqlCommand(createDXFModelsTable, connection).ExecuteNonQuery();

        // Measurements table
        string createMeasurementsTable = @"
          CREATE TABLE IF NOT EXISTS Measurements (
            Id INT PRIMARY KEY AUTO_INCREMENT,
            Filename VARCHAR(255) NOT NULL,
            Channels TEXT NOT NULL,
            Data TEXT NOT NULL
          );";
        new MySqlCommand(createMeasurementsTable, connection).ExecuteNonQuery();

        // CanvasAssignments table
        string createCanvasAssignmentsTable = @"
          CREATE TABLE IF NOT EXISTS CanvasAssignments (
            Id INT PRIMARY KEY AUTO_INCREMENT,
            DXFModelId INT NOT NULL,
            Type VARCHAR(255) NOT NULL,
            Measurement1To10Id INT NOT NULL DEFAULT -1,
            Measurement11To20Id INT NOT NULL DEFAULT -1,
            PyrometerPosition INT NOT NULL DEFAULT -1,
            PyrometerNumber INT NOT NULL DEFAULT -1,
            PerformanceSettings JSON NOT NULL,
            Thermoelements JSON NOT NULL,
            FOREIGN KEY (DXFModelId) REFERENCES DXFModels(Id)
          );";
        new MySqlCommand(createCanvasAssignmentsTable, connection).ExecuteNonQuery();

        // Thermoelements table
        string createThermoelementsTable = @"
          CREATE TABLE IF NOT EXISTS Thermoelements (
            Id INT PRIMARY KEY AUTO_INCREMENT,
            RelativeX DOUBLE NOT NULL,
            RelativeY DOUBLE NOT NULL,
            Channel INT NOT NULL,
            IsActive TINYINT(1) NOT NULL,
            Note VARCHAR(255) DEFAULT ''
          );";
        new MySqlCommand(createThermoelementsTable, connection).ExecuteNonQuery();

        // PointLayouts table
        string createPointLayoutsTable = @"
        CREATE TABLE IF NOT EXISTS PointLayouts (
            Id INT PRIMARY KEY AUTO_INCREMENT,
            Name VARCHAR(255) NOT NULL UNIQUE,
            Description VARCHAR(255) DEFAULT '',
            PointsData VARCHAR(255) NOT NULL,
            CreatedAt VARCHAR(255) NOT NULL
        );";
        new MySqlCommand(createPointLayoutsTable, connection).ExecuteNonQuery();
      }
    }

    /*
     * InsertDXFModel
     * Inserts a new DXFModel into the DXFModels table.
     * model: DXFModel object to insert.
     * Throws: Exception if insertion fails.
     */
    public void InsertDXFModel(DXFModel model)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = "INSERT INTO DXFModels (Name, SerialNumber, MainBodyFileContent, ReinforcementFileContent) VALUES (@name, @serialNumber, @mainBodyFileContent, @reinforcementFileContent)";
        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@name", model.Name);
          command.Parameters.AddWithValue("@serialNumber", model.SerialNumber ?? (object)DBNull.Value);
          command.Parameters.AddWithValue("@mainBodyFileContent", model.MainBodyFileContent ?? (object)DBNull.Value);
          command.Parameters.AddWithValue("@reinforcementFileContent", model.ReinforcementFileContent ?? (object)DBNull.Value);
          int rowsAffected = command.ExecuteNonQuery();
          if (rowsAffected == 0)
          {
            throw new Exception("Failed to insert DXFModel into the database.");
          }
        }
      }
    }

    /*
     * GetAllDXFModels
     * Fetches all DXFModels from the database.
     * Returns: List<DXFModel> containing all models.
     */
    public List<DXFModel> GetAllDXFModels()
    {
      var models = new List<DXFModel>();
      using (var connection = GetConnection())
      {
        connection.Open();
        using (var command = new MySqlCommand("SELECT Id, Name, SerialNumber, MainBodyFileContent, ReinforcementFileContent FROM DXFModels", connection))
        {
          using (var reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              models.Add(new DXFModel
              {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                SerialNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                MainBodyFileContent = reader.IsDBNull(3) ? null : (byte[])reader["MainBodyFileContent"],
                ReinforcementFileContent = reader.IsDBNull(4) ? null : (byte[])reader["ReinforcementFileContent"]
              });
            }
          }
        }
      }
      return models;
    }

    /*
     * GetDxfModelByNameAndSerial
     * Fetches a DXFModel by its name and serial number.
     * name: Name of the DXFModel.
     * serialNumber: Serial number (optional).
     * Returns: DXFModel if found, otherwise null.
     */
    public DXFModel? GetDxfModelByNameAndSerial(string name, string? serialNumber)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = serialNumber == null
            ? "SELECT Id, Name, SerialNumber, MainBodyFileContent, ReinforcementFileContent FROM DXFModels WHERE Name = @name AND SerialNumber IS NULL LIMIT 1"
            : "SELECT Id, Name, SerialNumber, MainBodyFileContent, ReinforcementFileContent FROM DXFModels WHERE Name = @name AND SerialNumber = @serialNumber LIMIT 1";

        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@name", name);
          if (serialNumber != null)
            command.Parameters.AddWithValue("@serialNumber", serialNumber);

          using (var reader = command.ExecuteReader())
          {
            if (reader.Read())
            {
              return new DXFModel
              {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                SerialNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                MainBodyFileContent = reader.IsDBNull(3) ? null : (byte[])reader["MainBodyFileContent"],
                ReinforcementFileContent = reader.IsDBNull(4) ? null : (byte[])reader["ReinforcementFileContent"]
              };
            }
          }
        }
      }
      return null;
    }

    /*
     * InsertMeasurement
     * Inserts a new Measurement into the Measurements table.
     * measurement: Measurement object to insert.
     * Returns: The database ID of the inserted measurement.
     */
    public int InsertMeasurement(Measurement measurement)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = @"
            INSERT INTO Measurements (Filename, Channels, Data)
            VALUES (@filename, @channels, @data);";
        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@filename", measurement.Filename);
          command.Parameters.AddWithValue("@channels", measurement.Channels);
          command.Parameters.AddWithValue("@data", measurement.Data);
          command.ExecuteNonQuery();
          using (var idCommand = new MySqlCommand("SELECT LAST_INSERT_ID();", connection))
          {
            return Convert.ToInt32(idCommand.ExecuteScalar());
          }
        }
      }
    }

    /*
     * GetMeasurementById
     * Fetches a Measurement by its ID.
     * id: Database ID of the measurement.
     * Returns: Measurement object if found, otherwise null.
     */
    public Measurement? GetMeasurementById(int id)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = "SELECT Id, Filename, Channels, Data FROM Measurements WHERE Id = @id LIMIT 1";
        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@id", id);
          using (var reader = command.ExecuteReader())
          {
            if (reader.Read())
            {
              return new Measurement
              {
                Id = reader.GetInt32(0),
                Filename = reader.GetString(1),
                Channels = reader.GetString(2),
                Data = reader.GetString(3)
              };
            }
          }
        }
      }
      return null;
    }

    /*
     * DeleteMeasurementById
     * Deletes a Measurement from the database by its ID.
     * id: Database ID of the measurement to delete.
     */
    public void DeleteMeasurementById(int id)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        var command = new MySqlCommand("DELETE FROM Measurements WHERE Id = @id", connection);
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
      }
    }

    /*
     * InsertCanvasAssignment
     * Inserts a new CanvasAssignment into the CanvasAssignments table.
     * canvas: CanvasAssignment object to insert.
     * Returns: The database ID of the inserted assignment.
     */
    public int InsertCanvasAssignment(CanvasAssignment canvas)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = @"
          INSERT INTO CanvasAssignments (DXFModelId, Type, Measurement1To10Id, Measurement11To20Id, PyrometerPosition, PyrometerNumber, PerformanceSettings, Thermoelements)
          VALUES (@dxfModelId, @type, @measurement1To10Id, @measurement11To20Id, @pyrometerPosition, @pyrometerNumber, @performanceSettings, @thermoelements);";
        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@dxfModelId", canvas.DXFModelId);
          command.Parameters.AddWithValue("@type", canvas.Type);
          command.Parameters.AddWithValue("@measurement1To10Id", canvas.Measurement1To10Id);
          command.Parameters.AddWithValue("@measurement11To20Id", canvas.Measurement11To20Id);
          command.Parameters.AddWithValue("@pyrometerPosition", canvas.PyrometerPosition);
          command.Parameters.AddWithValue("@pyrometerNumber", canvas.PyrometerNumber);
          command.Parameters.AddWithValue("@performanceSettings", JsonSerializer.Serialize(canvas.PerformanceSettings));
          command.Parameters.AddWithValue("@thermoelements", JsonSerializer.Serialize(canvas.Thermoelements));
          command.ExecuteNonQuery();
          using (var idCommand = new MySqlCommand("SELECT LAST_INSERT_ID();", connection))
          {
            return Convert.ToInt32(idCommand.ExecuteScalar());
          }
        }
      }
    }

    /*
     * DeleteCanvasAssignmentById
     * Deletes a CanvasAssignment from the database by its ID.
     * id: Database ID of the assignment to delete.
     */
    public void DeleteCanvasAssignmentById(int id)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        var command = new MySqlCommand("DELETE FROM CanvasAssignments WHERE Id = @id", connection);
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
      }
    }

    /*
     * GetAllCanvasAssignments
     * Fetches all CanvasAssignments from the database.
     * Returns: List<CanvasAssignment> containing all assignments.
     */
    public List<CanvasAssignment> GetAllCanvasAssignments()
    {
      var assignments = new List<CanvasAssignment>();
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = "SELECT Id, DXFModelId, Type, Measurement1To10Id, Measurement11To20Id, PyrometerPosition, PyrometerNumber, PerformanceSettings, Thermoelements FROM CanvasAssignments";
        using (var command = new MySqlCommand(query, connection))
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            assignments.Add(new CanvasAssignment
            {
              Id = reader.GetInt32(0),
              DXFModelId = reader.GetInt32(1),
              Type = reader.GetString(2),
              Measurement1To10Id = reader.GetInt32(3),
              Measurement11To20Id = reader.GetInt32(4),
              PyrometerPosition = reader.GetInt32(5),
              PyrometerNumber = reader.GetInt32(6),
              PerformanceSettings = JsonSerializer.Deserialize<List<int>>(reader.GetString(7)) ?? new List<int>(),
              Thermoelements = JsonSerializer.Deserialize<List<int>>(reader.GetString(8)) ?? new List<int>()
            });
          }
        }
      }
      return assignments;
    }

    /*
     * UpdateCanvasAssignmentThermoelements
     * Updates the Thermoelements field of a CanvasAssignment.
     * assignmentId: Database ID of the assignment.
     * thermoelementIds: List of thermoelement IDs to set.
     */
    public void UpdateCanvasAssignmentThermoelements(int assignmentId, List<int> thermoelementIds)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = "UPDATE CanvasAssignments SET Thermoelements = @thermoelements WHERE Id = @id";
        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@thermoelements", JsonSerializer.Serialize(thermoelementIds));
          command.Parameters.AddWithValue("@id", assignmentId);
          command.ExecuteNonQuery();
        }
      }
    }

    /*
     * InsertThermoelement
     * Inserts a new Thermoelement into the Thermoelements table.
     * te: Thermoelement object to insert.
     * Returns: The database ID of the inserted thermoelement.
     */
    public int InsertThermoelement(Thermoelement te)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = @"
            INSERT INTO Thermoelements (RelativeX, RelativeY, Channel, IsActive, Note)
            VALUES (@relativeX, @relativeY, @channel, @isActive, @note);";
        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@relativeX", te.RelativeX);
          command.Parameters.AddWithValue("@relativeY", te.RelativeY);
          command.Parameters.AddWithValue("@channel", te.Channel);
          command.Parameters.AddWithValue("@isActive", te.IsActive);
          command.Parameters.AddWithValue("@note", te.Note ?? "");
          command.ExecuteNonQuery();
          using (var idCommand = new MySqlCommand("SELECT LAST_INSERT_ID();", connection))
          {
            return Convert.ToInt32(idCommand.ExecuteScalar());
          }
        }
      }
    }

    /*
     * DeleteThermoelementsByIds
     * Deletes multiple Thermoelements from the database by their IDs.
     * ids: List of thermoelement IDs to delete.
     */
    public void DeleteThermoelementsByIds(List<int> ids)
    {
      if (ids == null || ids.Count == 0)
        return;
      using (var connection = GetConnection())
      {
        connection.Open();
        // Build parameterized IN clause
        var parameters = string.Join(",", ids.Select((id, i) => $"@id{i}"));
        string query = $"DELETE FROM Thermoelements WHERE Id IN ({parameters})";
        using (var command = new MySqlCommand(query, connection))
        {
          for (int i = 0; i < ids.Count; i++)
            command.Parameters.AddWithValue($"@id{i}", ids[i]);
          command.ExecuteNonQuery();
        }
      }
    }

    /*
     * GetThermoelementsForAssignment
     * Fetches Thermoelements by a list of IDs (for a specific assignment).
     * ids: List of thermoelement IDs.
     * Returns: List<Thermoelement> containing the matching thermoelements.
     */
    public List<Thermoelement> GetThermoelementsForAssignment(List<int> ids)
    {
      var thermoelements = new List<Thermoelement>();
      if (ids == null || ids.Count == 0)
        return thermoelements;

      using (var connection = GetConnection())
      {
        connection.Open();
        // Build parameterized IN clause
        var parameters = string.Join(",", ids.Select((id, i) => $"@id{i}"));
        string query = $"SELECT Id, RelativeX, RelativeY, Channel, IsActive, Note FROM Thermoelements WHERE Id IN ({parameters})";
        using (var command = new MySqlCommand(query, connection))
        {
          for (int i = 0; i < ids.Count; i++)
            command.Parameters.AddWithValue($"@id{i}", ids[i]);

          using (var reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              thermoelements.Add(new Thermoelement
              {
                Id = reader.GetInt32(0),
                RelativeX = reader.GetDouble(1),
                RelativeY = reader.GetDouble(2),
                Channel = reader.GetInt32(3),
                IsActive = reader.GetBoolean(4),
                Note = reader.IsDBNull(5) ? "" : reader.GetString(5)
              });
            }
          }
        }
      }
      return thermoelements;
    }

    /*
     * UpdateThermoelement
     * Updates the channel, active state, and note of a Thermoelement.
     * thermoelementId: Database ID of the thermoelement.
     * channel: Channel number to set.
     * isActive: Whether the thermoelement is active.
     * note: Note string to set.
     */
    public void UpdateThermoelement(int thermoelementId, int channel, bool isActive, string note)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = @" UPDATE Thermoelements SET Channel = @channel, IsActive = @isActive, Note = @note WHERE Id = @id;";

        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@channel", channel);
          command.Parameters.AddWithValue("@isActive", isActive);
          command.Parameters.AddWithValue("@note", note);
          command.Parameters.AddWithValue("@id", thermoelementId);

          command.ExecuteNonQuery();
        }
      }
    }

    /*
     * UpdateThermoelementPosition
     * Updates the position (RelativeX, RelativeY) of a Thermoelement.
     * thermoelementId: Database ID of the thermoelement.
     * relativeX: New X position.
     * relativeY: New Y position.
     */
    public void UpdateThermoelementPosition(int thermoelementId, double relativeX, double relativeY)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = @" UPDATE Thermoelements SET RelativeX = @x, RelativeY = @y WHERE Id = @id;";

        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@x", relativeX);
          command.Parameters.AddWithValue("@y", relativeY);
          command.Parameters.AddWithValue("@id", thermoelementId);

          command.ExecuteNonQuery();
        }
      }
    }

    /*
     * UpdateThermoelementActiveState
     * Updates the IsActive state of a Thermoelement.
     * thermoelementId: Database ID of the thermoelement.
     * isActive: New active state.
     */
    public void UpdateThermoelementActiveState(int thermoelementId, bool isActive)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = @" UPDATE Thermoelements SET IsActive = @isActive WHERE Id = @id;";

        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@isActive", isActive);
          command.Parameters.AddWithValue("@id", thermoelementId);

          command.ExecuteNonQuery();
        }
      }
    }

    /*
     * GetLastUsedPerformanceSettings
     * Fetches the last used PerformanceSettings for a given canvas type and DXFModelId.
     * canvasType: Type of the canvas assignment.
     * dxfModelId: Database ID of the DXFModel.
     * Returns: List<int> of performance settings, or empty list if none found.
     */
    public List<int> GetLastUsedPerformanceSettings(string canvasType, int dxfModelId)
    {
      using (var connection = GetConnection())
      {
        connection.Open();

        // Query to fetch the last CanvasAssignment for the given type and DXFModelId
        string query = @"
          SELECT PerformanceSettings
            FROM CanvasAssignments
            WHERE Type = @type AND DXFModelId = @dxfModelId
            ORDER BY Id DESC
            LIMIT 1";

        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@type", canvasType);
          command.Parameters.AddWithValue("@dxfModelId", dxfModelId);

          var result = command.ExecuteScalar();
          if (result != null && result is string jsonString && !string.IsNullOrWhiteSpace(jsonString))
          {
            // Deserialize the JSON string into a list of integers
            return JsonSerializer.Deserialize<List<int>>(jsonString) ?? new List<int>();
          }
        }
      }

      // Return an empty list if no matching entry is found
      return new List<int>();
    }

    /*
     * GetThermoelementsByCanvasAssignment
     * Fetches Thermoelements by a list of IDs (for a specific canvas assignment).
     * ids: List of thermoelement IDs.
     * Returns: List<Thermoelement> containing the matching thermoelements.
     */
    public List<Thermoelement> GetThermoelementsByCanvasAssignment(List<int> ids)
    {
      var thermoelements = new List<Thermoelement>();
      if (ids == null || ids.Count == 0)
        return thermoelements;

      using (var connection = GetConnection())
      {
        connection.Open();
        // Build parameterized IN clause
        var parameters = string.Join(",", ids.Select((id, i) => $"@id{i}"));
        string query = $"SELECT Id, RelativeX, RelativeY, Channel, IsActive, Note FROM Thermoelements WHERE Id IN ({parameters})";
        using (var command = new MySqlCommand(query, connection))
        {
          for (int i = 0; i < ids.Count; i++)
            command.Parameters.AddWithValue($"@id{i}", ids[i]);

          using (var reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              thermoelements.Add(new Thermoelement
              {
                Id = reader.GetInt32(0),
                RelativeX = reader.GetDouble(1),
                RelativeY = reader.GetDouble(2),
                Channel = reader.GetInt32(3),
                IsActive = reader.GetBoolean(4),
                Note = reader.IsDBNull(5) ? "" : reader.GetString(5)
              });
            }
          }
        }
      }
      return thermoelements;
    }

    /*
     * InsertPointLayout
     * Inserts a new PointLayout into the PointLayouts table.
     * layout: PointLayout object to insert.
     * Returns: The database ID of the inserted point layout.
     */
    public int InsertPointLayout(PointLayout layout)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = @"
            INSERT INTO PointLayouts (Name, Description, PointsData, CreatedAt)
            VALUES (@name, @description, @pointsData, @createdAt);
            SELECT last_insert_rowid();";
        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@name", layout.Name);
          command.Parameters.AddWithValue("@description", layout.Description ?? "");
          command.Parameters.AddWithValue("@pointsData", layout.PointsData);
          command.Parameters.AddWithValue("@createdAt", layout.CreatedAt.ToString("o"));
          return Convert.ToInt32(command.ExecuteScalar());
        }
      }
    }

    /*
     * GetAllPointLayouts
     * Fetches all PointLayouts from the database.
     * Returns: List<PointLayout> containing all point layouts.
     */
    public List<PointLayout> GetAllPointLayouts()
    {
      var layouts = new List<PointLayout>();
      using (var connection = GetConnection())
      {
        connection.Open();
        using (var command = new MySqlCommand("SELECT Id, Name, Description, PointsData, CreatedAt FROM PointLayouts ORDER BY CreatedAt DESC", connection))
        {
          using (var reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              layouts.Add(new PointLayout
              {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                PointsData = reader.GetString(3),
                CreatedAt = DateTime.Parse(reader.GetString(4))
              });
            }
          }
        }
      }
      return layouts;
    }

    /*
     * GetPointLayoutByName
     * Fetches a PointLayout by its name.
     * name: Name of the point layout.
     * Returns: PointLayout object if found, otherwise null.
     */
    public PointLayout? GetPointLayoutByName(string name)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = "SELECT Id, Name, Description, PointsData, CreatedAt FROM PointLayouts WHERE Name = @name LIMIT 1";
        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@name", name);
          using (var reader = command.ExecuteReader())
          {
            if (reader.Read())
            {
              return new PointLayout
              {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                PointsData = reader.GetString(3),
                CreatedAt = DateTime.Parse(reader.GetString(4))
              };
            }
          }
        }
      }
      return null;
    }

    /*
     * DeletePointLayout
     * Deletes a PointLayout from the database by its ID.
     * id: Database ID of the point layout to delete.
     */
    public void DeletePointLayout(int id)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = "DELETE FROM PointLayouts WHERE Id = @id";
        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@id", id);
          command.ExecuteNonQuery();
        }
      }
    }

    /*
     * GetLastUsedPyrometerPosition
     * Fetches the last used PyrometerPosition and PyrometerNumber for a given canvas type and DXFModelId.
     * canvasType: Type of the canvas assignment.
     * dxfModelId: Database ID of the DXFModel.
     * Returns: Tuple (PyrometerPosition, PyrometerNumber), or (-1, -1) if none found.
     */
    public (int PyrometerPosition, int PyrometerNumber) GetLastUsedPyrometerPosition(string canvasType, int dxfModelId)
    {
      using (var connection = GetConnection())
      {
        connection.Open();
        string query = @"
        SELECT PyrometerPosition, PyrometerNumber
        FROM CanvasAssignments
        WHERE Type = @type AND DXFModelId = @dxfModelId
        ORDER BY Id DESC
        LIMIT 1";
        using (var command = new MySqlCommand(query, connection))
        {
          command.Parameters.AddWithValue("@type", canvasType);
          command.Parameters.AddWithValue("@dxfModelId", dxfModelId);
          using (var reader = command.ExecuteReader())
          {
            if (reader.Read())
            {
              int position = reader.IsDBNull(0) ? -1 : reader.GetInt32(0);
              int number = reader.IsDBNull(1) ? -1 : reader.GetInt32(1);
              return (position, number);
            }
          }
        }
      }
      return (-1, -1);
    }
  }
}
