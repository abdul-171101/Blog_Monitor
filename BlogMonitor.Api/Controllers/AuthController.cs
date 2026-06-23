using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace BlogMonitor.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _config;

        public AuthController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (req.Role != "Admin" && req.Role != "Viewer") return BadRequest("Invalid role");

            var existing = await _userManager.FindByEmailAsync(req.Email);
            if (existing != null) return Conflict("Email already registered.");

            var user = new IdentityUser { UserName = req.Email, Email = req.Email };
            var res = await _userManager.CreateAsync(user, req.Password);
            if (!res.Succeeded) return BadRequest(res.Errors);

            await _userManager.AddToRoleAsync(user, req.Role);
            return StatusCode(201);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null) return Unauthorized();

            var signin = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
            if (!signin.Succeeded) return Unauthorized("Invalid credentials.");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Viewer";

            var jwtSection = _config.GetSection("Jwt");
            var secret = jwtSection["SecretKey"]!;
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var expiryMinutes = int.Parse(jwtSection["AccessTokenExpiryMinutes"] ?? "60");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddMinutes(expiryMinutes), signingCredentials: creds);
            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

            // refresh token
            var refreshBytes = new byte[64];
            RandomNumberGenerator.Fill(refreshBytes);
            var refreshToken = Convert.ToBase64String(refreshBytes);
            await _userManager.SetAuthenticationTokenAsync(user, "BlogMonitor", "RefreshToken", refreshToken);

            Response.Cookies.Append("refreshToken", refreshToken, new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = Request.IsHttps ? Microsoft.AspNetCore.Http.SameSiteMode.None : Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(int.Parse(jwtSection["RefreshTokenExpiryDays"] ?? "7"))
            });

            return Ok(new LoginResponse(tokenStr, expiryMinutes, role));
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken)) return Unauthorized();

            // find user by stored token
            var users = _userManager.Users.ToList();
            IdentityUser? matched = null;
            foreach (var u in users)
            {
                var tok = await _userManager.GetAuthenticationTokenAsync(u, "BlogMonitor", "RefreshToken");
                if (tok == refreshToken)
                {
                    matched = u; break;
                }
            }

            if (matched == null) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(matched);
            var role = roles.FirstOrDefault() ?? "Viewer";

            var jwtSection = _config.GetSection("Jwt");
            var secret = jwtSection["SecretKey"]!;
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var expiryMinutes = int.Parse(jwtSection["AccessTokenExpiryMinutes"] ?? "60");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, matched.Id),
                new Claim(ClaimTypes.Email, matched.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddMinutes(expiryMinutes), signingCredentials: creds);
            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

            var refreshBytes = new byte[64];
            RandomNumberGenerator.Fill(refreshBytes);
            var newRefresh = Convert.ToBase64String(refreshBytes);
            await _userManager.SetAuthenticationTokenAsync(matched, "BlogMonitor", "RefreshToken", newRefresh);

            Response.Cookies.Append("refreshToken", newRefresh, new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = Request.IsHttps ? Microsoft.AspNetCore.Http.SameSiteMode.None : Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(int.Parse(jwtSection["RefreshTokenExpiryDays"] ?? "7"))
            });

            return Ok(new RefreshResponse(tokenStr, expiryMinutes));
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
            {
                return Ok();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    await _userManager.RemoveAuthenticationTokenAsync(user, "BlogMonitor", "RefreshToken");
                }
            }

            Response.Cookies.Append("refreshToken", "", new Microsoft.AspNetCore.Http.CookieOptions
            {
                Expires = DateTimeOffset.UnixEpoch,
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = Request.IsHttps ? Microsoft.AspNetCore.Http.SameSiteMode.None : Microsoft.AspNetCore.Http.SameSiteMode.Lax
            });

            return Ok();
        }
    }
}