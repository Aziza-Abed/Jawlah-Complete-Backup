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
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber ?? string.Empty));

        // task → taskResponse (only map navigation properties and transformations)
        CreateMap<TaskEntity, TaskResponse>()
            .ForMember(dest => dest.AssignedToUserName, opt => opt.MapFrom(src => src.AssignedToUser.FullName))
            .ForMember(dest => dest.AssignedByUserName, opt => opt.MapFrom(src => src.AssignedByUser != null ? src.AssignedByUser.FullName : null))
            .ForMember(dest => dest.ZoneName, opt => opt.MapFrom(src => src.Zone != null ? src.Zone.ZoneName : null))
            .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => src.Photos.OrderBy(p => p.OrderIndex).Select(p => p.PhotoUrl).ToList()));

        // issue → issueResponse (only map photo transformation)
        CreateMap<Issue, IssueResponse>()
            .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => src.Photos.OrderBy(p => p.OrderIndex).Select(p => p.PhotoUrl).ToList()));

        // attendance → attendanceResponse (map navigation properties and renamed field)
        CreateMap<Attendance, AttendanceResponse>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName))
            .ForMember(dest => dest.ZoneName, opt => opt.MapFrom(src => src.Zone != null ? src.Zone.ZoneName : null))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CheckInSyncTime));

        // notification → notificationResponse (all properties match)
        CreateMap<Notification, NotificationResponse>();

        // zone → zoneResponse (all properties match)
        CreateMap<Zone, ZoneResponse>();

        // Sync Mappings
        CreateMap<TaskEntity, TaskSyncDto>();
        CreateMap<Issue, IssueSyncDto>();
    }
}
