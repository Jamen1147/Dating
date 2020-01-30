using System.Collections.Generic;
using System.Threading.Tasks;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
  public class DatingRepository : IDatingRepository
  {
    private readonly DataContext _ctx;
    public DatingRepository(DataContext ctx)
    {
      _ctx = ctx;
    }
    public void Add<T>(T entity) where T : class
    {
      _ctx.Add(entity);
    }

    public void Delete<T>(T entity) where T : class
    {
      _ctx.Remove(entity);
    }

    public async Task<User> GetUser(int id)
    {
      return await _ctx.Users.Include(p => p.Photos).FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<User>> GetUsers()
    {
      return await _ctx.Users.Include(p => p.Photos).ToListAsync();
    }

    public async Task<bool> SaveAll()
    {
      return await _ctx.SaveChangesAsync() > 0;
    }
  }
}