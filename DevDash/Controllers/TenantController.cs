using AutoMapper;
using DevDash.DTO.Tenant;
using DevDash.DTO.UserTenant;
using DevDash.Migrations;
using DevDash.model;
using DevDash.Repository;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Net;
using System.Security.Claims;

namespace DevDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TenantController : ControllerBase
    {
        private readonly ITenantRepository _dbTenant;
        private readonly IUserTenantRepository _dbUserTenant;
        private IUserRepository _dbUser;
        private readonly IMapper _mapper;
        private APIResponse _response;
        public TenantController(ITenantRepository tenantRepo,IMapper mapper
            , IUserTenantRepository userTenant,IUserRepository user)
        {
            _dbTenant = tenantRepo;
            _dbUserTenant = userTenant;
            _dbUser = user;
            _mapper = mapper;   
            this._response = new APIResponse();
        }
        [HttpGet(Name = "GetTenants")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetTenants([FromQuery] string? search, int pageSize = 0, int pageNumber = 1)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                int parsedUserId = int.Parse(userId);

                IEnumerable<Tenant> tenants = await _dbTenant.GetAllAsync(
                    filter: t =>
                        t.OwnerID == parsedUserId ||
                        t.UserTenants.Any(ut => ut.UserId == parsedUserId) &&
                        (string.IsNullOrEmpty(search) ||
                         t.Name.ToLower().Contains(search.ToLower()) ||
                         t.Keywords.ToLower().Contains(search.ToLower())),
                    includeProperties: "Owner,UserTenants",
                    pageSize: pageSize,
                    pageNumber: pageNumber
                );
                _response.Result = _mapper.Map<List<TenantDTO>>(tenants);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }
            return _response;

        }

        [HttpGet("{tenantId:int}", Name = "GetTenant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetTenant([FromRoute] int tenantId)
        {
            try
            {
                if (tenantId == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var userTenant = await _dbUserTenant.GetAsync(ut => ut.UserId == int.Parse(userId) && ut.TenantId == tenantId);

                if (userTenant == null)
                {
                    return Unauthorized();
                }

                var tenant = await _dbTenant.GetAsync(u => u.Id == tenantId,includeProperties: "JoinedUsers,Owner");
                if (tenant == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess=false;
                    return NotFound(_response);
                }
                _response.Result = _mapper.Map<TenantDTO>(tenant);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                 = new List<string>() { ex.ToString() };
            }
            return _response;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> CreateTenant([FromBody] TenantCreateDTO createDTO)
        {
            try
            {

                if (createDTO == null)
                {
                    return BadRequest(createDTO);
                }
                var OwnerID = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                var user = await _dbUser.GetAsync(u => u.Id == int.Parse(OwnerID));
                if (user == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User ID is Invalid!");
                    return BadRequest(ModelState);
                }
                TenantDTO tenantDTO = _mapper.Map<TenantDTO>(createDTO);
                tenantDTO.OwnerID = int.Parse(OwnerID);
                Tenant tenant = _mapper.Map<Tenant>(tenantDTO);
                await _dbTenant.CreateAsync(tenant);

                UserTenantDTO userTenantDTO = new()
                {
                    UserId = user.Id,
                    TenantId = tenant.Id,
                    Role = "Admin",
                    JoinedDate = DateTime.Now,
                };
                UserTenant userTenant = _mapper.Map<UserTenant>(userTenantDTO);
                await _dbUserTenant.JoinAsync(userTenant);

                _response.Result = new
                {
                    id=tenant.Id,
                    tenant= tenantDTO
                };
                _response.StatusCode = HttpStatusCode.Created;
                return CreatedAtRoute("GetTenant", new { tenantId = tenant.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }
            return _response;
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpDelete("{tenantId:int}", Name = "DeleteTenant")]
        public async Task<ActionResult<APIResponse>> DeleteTenant([FromRoute]int tenantId)
        {
            try
            {
                if (tenantId == 0)
                {
                    return BadRequest();
                }
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                
                var tenant = await _dbTenant.GetAsync(u => u.Id == tenantId);
                if (tenant == null)
                {
                    return NotFound();
                }
                if (tenant.OwnerID != int.Parse(userId))
                {
                    return Unauthorized();
                }
                    await _dbTenant.RemoveAsync(tenant);
                    _response.StatusCode = HttpStatusCode.NoContent;
                    _response.IsSuccess = true;
                    return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }
            return _response;
        }
        [HttpPut("{tenantId:int}", Name = "UpdateTenant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateTenant([FromRoute] int tenantId, [FromBody] TenantUpdateDTO updateDTO)
        {
            try
            {
                if (updateDTO == null || tenantId !=updateDTO.Id )
                {
                    return BadRequest();
                }
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Invalid token" });
                var tenant = await _dbTenant.GetAsync(u => u.Id == updateDTO.Id);
                var userTenant = await _dbUserTenant.GetAsync(ut => ut.UserId == int.Parse(userId) && ut.TenantId == tenantId);
                if(userTenant.Role != "Admin")
                {
                    return Unauthorized();  
                }

                if (tenant == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Tenant ID is Invalid!");
                    return BadRequest(ModelState);
                }
                _mapper.Map(updateDTO, tenant);

                await _dbTenant.UpdateAsync(tenant);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }
            return _response;
        }
    }
}

