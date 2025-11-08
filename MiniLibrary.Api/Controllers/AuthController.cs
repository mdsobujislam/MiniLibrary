using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniLibrary.Infrastructure.Services;

namespace MiniLibrary.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwt;
        public AuthController(JwtService jwt) { _jwt = jwt; }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            if (req.Username == "admin" && req.Password == "123456")
            {
                var token = _jwt.GenerateToken(req.Username);
                return Ok(new { Token = token });
            }
            return Unauthorized();
        }
        public record LoginRequest(string Username, string Password);
    }

}
