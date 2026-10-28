using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherV2.Services.DB
{
    public interface ILocalDBService<T> where T : class
    {
        Task <List<T>> GetAll();
        Task<T> GetById(int id);
        Task <T> GetFistRow();
        Task<int> Insert(T entity);
        Task<int> Update(T entity, int id);
        Task<int> Delete(T entity);
        Task<int> DeleteAll();
    }
}
