﻿using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using Unity;
using Unity.Wcf;

namespace BankGateway.Services.Wcf
{
    public class UnityWebServiceHost : WebServiceHost
    {
        public UnityWebServiceHost(IUnityContainer container, Type serviceType, params Uri[] baseAddresses): base(serviceType, baseAddresses)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            ApplyServiceBehaviors(container);

            ApplyContractBehaviors(container);

            foreach (var contractDescription in ImplementedContracts.Values)
            {
                var contractBehavior =
                    new UnityContractBehavior(new UnityInstanceProvider(container, contractDescription.ContractType));

                contractDescription.Behaviors.Add(contractBehavior);
            }

        }

        private void ApplyContractBehaviors(IUnityContainer container)
        {
            var registeredContractBehaviors = container.ResolveAll<IContractBehavior>();

            foreach (var contractBehavior in registeredContractBehaviors)
            {
                foreach (var contractDescription in ImplementedContracts.Values)
                {
                    contractDescription.Behaviors.Add(contractBehavior);
                }
            }
        }

        private void ApplyServiceBehaviors(IUnityContainer container)
        {
            var registeredServiceBehaviors = container.ResolveAll<IServiceBehavior>();

            foreach (var serviceBehavior in registeredServiceBehaviors)
            {
                Description.Behaviors.Add(serviceBehavior);
            }
        }
       
    }
}