using AutoMapper;
using PGKing.Application.DTOs;
using PGKing.Application.Entities;

namespace PGKing.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<VendorCreateDto, Vendor>();
            CreateMap<VendorUpdateDto, Vendor>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<Vendor, VendorResponseDto>();

            CreateMap<TenantCreateDto, Tenant>();
            CreateMap<TenantUpdateDto, Tenant>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<Tenant, TenantResponseDto>();
        }
    }
}
