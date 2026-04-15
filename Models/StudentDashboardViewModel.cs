namespace Projectpath.Models
{
    public class StudentDashboardViewModel
    {
        public Assignment? Assignment { get; set; }
        public List<ProgressUpdate> Updates { get; set; } = new();
    }
}