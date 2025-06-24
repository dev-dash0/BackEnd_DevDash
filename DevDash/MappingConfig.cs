using AutoMapper;
using DevDash.DTO;
using DevDash.DTO.Account;
using DevDash.DTO.Comment;
using DevDash.DTO.Issue;
using DevDash.DTO.IssueAssignedUser;
using DevDash.DTO.Project;
using DevDash.DTO.Sprint;
using DevDash.DTO.Tenant;
using DevDash.DTO.User;
using DevDash.DTO.UserProject;
using DevDash.DTO.UserTenant;
using DevDash.model;
using DevDash.Repository;

namespace DevDash
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<Tenant,TenantDTO>().ReverseMap();
            CreateMap<Tenant,TenantCreateDTO>().ReverseMap();
            CreateMap<Tenant,TenantUpdateDTO>().ReverseMap();
            CreateMap<TenantDTO, TenantCreateDTO>().ReverseMap();
            CreateMap<TenantDTO,TenantUpdateDTO>().ReverseMap();

            CreateMap<Project, ProjectDTO>().ReverseMap();
            CreateMap<Project, ProjectCreateDTO>().ReverseMap();
            CreateMap<Project, ProjectUpdateDTO>().ReverseMap();
            CreateMap<ProjectDTO, ProjectCreateDTO>().ReverseMap();
            CreateMap<ProjectDTO, ProjectUpdateDTO>().ReverseMap();

            CreateMap<Sprint, SprintDTO>().ReverseMap();
            CreateMap<Sprint, SprintCreateDTO>().ReverseMap();
            CreateMap<Sprint, SprintUpdateDTO>().ReverseMap();
            CreateMap<SprintDTO, SprintCreateDTO>().ReverseMap();
            CreateMap<SprintDTO, SprintUpdateDTO>().ReverseMap();

            CreateMap<Issue, IssueDTO>().ReverseMap();
            CreateMap<Issue, IssueCreataDTO>().ReverseMap();
            CreateMap<Issue, IssueUpdateDTO>().ReverseMap();
            CreateMap<IssueDTO, IssueCreataDTO>().ReverseMap();
            CreateMap<IssueDTO, IssueUpdateDTO>().ReverseMap();

            CreateMap<Comment, CommentDTO>().ReverseMap();
            CreateMap<Comment, CommentCreateDTO>().ReverseMap();
            CreateMap<Comment, CommentUpdateDTO>().ReverseMap();
            CreateMap<CommentDTO, CommentCreateDTO>().ReverseMap();
            CreateMap<CommentDTO, CommentUpdateDTO>().ReverseMap();


            CreateMap<UserTenantDTO, UserTenant>().ReverseMap();

            CreateMap<UserProjectDTO,UserProject>().ReverseMap();


            CreateMap<IssueAssignedUser, IssueAssignedUserDTO>().ReverseMap();

            CreateMap<User, UserDTO>().ReverseMap();
            CreateMap<ChangePasswordDTO,User>().ReverseMap();
            //CreateMap<ResetPasswordDTO,User>().ReverseMap();
            CreateMap<StepResponseDTO, PasswordReset>().ReverseMap();
            CreateMap<UpdateProfileDTO,User>().ReverseMap();


            //dashboard
            CreateMap<Project, ProjectDashBoardDTO>().ReverseMap();
            CreateMap<Issue, IssueDashBoardDTO>().ReverseMap();



        }
    }
}

