using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class AuthController : ControllerBase
  {
    private readonly IAuthRepository _repo;
    private readonly IConfiguration _config;
    public AuthController(IAuthRepository repo, IConfiguration config)
    {
      _config = config;
      _repo = repo;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
    {
      userForRegisterDto.Username = userForRegisterDto.Username.ToLower();
      if (await _repo.UserExists(userForRegisterDto.Username))
        return BadRequest("Username already exists");

      var userToCreate = new User
      {
        Username = userForRegisterDto.Username
      };

      var createdUser = await _repo.Register(userToCreate, userForRegisterDto.Password);

      return StatusCode(201);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
    {
      // Try to login the user first
      var userFromRepo = await _repo.Login(userForLoginDto.Username.ToLower(), userForLoginDto.Password);

      // Return Unauthorized if Login failed
      if (userFromRepo == null)
        return Unauthorized();

      // Build Token, token contains two claim, the userId and the userName
      var claims = new[]
      {
        new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
        new Claim(ClaimTypes.Name, userFromRepo.Username)
      };

      // Create a security key
      var key = new SymmetricSecurityKey(Encoding.UTF8
        .GetBytes(_config.GetSection("AppSettings:Token").Value));

      // use the created key as part of the signing credential
      // and encrypt the key with a hashing algorithm
      var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

      // create token descriptor
      var tokenDescriptor = new SecurityTokenDescriptor
      {
        Subject = new ClaimsIdentity(claims),
        Expires = System.DateTime.Now.AddDays(1),
        SigningCredentials = credentials
      };

      // create jwt security token handler
      var tokenHandler = new JwtSecurityTokenHandler();

      // use the handler to create the actual token based on the tokenDescriptor
      var token = tokenHandler.CreateToken(tokenDescriptor);

      // use the handler to write the token response and send back to the client
      return Ok(new
      {
        token = tokenHandler.WriteToken(token)
      });
    }
  }
}