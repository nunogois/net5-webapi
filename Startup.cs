using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using net5_webapi.Engines;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Reflection;
using System.Text;

namespace net5_webapi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // CORS - This enables everything, might not be what you need, evaluate accordingly
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                          builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .Build());
            });

            // JWT
            dynamic JwtConfig = new
            {
                Issuer = Configuration.GetSection("JWT:Issuer").Value,
                Key = Configuration.GetSection("JWT:Key").Value
            };

            // Remove default claims
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

                })
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = JwtConfig.Issuer,
                        ValidAudience = JwtConfig.Issuer,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtConfig.Key)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            // ENGINES
            services.AddTransient<IDBEngine, DBEngine>();
            services.AddTransient<ICryptoEngine, CryptoEngine>();

            // I'm overriding the default System.Text.Json of .NET 5.0 because I prefer Newtonsoft's behaviour
            services.AddControllers().AddNewtonsoftJson();

            // SWAGGER
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "net5-webapi", Version = "v1", Description = ".NET 5.0 Docker-ready C# REST API example using JWT authentication, Swagger, Newtonsoft JSON, and Dapper with SQL Server." });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                        Reference = new OpenApiReference
                            {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                            },
                            Name = "Bearer",
                            Scheme = "Bearer",
                            In = ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "net5_webapi v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
