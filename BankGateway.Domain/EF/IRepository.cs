using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace BankGateway.Domain.EF
{
    public interface IRepository<T> where T : class 
    {
        T GetById(int id);
        IDbSet<T> GetDbSet();
        IQueryable<T> GetBy(Expression<Func<T, bool>> predicate);
        IQueryable<T> GetAll();
        void Add(T entity);
        void Attach(T entity);
        void Delete(T entity);
        IUnitOfWork Context { get; }
        void Update(T entity, params Expression<Func<T, object>>[] updatedProperties);
    }
}