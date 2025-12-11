using AutoMapper;
using Jawlah.Core.DTOs.Attendance;
using Jawlah.Core.DTOs.Issues;
using Jawlah.Core.DTOs.Notifications;
using Jawlah.Core.DTOs.Tasks;
using Jawlah.Core.DTOs.Users;
using Jawlah.Core.DTOs.Zones;
using Jawlah.Core.Entities;
using TaskEntity = Jawlah.Core.Entities.Task;

namespace Jawlah.API.Mappings;

/// <summary>
/// AutoMapper configuration profile for entity-to-DTO mappings
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserResponse>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
            .ForMember(dest => dest.WorkerType, opt => opt.MapFrom(src => src.WorkerType))
            .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.LastLoginAt, opt => opt.MapFrom(src => src.LastLoginAt));

        // Task mappings
        CreateMap<TaskEntity, TaskResponse>()
            .ForMember(dest => dest.TaskId, opt => opt.MapFrom(src => src.TaskId))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.AssignedToUserId, opt => opt.MapFrom(src => src.AssignedToUserId))
            .ForMember(dest => dest.AssignedToUserName, opt => opt.MapFrom(src => src.AssignedToUser.FullName))
            .ForMember(dest => dest.AssignedByUserId, opt => opt.MapFrom(src => src.AssignedByUserId))
            .ForMember(dest => dest.ZoneId, opt => opt.MapFrom(src => src.ZoneId))
            .ForMember(dest => dest.ZoneName, opt => opt.MapFrom(src => src.Zone != null ? src.Zone.ZoneName : null))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.DueDate))
            .ForMember(dest => dest.StartedAt, opt => opt.MapFrom(src => src.StartedAt))
            .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src => src.CompletedAt))
            .ForMember(dest => dest.LocationDescription, opt => opt.MapFrom(src => src.LocationDescription))
            .ForMember(dest => dest.CompletionNotes, opt => opt.MapFrom(src => src.CompletionNotes))
            .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src => src.PhotoUrl))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Latitude))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Longitude))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        // Issue mappings
        CreateMap<Issue, IssueResponse>()
            .ForMember(dest => dest.IssueId, opt => opt.MapFrom(src => src.IssueId))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Severity, opt => opt.MapFrom(src => src.Severity))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.ReportedByUserId, opt => opt.MapFrom(src => src.ReportedByUserId))
            .ForMember(dest => dest.ZoneId, opt => opt.MapFrom(src => src.ZoneId))
            .ForMember(dest => dest.LocationDescription, opt => opt.MapFrom(src => src.LocationDescription))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Latitude))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Longitude))
            .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src => src.PhotoUrl))
            .ForMember(dest => dest.ResolutionNotes, opt => opt.MapFrom(src => src.ResolutionNotes))
            .ForMember(dest => dest.ReportedAt, opt => opt.MapFrom(src => src.ReportedAt))
            .ForMember(dest => dest.ResolvedAt, opt => opt.MapFrom(src => src.ResolvedAt));

        // Attendance mappings
        CreateMap<Attendance, AttendanceResponse>()
            .ForMember(dest => dest.AttendanceId, opt => opt.MapFrom(src => src.AttendanceId))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName))
            .ForMember(dest => dest.ZoneId, opt => opt.MapFrom(src => src.ZoneId))
            .ForMember(dest => dest.ZoneName, opt => opt.MapFrom(src => src.Zone != null ? src.Zone.ZoneName : null))
            .ForMember(dest => dest.CheckInEventTime, opt => opt.MapFrom(src => src.CheckInEventTime))
            .ForMember(dest => dest.CheckOutEventTime, opt => opt.MapFrom(src => src.CheckOutEventTime))
            .ForMember(dest => dest.CheckInLatitude, opt => opt.MapFrom(src => src.CheckInLatitude))
            .ForMember(dest => dest.CheckInLongitude, opt => opt.MapFrom(src => src.CheckInLongitude))
            .ForMember(dest => dest.CheckOutLatitude, opt => opt.MapFrom(src => src.CheckOutLatitude))
            .ForMember(dest => dest.CheckOutLongitude, opt => opt.MapFrom(src => src.CheckOutLongitude))
            .ForMember(dest => dest.IsValidated, opt => opt.MapFrom(src => src.IsValidated))
            .ForMember(dest => dest.ValidationMessage, opt => opt.MapFrom(src => src.ValidationMessage))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.WorkDuration, opt => opt.MapFrom(src => src.WorkDuration))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CheckInSyncTime));

        // Notification mappings
        CreateMap<Notification, NotificationResponse>()
            .ForMember(dest => dest.NotificationId, opt => opt.MapFrom(src => src.NotificationId))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => src.IsRead))
            .ForMember(dest => dest.IsSent, opt => opt.MapFrom(src => src.IsSent))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.SentAt, opt => opt.MapFrom(src => src.SentAt))
            .ForMember(dest => dest.ReadAt, opt => opt.MapFrom(src => src.ReadAt));

        // Zone mappings
        CreateMap<Zone, ZoneResponse>()
            .ForMember(dest => dest.ZoneId, opt => opt.MapFrom(src => src.ZoneId))
            .ForMember(dest => dest.ZoneName, opt => opt.MapFrom(src => src.ZoneName))
            .ForMember(dest => dest.ZoneCode, opt => opt.MapFrom(src => src.ZoneCode))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.CenterLatitude, opt => opt.MapFrom(src => src.CenterLatitude))
            .ForMember(dest => dest.CenterLongitude, opt => opt.MapFrom(src => src.CenterLongitude))
            .ForMember(dest => dest.AreaSquareMeters, opt => opt.MapFrom(src => src.AreaSquareMeters))
            .ForMember(dest => dest.District, opt => opt.MapFrom(src => src.District))
            .ForMember(dest => dest.Version, opt => opt.MapFrom(src => src.Version))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));
    }
}
