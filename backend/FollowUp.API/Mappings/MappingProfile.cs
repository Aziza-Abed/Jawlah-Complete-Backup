using AutoMapper;
using FollowUp.Core.DTOs.Sync;
using FollowUp.Core.DTOs.Attendance;
using FollowUp.Core.DTOs.Appeals;
using FollowUp.Core.DTOs.Auth;
using FollowUp.Core.DTOs.Issues;
using FollowUp.Core.DTOs.Notifications;
using FollowUp.Core.DTOs.Tasks;
using FollowUp.Core.DTOs.Users;
using FollowUp.Core.DTOs.Zones;
using FollowUp.Core.Entities;
using TaskEntity = FollowUp.Core.Entities.Task;

namespace FollowUp.API.Mappings;

// autoMapper configuration profile for entity-to-DTO mappings
// only explicit mappings for properties that need transformation or have different names
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // user → userResponse (map supervisor name from navigation property)
        CreateMap<User, UserResponse>()
            .ForMember(dest => dest.SupervisorName, opt => opt.MapFrom(src => src.Supervisor != null ? src.Supervisor.FullName : null));

        // user → userDto (only map properties that need transformation)
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.WorkerType, opt => opt.MapFrom(src => src.WorkerType != null ? src.WorkerType.ToString() : null))
            .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber ?? string.Empty))
            .ForMember(dest => dest.MunicipalityId, opt => opt.MapFrom(src => src.MunicipalityId))
            .ForMember(dest => dest.MunicipalityCode, opt => opt.MapFrom(src => src.Municipality != null ? src.Municipality.Code : ""))
            .ForMember(dest => dest.MunicipalityName, opt => opt.MapFrom(src => src.Municipality != null ? src.Municipality.Name : ""))
            .ForMember(dest => dest.MunicipalityNameEnglish, opt => opt.MapFrom(src => src.Municipality != null ? src.Municipality.NameEnglish : null));

        // task → taskResponse (only map navigation properties and transformations)
        CreateMap<TaskEntity, TaskResponse>()
            .ForMember(dest => dest.AssignedToUserName, opt => opt.MapFrom(src => src.AssignedToUser != null ? src.AssignedToUser.FullName : "غير متوفر"))
            .ForMember(dest => dest.AssignedByUserName, opt => opt.MapFrom(src => src.AssignedByUser != null ? src.AssignedByUser.FullName : null))
            .ForMember(dest => dest.ZoneName, opt => opt.MapFrom(src => src.Zone != null ? src.Zone.ZoneName : null))
            .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => src.Photos.OrderBy(p => p.OrderIndex).Select(p => p.PhotoUrl).ToList()));

        // issue → issueResponse (map navigation properties and photo transformation)
        CreateMap<Issue, IssueResponse>()
            .ForMember(dest => dest.ReportedByName, opt => opt.MapFrom(src => src.ReportedByUser != null ? src.ReportedByUser.FullName : "غير معروف"))
            .ForMember(dest => dest.ZoneName, opt => opt.MapFrom(src => src.Zone != null ? src.Zone.ZoneName : null))
            .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => src.Photos.OrderBy(p => p.OrderIndex).Select(p => p.PhotoUrl).ToList()));

        // attendance → attendanceResponse (map navigation properties and renamed field)
        CreateMap<Attendance, AttendanceResponse>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName))
            .ForMember(dest => dest.ZoneName, opt => opt.MapFrom(src => src.Zone != null ? src.Zone.ZoneName : null))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CheckInSyncTime))
            .ForMember(dest => dest.ApprovedByUserName, opt => opt.MapFrom(src => src.ApprovedByUser != null ? src.ApprovedByUser.FullName : null));

        // notification → notificationResponse (convert type enum to string for mobile compatibility)
        CreateMap<Notification, NotificationResponse>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));

        // zone → zoneResponse (all properties match)
        CreateMap<Zone, ZoneResponse>();

        // appeal → appealResponse (map navigation properties and enum conversions)
        CreateMap<Appeal, AppealResponse>()
            .ForMember(dest => dest.WorkerName, opt => opt.MapFrom(src => src.User.FullName))
            .ForMember(dest => dest.ReviewedByName, opt => opt.MapFrom(src => src.ReviewedByUser != null ? src.ReviewedByUser.FullName : null))
            .ForMember(dest => dest.AppealTypeName, opt => opt.MapFrom(src => GetAppealTypeName(src.AppealType)))
            .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => GetAppealStatusName(src.Status)))
            .ForMember(dest => dest.EntityTitle, opt => opt.Ignore()); // Will be set manually in controller

        // Sync Mappings
        CreateMap<TaskEntity, TaskSyncDto>();
        CreateMap<Issue, IssueSyncDto>();
    }

    private static string GetAppealTypeName(Core.Enums.AppealType type)
    {
        return type switch
        {
            Core.Enums.AppealType.TaskRejection => "رفض مهمة",
            Core.Enums.AppealType.AttendanceFailure => "فشل حضور",
            _ => type.ToString()
        };
    }

    private static string GetAppealStatusName(Core.Enums.AppealStatus status)
    {
        return status switch
        {
            Core.Enums.AppealStatus.Pending => "قيد المراجعة",
            Core.Enums.AppealStatus.Approved => "مقبول",
            Core.Enums.AppealStatus.Rejected => "مرفوض",
            _ => status.ToString()
        };
    }
}
