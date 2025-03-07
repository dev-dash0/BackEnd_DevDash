using AutoMapper;
using Azure;
using DevDash.DTO.Tenant;
using DevDash.DTO.UserTenant;
using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace DevDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserTenantController : ControllerBase
    {
        private IUserTenantRepository _userTenantRepository;
        private ITenantRepository _tenantRepository;
        private IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private APIResponse _response;

        public UserTenantController(ITenantRepository tenantRepository, IUserRepository userRepository,
             IMapper mapper, IUserTenantRepository userTenantRepository)
        {
            _tenantRepository = tenantRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            this._response = new APIResponse();
            _userTenantRepository = userTenantRepository;
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> JoinTenant([FromQuery]string tenantCode)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                var user = await _userRepository.GetAsync(u => u.Id == int.Parse(userId));
                if (user == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User ID is Invalid!");
                    return BadRequest(ModelState);
                }

                var tenant = await _tenantRepository.GetAsync(t => t.TenantCode == tenantCode);
                if (tenant == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Tenant Code is Invalid!");
                    return BadRequest(ModelState);
                }

                var existingUserTenant = await _userTenantRepository
                    .GetAsync(ut => ut.UserId == user.Id && ut.TenantId == tenant.Id);

                if (existingUserTenant != null)
                {
                    ModelState.AddModelError("ErrorMessages", "User is already a member of this Tenant!");
                    return BadRequest(ModelState);
                }

                UserTenantDTO userTenantDTO = new()
                {
                    UserId = user.Id,
                    TenantId = tenant.Id,
                    Role = "Developer",
                    JoinedDate = DateTime.Now,
                };
                UserTenant userTenant = _mapper.Map<UserTenant>(userTenantDTO);
                await _userTenantRepository.JoinAsync(userTenant);

                _response.Result = userTenantDTO;
                _response.StatusCode = HttpStatusCode.Created;
                return _response;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
            }
            return _response;
        }

        [HttpDelete("{tenantId:int}", Name = "LeaveTenant")]
        public async Task<ActionResult<APIResponse>> LeaveTenant([FromRoute] int tenantId)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                var user = await _userRepository.GetAsync(u => u.Id == int.Parse(userId));
                if (user == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User ID is Invalid!");
                    return BadRequest(ModelState);
                }

                var tenant = await _tenantRepository.GetAsync(t => t.Id == tenantId);
                if (tenant == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Tenant Code is Invalid!");
                    return BadRequest(ModelState);
                }

                var existingUserTenant = await _userTenantRepository
                    .GetAsync(ut => ut.UserId == user.Id && ut.TenantId == tenant.Id);

                if (existingUserTenant == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User is already out of this Tenant!");
                    return BadRequest(ModelState);
                }

                
                await _userTenantRepository.LeaveAsync(existingUserTenant);

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
            }
            return _response;
        }




    }
}
