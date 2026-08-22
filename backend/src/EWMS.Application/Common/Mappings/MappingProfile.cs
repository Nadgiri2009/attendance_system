using AutoMapper;
using EWMS.Application.Employees;
using EWMS.Domain.Entities;

namespace EWMS.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Employee, EmployeeDto>()
            .ForMember(d => d.Gender, o => o.MapFrom(s => s.Gender.ToString()))
            .ForMember(d => d.DepartmentName, o => o.MapFrom(s => s.Department.Name))
            .ForMember(d => d.DesignationTitle, o => o.MapFrom(s => s.Designation.Title))
            .ForMember(d => d.ReportingManagerName, o => o.MapFrom(
                s => s.ReportingManager == null ? null : s.ReportingManager.FirstName + " " + s.ReportingManager.LastName));
    }
}
