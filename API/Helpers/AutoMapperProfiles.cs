using API.DTOs;
using API.Entities;
using API.Extensions;
using AutoMapper;

namespace API.Helpers;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<AppUser, MemberDto>()
            .ForMember(
                dest => dest.PhotoUrl, opt => opt.MapFrom(
                    src => src.Photos.SingleOrDefault(p => p.IsMain).Url
                )
            )
            .ForMember(
                dest => dest.Age, opt => opt.MapFrom(
                    src => src.DateOfBirth.CalculateAge()
                )
            );
        CreateMap<Photo, PhotoDto>();
        CreateMap<Photo, PhotoForApprovalDto>()
            .ForMember(
                dest => dest.UserName, opt => opt.MapFrom(
                    src => src.AppUser.UserName
                )
            );

        CreateMap<MemberUpdateDto, AppUser>();
        CreateMap<RegisterDto, AppUser>();

        CreateMap<Message, MessageDto>()
            .ForMember(
                dest => dest.SenderPhotoUrl, opt => opt.MapFrom(
                    src => src.Sender.Photos.SingleOrDefault(p => p.IsMain).Url
                )
            )
            .ForMember(
                dest => dest.RecipientPhotoUrl, opt => opt.MapFrom(
                    src => src.Recipient.Photos.SingleOrDefault(p => p.IsMain).Url
                )
            );

        CreateMap<DateTime, DateTime>()
            .ConvertUsing(d => DateTime.SpecifyKind(d, DateTimeKind.Utc));
            
        CreateMap<DateTime?, DateTime?>()
            .ConvertUsing(d => d.HasValue ? DateTime.SpecifyKind(d.Value, DateTimeKind.Utc) : null);
    }
}
