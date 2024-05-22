using Hikaria.Core.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Hikaria.Core.EntityFramework.Repositories
{
    public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected GTFODbContext GTFODbContext { get; set; }
        protected BaseRepository(GTFODbContext repositoryContext)
        {
            GTFODbContext = repositoryContext;
        }

        public void Create(T entity)
        {
            GTFODbContext.Set<T>().Add(entity);
        }
        public void Delete(T entity)
        {
            GTFODbContext.Set<T>().Remove(entity);
        }
        public void Update(T entity)
        {
            GTFODbContext.Set<T>().Update(entity);
        }

        public IQueryable<T> FindAll()
        {
            return GTFODbContext.Set<T>().AsNoTracking();
        }

        public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression)
        {
            return GTFODbContext.Set<T>().Where(expression).AsNoTracking();
        }
    }
}
