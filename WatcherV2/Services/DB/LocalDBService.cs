using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatcherV2.Data;

namespace WatcherV2.Services.DB
{
    public class LocalDBService<T> : ILocalDBService<T> where T : class
    {
        private readonly DataContext _context;
        private readonly DbSet<T> _dbSet;

        public LocalDBService(DataContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }
        public async Task<List<T>> GetAll()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }
        public async Task<T> GetById(int id)
        {
            return await _dbSet.FindAsync(id);
        }
        public async Task<T> GetFistRow()
        {
            return  await _dbSet.AsNoTracking().FirstOrDefaultAsync();
        }
        public async Task<int> Insert(T entity)
        {
            _dbSet.Add(entity);
            return await _context.SaveChangesAsync();
        }
        public async Task<int> Update(T entity, int id)
        {
            // Find the existing entity with the same key
            var existingEntity = await _dbSet.FindAsync(id);

            if (existingEntity != null)
            {
                // Detach the existing entity to avoid tracking conflicts
                _context.Entry(existingEntity).State = EntityState.Detached;
            }

            // Attach the new entity and set its state to Modified
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;

            // Save changes to the database
            return await _context.SaveChangesAsync();
        }
        public async Task<int> Delete(T entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteAll()
        {
            _dbSet.RemoveRange(_dbSet);
            return await _context.SaveChangesAsync();
        }
    }
}
