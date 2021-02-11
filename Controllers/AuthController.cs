using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using net5_webapi.Engines;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace net5_webapi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration config;
        private readonly IDBEngine db;
        private readonly ICryptoEngine crypto;

        public AuthController(IConfiguration Configuration, IDBEngine DBEngine, ICryptoEngine CryptoEngine)
        {
            config = Configuration;
            db = DBEngine;
            crypto = CryptoEngine;
        }

        public class LoginBody
        {
            [Required]
            public string Username { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        /// <summary>
        /// Login with username and password. No previous authorization required.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Login([FromBody] LoginBody body)
        {
            string username = body.Username.ToLower();

            string hash = await db.Value<string>("SELECT hash FROM User WHERE LOWER(username)=@username", new { username });

            if (!string.IsNullOrEmpty(hash) && crypto.HashCheck(hash, body.Password))
            {
                JObject session = await LoadSession(username);
                session["token"] = GenerateJwtToken(session);

                return Ok(session);
            }

            return Unauthorized("error.credentials");
        }

        public class RegisterBody
        {
            [Required]
            public string Username { get; set; }
            [Required]
            [DataType(DataType.EmailAddress)]
            public string Email { get; set; }
            [Required]
            public string Name { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        /// <summary>
        /// Register with username, email, name and password. No previous authorization required.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Register([FromBody] RegisterBody body)
        {
            string username = body.Username.ToLower();
            string email = body.Email.ToLower();

            if (await db.Value<int>("SELECT COUNT(*) FROM User WHERE LOWER(username)=@username OR LOWER(email)=@email", new { username, email }) > 0)
                return Unauthorized("error.unavailable");

            await db.Execute("INSERT INTO User (username, hash, email) VALUES (@username, @hash, @email)", new { username, hash = crypto.Hash(body.Password), email });

            return Ok();
        }

        /// <summary>
        /// Returns the session data for this user.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Session()
        {
            return Ok(await LoadSession());
        }

        private async Task<JObject> LoadSession(string username = "")
        {
            if (username == "")
                username = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return await db.Json("SELECT id, username, email FROM User WHERE LOWER(username)=@username", new { username = username.ToLower() });
        }

        private string GenerateJwtToken(JObject session)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, session["username"].ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, session["id"].ToString())
            };

            dynamic JwtConfig = new
            {
                Issuer = config.GetSection("JWT:Issuer").Value,
                Key = config.GetSection("JWT:Key").Value,
                ExpireMinutes = config.GetSection("JWT:ExpireMinutes").Value
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtConfig.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(Convert.ToDouble(JwtConfig.ExpireMinutes));

            var token = new JwtSecurityToken(
                JwtConfig.Issuer,
                JwtConfig.Issuer,
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
