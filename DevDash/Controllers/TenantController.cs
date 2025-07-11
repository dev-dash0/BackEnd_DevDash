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
            , IUserTenantRepository userTenant,IUserRepository user
            , INotificationRepository notificationRepository)
        {
            _dbTenant = tenantRepo;
            _dbUserTenant = userTenant;
            _dbUser = user;
            _mapper = mapper;
            this._response = new APIResponse();
        }
        [HttpGet("tenants")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetTenants([FromQuery] string? search, int pageSize = 0, int pageNumber = 1)
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized();

                if (!int.TryParse(userIdClaim, out int userId))
                    return Unauthorized();

                var tenants = await _dbTenant.GetAllAsync(
     filter: t =>
         t.UserTenants.Any(ut =>
             ut.UserId == userId && ut.AcceptedInvitation == true) &&
         (
             string.IsNullOrEmpty(search) ||
             t.Name.ToLower().Contains(search.ToLower()) ||
             t.Keywords.ToLower().Contains(search.ToLower())
         ),
     includeProperties: "Owner,UserTenants",
     pageSize: pageSize,
     pageNumber: pageNumber
 );


                var tenantDTOs = _mapper.Map<List<TenantDTO>>(tenants);

                
                foreach (var item in tenantDTOs)
                {
                    var userTenant = await _dbUserTenant.GetAsync(i => i.TenantId == item.Id && i.UserId == userId);
                    item.Role = userTenant?.Role; 
                }

                _response.Result = tenantDTOs;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }


        [HttpGet("{tenantId:int}", Name = "GetTenant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> GetTenant([FromRoute] int tenantId)
        {
            try
            {
                if (tenantId <= 0)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string> { "Invalid Tenant ID" };
                    return BadRequest(_response);
                }

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string> { "Invalid token" };
                    return Unauthorized(_response);
                }

                var userTenant = await _dbUserTenant.GetAsync(ut => ut.UserId == userId && ut.TenantId == tenantId && ut.AcceptedInvitation == true);
                if (userTenant == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string> { "You are not authorized to access this tenant" };
                    return Unauthorized(_response);
                }

                var tenant = await _dbTenant.GetAsync(
                    u => u.Id == tenantId,
                    includeProperties: "JoinedUsers,Owner"
                );
               

                if (tenant == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string> { "Tenant not found" };
                    return NotFound(_response);
                }
             
              

                var tenantDto = _mapper.Map<TenantDTO>(tenant);
                tenantDto.Role = userTenant?.Role;
                _response.Result = tenantDto;
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
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

                var temp = await _dbUserTenant.GetAllAsync(ut => ut.UserId == int.Parse(OwnerID));
                var user = await _dbUser.GetAsync(u => u.Id == int.Parse(OwnerID));
                var tempCount = temp.Count();
                if(tempCount >= 3 && user.statepricing=="normal") 
                    { 
                     return BadRequest(new { message = "You have reached the maximum number of tenants allowed for your account. " +
                         "Please upgrade to create more tenants." });

                }

                //var owertenant= await _dbUserTenant.Get(ut => ut.UserId == int.Parse(OwnerID) && ut.Role == "Admin");
                // var owertenantcount = owertenant.Count();


        
                if (user == null)
                {
                    ModelState.AddModelError("ErrorMessages", "User ID is Invalid!");
                    return BadRequest(ModelState);
                }
                TenantDTO tenantDTO = _mapper.Map<TenantDTO>(createDTO);
                tenantDTO.OwnerID = int.Parse(OwnerID);
                Tenant tenant = _mapper.Map<Tenant>(tenantDTO);
                await _dbTenant.CreateAsync(tenant,int.Parse(OwnerID));

                UserTenantDTO userTenantDTO = new()
                {
                    UserId = user.Id,
                    TenantId = tenant.Id,
                    Role = "Admin",
                    JoinedDate = DateTime.Now,
                    AcceptedInvitation = true
                };
                UserTenant userTenant = _mapper.Map<UserTenant>(userTenantDTO);
                await _dbUserTenant.JoinAsync(userTenant,int.Parse(OwnerID));


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


                await _dbTenant.RemoveAsync(tenant,int.Parse(userId));

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
                if (updateDTO == null )
                {
                    return BadRequest();
                }
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Invalid token" });
                var tenant = await _dbTenant.GetAsync(u => u.Id == tenantId);
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

                await _dbTenant.UpdateAsync(tenant,int.Parse(userId));

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

