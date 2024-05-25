using Hikaria.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Hikaria.Core.EntityFramework.Repositories
{
    public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected GTFODbContext _dbContext { get; set; }
        protected BaseRepository(GTFODbContext repositoryContext)
        {
            _dbContext = repositoryContext;
        }

        public void Create(T entity)
        {
            _dbContext.Set<T>().Add(entity);
        }
        public void Delete(T entity)
        {
            _dbContext.Set<T>().Remove(entity);
        }
        public void Update(T entity)
        {
            _dbContext.Set<T>().Update(entity);
        }

        public IQueryable<T> FindAll()
        {
            return _dbContext.Set<T>().AsNoTracking();
        }

        public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression)
        {
            return _dbContext.Set<T>().Where(expression).AsNoTracking();
        }
    }
}
