using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace BankGateway.Domain.EF
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly BankGatewayContext _unitOfWork;
        private readonly IDbSet<T> _dbSet;

        public Repository(IUnitOfWork context)
        {
            _unitOfWork = context as BankGatewayContext;
            if (_unitOfWork != null)
                _dbSet = _unitOfWork.Set<T>();

        }

        public IDbSet<T> GetDbSet()
        {
            return _dbSet;
        }



        public T GetById(int id)
        {
            return _dbSet.Find(id);
        }

        public IQueryable<T> GetBy(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Where(predicate);
        }

        public virtual IQueryable<T> GetAll()
        {
            _unitOfWork.Configuration.AutoDetectChangesEnabled = false;
            return _dbSet;
        }

        public void Add(T entity)
        {
            _dbSet.Add(entity);
        }


     

        public void Attach(T entity)
        {
            _dbSet.Attach(entity);
            _unitOfWork.Entry(entity).State = EntityState.Modified;
        }

        public void Delete(T entity)
        {
            if (_unitOfWork.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
        }

        public void Update(T entity, params Expression<Func<T, object>>[] updatedProperties)
        {
            _unitOfWork.Entry(entity).State = EntityState.Unchanged;
            if (updatedProperties.Any())
            {
                foreach (var property in updatedProperties)
                {
                    _unitOfWork.Entry(entity).Property(property).IsModified = true;
                }
            }
            else
            {
                //no items mentioned, so find out the updated entries
                foreach (var property in _unitOfWork.Entry(entity).OriginalValues.PropertyNames)
                {
                    var original = _unitOfWork.Entry(entity).OriginalValues.GetValue<object>(property);
                    var current = _unitOfWork.Entry(entity).CurrentValues.GetValue<object>(property);
                    if (!original.Equals(current))
                        _unitOfWork.Entry(entity).Property(property).IsModified = true;
                }
            }
        }

        public IUnitOfWork Context
        {
            get { return _unitOfWork; }
        }
    }
}