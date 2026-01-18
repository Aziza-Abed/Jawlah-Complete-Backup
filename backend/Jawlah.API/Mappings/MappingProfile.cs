using AutoMapper;
using Jawlah.Core.DTOs.Sync;
using Jawlah.Core.DTOs.Attendance;
using Jawlah.Core.DTOs.Auth;
using Jawlah.Core.DTOs.Issues;
using Jawlah.Core.DTOs.Notifications;
using Jawlah.Core.DTOs.Tasks;
using Jawlah.Core.DTOs.Users;
using Jawlah.Core.DTOs.Zones;
using Jawlah.Core.Entities;
using TaskEntity = Jawlah.Core.Entities.Task;

namespace Jawlah.API.Mappings;

// autoMapper configuration profile for entity-to-DTO mappings
// only explicit mappings for properties that need transformation or have different names
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // user → userResponse (all properties match, no explicit mapping needed)
        CreateMap<User, UserResponse>();

        // user → userDto (only map properties that need transformation)
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.WorkerType, opt => opt.MapFrom(src => src.WorkerType != null ? src.WorkerType.ToString() : null))
            .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.Pin ?? src.Username))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber ?? string.Empty))
            .ForMember(dest => dest.MunicipalityId, opt => opt.MapFrom(src => src.MunicipalityId))
            .ForMember(dest => dest.MunicipalityCode, opt => opt.MapFrom(src => src.Municipality != null ? src.Municipality.Code : ""))
            .ForMember(dest => dest.MunicipalityName, opt => opt.MapFrom(src => src.Municipality != null ? src.Municipality.Name : ""))
            .ForMember(dest => dest.MunicipalityNameEnglish, opt => opt.MapFrom(src => src.Municipality != null ? src.Municipality.NameEnglish : null));

        // task → taskResponse (only map navigation properties and transformations)
        CreateMap<TaskEntity, TaskResponse>()
            .ForMember(dest => dest.AssignedToUserName, opt => opt.MapFrom(src => src.AssignedToUser.FullName))
            .ForMember(dest => dest.AssignedByUserName, opt => opt.MapFrom(src => src.AssignedByUser != null ? src.AssignedByUser.FullName : null))
            .ForMember(dest => dest.ZoneName, opt => opt.MapFrom(src => src.Zone != null ? src.Zone.ZoneName : null))
            .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => src.Photos.OrderBy(p => p.OrderIndex).Select(p => p.PhotoUrl).ToList()));

        // issue → issueResponse (map navigation properties and photo transformation)
        CreateMap<Issue, IssueResponse>()
            .ForMember(dest => dest.ReportedByName, opt => opt.MapFrom(src => src.ReportedByUser.FullName))
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

        // Sync Mappings
        CreateMap<TaskEntity, TaskSyncDto>();
        CreateMap<Issue, IssueSyncDto>();
    }
}
