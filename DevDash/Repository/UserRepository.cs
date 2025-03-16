using AutoMapper;
using DevDash.DTO.User;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DevDash.model;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DevDash.DTO.Account;

namespace DevDash.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private string secretKey;
        private readonly IMapper _mapper;

        public UserRepository(AppDbContext db, IConfiguration configuration,
            UserManager<User> userManager, IMapper mapper, RoleManager<IdentityRole<int>> roleManager)
        {
            _db = db;
            _mapper = mapper;
            _userManager = userManager;
            secretKey = configuration.GetValue<string>("JWT:Secret");
            _roleManager = roleManager;
        }

        public bool IsUniqueEmail(string email)
        {
            return !_db.Users.Any(x => x.Email == email);
        }

        public async Task<TokenDTO> Login(LoginDTO loginDTO)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == loginDTO.Email.ToLower());
            if (user == null || !(await _userManager.CheckPasswordAsync(user, loginDTO.Password)))
            {
                return new TokenDTO { AccessToken = "" };
            }
            var jwtTokenId = $"JTI{Guid.NewGuid()}";
            var accessToken = await GetAccessToken(user, jwtTokenId);
            var refreshToken = await CreateNewRefreshToken(user.Id, jwtTokenId);

            return new TokenDTO { AccessToken = accessToken, RefreshToken = refreshToken };
        }

        public async Task<UserDTO> Register(RegisterDTO registerDTO)
        {

            User user = new()
            {
                FirstName = registerDTO.FirstName,
                LastName = registerDTO.LastName,
                UserName = registerDTO.Username,
                Email = registerDTO.Email,
                PhoneNumber = registerDTO.PhoneNumber,
                Birthday = registerDTO.Birthday,
            };


            var result = await _userManager.CreateAsync(user, registerDTO.Password);
            if (result.Succeeded)
            {
                var userToReturn = await _db.Users.FirstOrDefaultAsync(u => u.Email == registerDTO.Email);
                return _mapper.Map<UserDTO>(userToReturn);
            }
            else
            {
                return null;
            }
            
        }

        private async Task<string> GetAccessToken(User user, string jwtTokenId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new ArgumentException("Secret key is not provided.");
            }
            var key = Encoding.ASCII.GetBytes(secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, jwtTokenId),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                }),
                Expires = DateTime.UtcNow.AddHours(12),
                Issuer = "http://localhost:44306",
                Audience = "http://localhost:4200",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<TokenDTO> RefreshAccessToken(TokenDTO tokenDTO)
        {
            var existingRefreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(u => u.Refresh_Token == tokenDTO.RefreshToken);
            if (existingRefreshToken == null)
            {
                return new TokenDTO();
            }

            if (!int.TryParse(existingRefreshToken.UserId.ToString(), out int tokenUserId))
            {
                return new TokenDTO();
            }

            var isTokenValid = GetAccessTokenData(tokenDTO.AccessToken, tokenUserId, existingRefreshToken.JwtTokenId);
            if (!isTokenValid || !existingRefreshToken.IsValid || existingRefreshToken.ExpiresAt < DateTime.UtcNow)
            {
                await MarkTokenAsInvalid(existingRefreshToken);
                return new TokenDTO();
            }

            var newRefreshToken = await CreateNewRefreshToken(tokenUserId, existingRefreshToken.JwtTokenId);
            await MarkTokenAsInvalid(existingRefreshToken);

            var applicationUser = await _db.Users.FindAsync(tokenUserId);
            if (applicationUser == null) return new TokenDTO();

            var newAccessToken = await GetAccessToken(applicationUser, existingRefreshToken.JwtTokenId);
            return new TokenDTO { AccessToken = newAccessToken, RefreshToken = newRefreshToken };
        }

        private bool GetAccessTokenData(string accessToken, int expectedUserId, string expectedTokenId)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(accessToken);

                if (!int.TryParse(jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub)?.Value, out int extractedUserId))
                {
                    return false;
                }

                var jwtTokenId = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Jti)?.Value;
                return extractedUserId == expectedUserId && jwtTokenId == expectedTokenId;
            }
            catch
            {
                return false;
            }
        }

        private async Task MarkTokenAsInvalid(RefreshToken refreshToken)
        {
            refreshToken.IsValid = false;
            await _db.SaveChangesAsync();
        }

        private async Task<string> CreateNewRefreshToken(int userId, string tokenId)
        {
            RefreshToken refreshToken = new()
            {
                IsValid = true,
                UserId = userId,
                JwtTokenId = tokenId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(2),
                Refresh_Token = Guid.NewGuid() + "-" + Guid.NewGuid(),
            };

            await _db.RefreshTokens.AddAsync(refreshToken);
            await _db.SaveChangesAsync();
            return refreshToken.Refresh_Token;
        }

        public async Task RevokeRefreshToken(TokenDTO tokenDTO)
        {
            var existingRefreshToken = await _db.RefreshTokens
                .FirstOrDefaultAsync(_ => _.Refresh_Token == tokenDTO.RefreshToken);

            if (existingRefreshToken == null)
                return;

            var isTokenValid = GetAccessTokenData(tokenDTO.AccessToken, existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
            if (!isTokenValid)
            {
                return;
            }

            await MarkAllTokenInChainAsInvalid(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
        }

        private async Task MarkAllTokenInChainAsInvalid(int userId, string tokenId)
        {
            await _db.RefreshTokens
                .Where(u => u.UserId == userId && u.JwtTokenId == tokenId)
                .ExecuteUpdateAsync(u => u.SetProperty(refreshToken => refreshToken.IsValid, false));
        }
        public async Task<User> UpdateAsync(User user)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            return user;
        }
        public async Task RemoveAsync(User user)
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }

        public async Task<List<User>> GetAllAsync(Expression<Func<User, bool>>? filter = null, string? includeProperties = null, int pageSize = 0, int pageNumber = 1)
        {
            IQueryable<User> query = _db.Users;

            if (filter != null)
            {
                query = query.Where(filter);
            }


            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProp in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }

            return await query.ToListAsync();
        }

        public async Task<User> GetAsync(Expression<Func<User, bool>>? filter = null, bool tracked = true, string? includeProperties = null)
        {
            IQueryable<User> query = _db.Users;

            if (!tracked)
            {
                query = query.AsNoTracking();
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProp in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }

            return await query.FirstOrDefaultAsync();
        }

        public Task CreateAsync(User entity)
        {
            throw new NotImplementedException();
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task<UserDTO> GetUserProfile(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return user != null ? _mapper.Map<UserDTO>(user) : null;
        }

        public async Task<bool> ChangePassword(int userId, ChangePasswordDTO changePasswordDTO)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var result = await _userManager.ChangePasswordAsync(user, changePasswordDTO.CurrentPassword, changePasswordDTO.NewPassword);
            return result.Succeeded;
        }

        public async Task<bool> SendPasswordResetToken(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // Send the token via email (implementation needed)
            return true;
        }

        public async Task<bool> ResetPassword(ResetPasswordDTO resetPasswordDTO)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDTO.Email);
            if (user == null) return false;

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordDTO.Token, resetPasswordDTO.NewPassword);
            return result.Succeeded;
        }

        public async Task<UserDTO> UpdateUserProfile(int userId, UpdateProfileDTO updateProfileDTO)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return null;

            user.FirstName = updateProfileDTO.FirstName;
            user.LastName = updateProfileDTO.LastName;
            user.PhoneNumber = updateProfileDTO.PhoneNumber;
            user.Birthday = updateProfileDTO.Birthday;
            user.ImageUrl = updateProfileDTO.ImageUrl;

        
            if (!string.IsNullOrWhiteSpace(updateProfileDTO.UserName) && user.UserName != updateProfileDTO.UserName)
            {
                var existingUser = await _userManager.FindByNameAsync(updateProfileDTO.UserName);
                if (existingUser != null)
                {
                    throw new Exception("Username is already taken.");
                }
                user.UserName = updateProfileDTO.UserName;
            }

           
            if (!string.IsNullOrWhiteSpace(updateProfileDTO.Email) && user.Email != updateProfileDTO.Email)
            {
                var existingEmailUser = await _userManager.FindByEmailAsync(updateProfileDTO.Email);
                if (existingEmailUser != null)
                {
                    throw new Exception("Email is already in use.");
                }

             
                var token = await _userManager.GenerateChangeEmailTokenAsync(user, updateProfileDTO.Email);
                var result = await _userManager.ChangeEmailAsync(user, updateProfileDTO.Email, token);

                if (!result.Succeeded)
                {
                    throw new Exception("Failed to update email.");
                }

              
                user.EmailConfirmed = false;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            return updateResult.Succeeded ? _mapper.Map<UserDTO>(user) : null;
        }

        public async Task<bool> RemoveAccount(int userId)
        {
            var commentsToRemove = await _db.Comments.Where(u => u.CreatedById == userId).ToListAsync();
            _db.Comments.RemoveRange(commentsToRemove);

            var IssuesToRemove = await _db.Issues.Where(u => u.CreatedById == userId).ToListAsync();
            _db.Issues.RemoveRange(IssuesToRemove);

            var SprintsToRemove = await _db.Sprints.Where(u => u.CreatedById == userId).ToListAsync();
            _db.Sprints.RemoveRange(SprintsToRemove);

            var ProjectsToRemove = await _db.Projects.Where(u => u.CreatorId == userId).ToListAsync();
            _db.Projects.RemoveRange(ProjectsToRemove);

            var TenantsToRemove = await _db.Tenants.Where(u => u.OwnerID == userId).ToListAsync();
            _db.Tenants.RemoveRange(TenantsToRemove);

            var UserTenantsToRemove = await _db.UserTenants.Where(u => u.UserId == userId).ToListAsync();
            _db.UserTenants.RemoveRange(UserTenantsToRemove);

            var UserProjectToRemove = await _db.UserProjects.Where(u => u.UserId == userId).ToListAsync();
            _db.UserProjects.RemoveRange(UserProjectToRemove);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }


    }


}
