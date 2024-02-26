using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using Z.EntityFramework.Extensions;

namespace BankGateway.Domain.EF
{
    public interface IUnitOfWork : IDisposable
    {
        //DbContext GatewayContext { get;}
        void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified);
        bool AutoDetectChangesEnabled { get; set; }

        bool Commit();
        void Rollback();
        int Save();

        Repository<T> Repository<T>() where T : class;
        void BulkSaveChange(int batchSize);

        void CustomBulkInsert<T>(IEnumerable<T> collection , Action<EntityBulkOperation<T>> operationAction = null) where T : class;
        void CustomBulkUpdate<T>(IEnumerable<T> collection, Action<EntityBulkOperation<T>> operationAction = null) where T : class;




    }
}