using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using Unity;

namespace BankGateway.Services.Wcf
{
    public abstract class UnityWebServiceHostFactory : WebServiceHostFactory
    {
        protected abstract void ConfigureContainer(IUnityContainer container);

        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            var container = new UnityContainer();

            ConfigureContainer(container);

            return new UnityWebServiceHost(container, serviceType, baseAddresses);
        }
    }
}