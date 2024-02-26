namespace BankGateway.Domain.Factory
{

    using Services;
    using Services.Interface;
    using Microsoft.Practices.Unity;

    public class BankServiceFactory : IBankServiceFactory
    {
        private readonly IUnityContainer _unityContainer;

        public BankServiceFactory(IUnityContainer container)
        {
            _unityContainer = container;
        }

        public IBankService Create(string bankName)
        {
            switch (bankName)
            {
                case "Baam":
                    return _unityContainer.Resolve<BaamService>();
                //case BankName.Mellat:
                //    return _unityContainer.Resolve<MellatBankService>();
                default:
                    return null;

            }
        }
    }
}
