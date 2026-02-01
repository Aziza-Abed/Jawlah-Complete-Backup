namespace FollowUp.Core.Enums;

// Worker types matching Al-Bireh Municipality departments
public enum WorkerType
{
    Sanitation = 0,      // صحة/نظافة - 100 workers, routine tasks by zone
    PublicWorks = 1,     // أشغال - 30 workers, groups of ~5, specific tasks
    Agriculture = 2,     // زراعة - 18 workers, teams of 3-4
    Maintenance = 3      // صيانة - general maintenance
}
