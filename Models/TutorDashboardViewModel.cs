namespace Projectpath.Models
{
    public class TutorDashboardViewModel
    {
        public int AssignedStudentsCount { get; set; }
        public int ActiveProjectsCount { get; set; }
        public int PendingUpdatesCount { get; set; }

        public List<Assignment> Assignments { get; set; } = new();
    }
}