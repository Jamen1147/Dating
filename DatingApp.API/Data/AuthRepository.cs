using System;
using System.Threading.Tasks;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{

  public class AuthRepository : IAuthRepository
  {
    private readonly DataContext _ctx;
    public AuthRepository(DataContext ctx)
    {
      _ctx = ctx;
    }
    public async Task<User> Login(string username, string password)
    {
      var user = await _ctx.Users.FirstOrDefaultAsync(x => x.Username == username);

      if (user == null)
        return null;

      if (!VerifyPasswordHash(password, user.PassowrdHash, user.PasswordSalt))
        return null;

      return user;
    }

    private bool VerifyPasswordHash(string password, byte[] passowrdHash, byte[] passwordSalt)
    {
      using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
      {
        var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        for (int i = 0; i < computedHash.Length; i++)
        {
          if (computedHash[i] != passowrdHash[i])
            return false;
        }
      }
      return true;
    }

    public async Task<User> Register(User user, string password)
    {
      byte[] passwordHash, passwordSalt;
      CreatePasswordHash(password, out passwordHash, out passwordSalt);
      user.PassowrdHash = passwordHash;
      user.PasswordSalt = passwordSalt;

      await _ctx.Users.AddAsync(user);
      await _ctx.SaveChangesAsync();

      return user;
    }

    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
      using (var hmac = new System.Security.Cryptography.HMACSHA512())
      {
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
      }
    }

    public async Task<bool> UserExists(string username)
    {
      return await _ctx.Users.AnyAsync(x => x.Username == username);
    }
  }
}