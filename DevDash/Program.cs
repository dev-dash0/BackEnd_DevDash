using DevDash.Middleware;
using DevDash.model;
using DevDash.Repository;
using DevDash.Repository.IRepository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
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

            // ? Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    policy =>
                    {
                        policy.WithOrigins("https://localhost:44306", "https://localhost:4200", "http://localhost:5197", "https://localhost:7275")
                             .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();
                    });
            });

            // ? Configure Controllers with JSON options
            builder.Services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
                });

            // ? Configure Database Context
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("cs"));
            });

            // ? Register Repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ITenantRepository, TenantRepository>();
            builder.Services.AddScoped<ISprintRepository, SprintRepository>();
            builder.Services.AddScoped<ICommentRepository, CommentRepository>();
            builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
            builder.Services.AddScoped<IIssueRepository, IssueRepository>();
            builder.Services.AddScoped<IUserTenantRepository, UserTenantRepository>();
            builder.Services.AddScoped<IUserProjectRepository, UserProjectRepository>();
            builder.Services.AddScoped<IIssueAssignUserRepository, IssueAssignUserRepository>();
            builder.Services.AddScoped<IDashBoardRepository, DashBoardRepository>();
            builder.Services.AddScoped<IPinnedItemRepository, PinnedItemRepository>();
            builder.Services.AddScoped<ISearchRepository, SearchRepository>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

            // ? Register AutoMapper
            builder.Services.AddAutoMapper(typeof(MappingConfig));

            // ? Configure Identity
            builder.Services.AddIdentity<User, IdentityRole<int>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // ? Configure JWT Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
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
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    }
                };
            });

            // ? Enable Memory Caching & Token Blacklist Service
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<TokenBlacklistService>();

            // ? Enable SignalR
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            }
                );

            // ? Configure Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
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

            // ? Middleware for handling invalid models manually in .NET 8
            app.Use(async (context, next) =>
            {
                await next();

                if (context.Response.StatusCode == 400 && !context.Response.HasStarted)
                {
                    var problemDetails = new { Message = "Invalid request parameters.", StatusCode = 400 };
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(problemDetails);
                }
            });

            // ? Configure Middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();

                // ? Enable Swagger in production securely
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "DevDash API v1");
                    options.RoutePrefix = string.Empty; // Make Swagger available at the root URL
                });
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");

            app.UseWebSockets();

            app.UseRouting();

            // ? Ensure token processing is done first
            app.UseMiddleware<TokenBlacklistMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            // ? Enable SignalR notifications
            app.MapHub<NotificationHub>("/notificationHub");

            app.MapControllers();
            app.Run();
        }
    }
}
