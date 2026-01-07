namespace fcb_thermo_app.Models
{
    /*
     * PointLayout
     * Represents a layout of points for a DXF model, including metadata and serialized data.
     */
    public class PointLayout
    {
        /* Id: Primary key for the point layout. */
        public int Id { get; set; }

        /* Name: Name of the point layout. */
        public string Name { get; set; } = string.Empty;

        /* Description: Description of the point layout. */
        public string Description { get; set; } = string.Empty;

        /* PointsData: JSON serialized list of points. */
        public string PointsData { get; set; } = string.Empty;

        /* CreatedAt: Date and time the layout was created. */
        public DateTime CreatedAt { get; set; }
    }
}
