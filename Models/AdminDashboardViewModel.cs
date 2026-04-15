namespace Projectpath.Models
{
    public class AdminDashboardViewModel
    {
        public int PendingProjects { get; set; }
        public int ApprovedProjects { get; set; }
        public int ActiveAssignments { get; set; }
        public int CompletedProjects { get; set; }

        public List<Project> RecentPendingProjects { get; set; } = new();
        public List<string> RecentActivities { get; set; } = new();
    }
}