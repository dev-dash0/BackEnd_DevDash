using AutoMapper;
using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace DevDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ITenantRepository _dbTenant;
        private readonly IUserTenantRepository _dbUserTenant;
        private IUserRepository _dbUser;
        private readonly IMapper _mapper;
        private APIResponse _response;
        public PaymentController(IMapper mapper
          , IUserRepository user
             )
        {
           
            _dbUser = user;
            _mapper = mapper;
            this._response = new APIResponse();
        }


        [Authorize]

        [HttpPost("create-checkout-session")]
        public IActionResult CreateCheckoutSession()
        {
            
        var domain = "http://localhost:4200/MyDashboard"; // edit frontend domain as needed
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = 100000, // 100 egp
                            Currency = "egp",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Premium Subscription",
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = domain + "/payment-success",
                CancelUrl = domain + "/payment-cancel",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            return Ok(new { url = session.Url });
        }

        [HttpPost("changestate")]
        [Authorize]
        public async Task<IActionResult> ChangeState()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            if (!int.TryParse(userIdClaim, out int userId))
                return BadRequest("Invalid user ID.");

            var user = await _dbUser.GetAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found.");

          
            user.statepricing = "premium"; 

            await _dbUser.UpdateAsync(user);

            return Ok(new { message = "State updated to Premium successfully." });
        }

    }

}