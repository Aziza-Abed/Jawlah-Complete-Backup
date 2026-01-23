namespace FollowUp.Core.Enums;

// types of tasks that can be assigned to workers
public enum TaskType
{
    GarbageCollection = 0,       // garbage collection from containers
    StreetSweeping = 1,          // street sweeping and cleaning
    ContainerMaintenance = 2,    // container maintenance and cleaning
    RepairMaintenance = 3,       // repair and maintenance work
    PublicSpaceCleaning = 4,     // public space cleaning (parks, gardens)
    Inspection = 5,              // inspection and reporting
    Other = 99                   // other tasks (99 allows future expansion 6-98)
}
