using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using Z.EntityFramework.Extensions;

namespace BankGateway.Domain.EF
{
    public class BankGatewayContext : DbContext, IUnitOfWork, IDisposable
    {
        protected DbTransaction Transaction;
        private bool _disposed;
        private Dictionary<string, object> _repositories;
        string errorMessage = string.Empty;

        public BankGatewayContext() : base("name=BankGatewayDBEntities")
        {
            //this.Configuration.ProxyCreationEnabled = false;

        }


        public virtual void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified)
        {
            var objectContext = ((IObjectContextAdapter)this).ObjectContext;
            if (objectContext.Connection.State != ConnectionState.Open)
            {
                objectContext.Connection.Open();
            }
            Transaction = objectContext.Connection.BeginTransaction(isolationLevel);
        }


        public bool AutoDetectChangesEnabled
        {
            get
            {
                return Configuration.AutoDetectChangesEnabled;
            }
            set { Configuration.AutoDetectChangesEnabled = value; }
        }

        public virtual bool Commit()
        {
            try
            {
                Transaction.Commit();
                return true;
            }
            catch (Exception e)
            {
                //TODO : LOG ERROR
                Rollback();
                return false;
            }

        }

        public virtual void Rollback()
        {
            Transaction.Rollback();
        }

        public int Save()
        {
            try
            {
                return this.SaveChanges();

            }
            catch (DbEntityValidationException dbEx)
            {

                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMessage += Environment.NewLine + string.Format("Property: {0} Error: {1}",
                                            validationError.PropertyName, validationError.ErrorMessage);
                    }
                }
                throw new Exception(errorMessage, dbEx);
            }
            catch (DbUpdateException ex)
            {
                var stringBuilder = new StringBuilder("A DbUpdateException was caught while saving changes. ");

                try
                {
                    stringBuilder.AppendLine($"DbUpdateException error details - {ex?.InnerException?.InnerException?.Message}");
                    foreach (var eve in ex.Entries)
                    {
                        stringBuilder.AppendLine($"Entity of type {eve.Entity.GetType().Name} in state {eve.State} could not be updated");
                    }

                }
                catch (Exception e)
                {
                    stringBuilder.Append("Error parsing DbUpdateException: " + e.ToString());
                }

                string message = stringBuilder.ToString();
                throw new Exception(message, ex);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public void BulkSaveChange(int batchSize)
        {
            this.BulkSaveChanges(bulk => bulk.BatchSize = batchSize);
        }


        public void CustomBulkInsert<T>(IEnumerable<T> collection, Action<EntityBulkOperation<T>> operationAction) where T : class
        {
            this.BulkInsert<T>(collection, operationAction);
        }

        public void CustomBulkUpdate<T>(IEnumerable<T> collection, Action<EntityBulkOperation<T>> operationAction = null) where T : class
        {
            this.BulkUpdate<T>(collection, operationAction);
        }


        public virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    this.Dispose();
                }
            }
            _disposed = true;
        }

        public Repository<T> Repository<T>() where T : class
        {
            if (_repositories == null)
            {
                _repositories = new Dictionary<string, object>();
            }

            var type = typeof(T).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(Repository<>);
                var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), this);
                _repositories.Add(type, repositoryInstance);
            }
            return (Repository<T>)_repositories[type];
        }


    }
}
