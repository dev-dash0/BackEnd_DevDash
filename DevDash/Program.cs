using DevDash.Middleware;
using DevDash.model;

using DevDash.Repository;
using DevDash.Repository.IRepository;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Microsoft.OpenApi.Models;
using System.Text;


namespace DevDash
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddControllers().ConfigureApiBehaviorOptions(
                options =>
                    options.SuppressModelStateInvalidFilter = true);

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("cs"));


            });
            builder.Services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                });
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ITenantRepository, TenantRepository>();
            builder.Services.AddScoped<ISprintRepository, SprintRepository>();
            builder.Services.AddScoped<ICommentRepository, CommentRepository>();
            builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
            builder.Services.AddScoped<IIssueRepository, IssueRepository>();
            builder.Services.AddScoped<IUserTenantRepository, UserTenantRepository>();
            builder.Services.AddScoped<IUserProjectRepository, UserProjectRepository>();
            builder.Services.AddScoped<IIssueAssignUserRepository, IssueAssignUserRepository>();

            builder.Services.AddAutoMapper(typeof(MappingConfig));
            //------------------------------------------------------------------
            // Add services to the container.
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
                });


            builder.Services.AddIdentity<User, IdentityRole<int>>()
                .AddEntityFrameworkStores<AppDbContext>();


            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true; // Save the token in the server
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
                        ValidAudience = builder.Configuration["JWT:ValidAudience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
                    };
                });

            builder.Services.AddControllers().AddNewtonsoftJson();
            //------------------------------------------------------------------
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<TokenBlacklistService>();
            //------------------------------------------------------------------


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options => {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description =
                        "JWT Authorization header using the Bearer scheme. \r\n\r\n " +
                        "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                        "Example: \"Bearer 12345abcdef\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.SecurityScheme,
                                            Id = "Bearer"
                                        },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });

            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage(); // Enable detailed error messages
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();


            app.UseAuthentication(); // Ensure authentication middleware is called before authorization
            app.UseAuthorization();
            app.UseMiddleware<TokenBlacklistMiddleware>();

            //app.UseCustomUnauthorizedResponse();

            app.MapControllers();

            app.Run();
        }
    }

  
}
