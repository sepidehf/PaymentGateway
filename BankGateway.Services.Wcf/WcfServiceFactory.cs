using BankGateway.Domain.EF;
using BankGateway.Domain.Entities;
using BankGateway.Domain.Factory;
using BankGateway.Domain.Infrastracture;
using BankGateway.Domain.Services;
using BankGateway.Domain.Services.Interface;
using Core.Infrastructure.Common;
using Microsoft.Practices.Unity;
using Unity.Wcf;

namespace BankGateway.Services.Wcf
{
    public class WcfServiceFactory : UnityServiceHostFactory
    {
        protected override void ConfigureContainer(IUnityContainer container)
        {
            log4net.Config.XmlConfigurator.Configure();
            container.RegisterType<IBankGatewayService, BankGatewayService>();
            container.RegisterType<IHttpClientFactory, HttpClientFactory>();
            container.RegisterType<IBankService, BaamService>();
            container.RegisterType(typeof(IRepository<>), typeof(Repository<>));
            container.RegisterType<IProcessOrderService,OrderService>();
            container.RegisterType<IUnitOfWork,BankGatewayContext>(new ContainerControlledLifetimeManager());
            container.RegisterType<IRecordService, RecordService>();
            container.RegisterType<IPackageService, PackageService>();
            container.RegisterType<IBankServiceFactory, BankServiceFactory>(new InjectionConstructor(typeof(IUnityContainer)));
            container.RegisterType<IApplicationService, ApplicationService>();
           // HibernatingRhinos.Profiler.Appender.EntityFramework.EntityFrameworkProfiler.Initialize();

        }


    }
}