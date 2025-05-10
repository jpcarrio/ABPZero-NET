using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Authorization.Roles;
using Abp.AutoMapper;
using ABPNET.Authorization.Roles;
using Newtonsoft.Json;

namespace ABPNET.Roles.Dto
{
    [AutoMapFrom(typeof(Role))]
    [AutoMapTo(typeof(Role))]
    public class RoleDto : EntityDto<int>
    {
        public RoleDto()
        {
        }

        [Required]
        [StringLength(AbpRoleBase.MaxNameLength)]
        public string Name { get; set; }

        [Required]
        [StringLength(AbpRoleBase.MaxDisplayNameLength)]
        public string DisplayName { get; set; }

        public string NormalizedName { get; set; }

        [StringLength(Role.MaxDescriptionLength)]
        public string Description { get; set; }

        public List<string> GrantedPermissions { get; set; }
    }
}



