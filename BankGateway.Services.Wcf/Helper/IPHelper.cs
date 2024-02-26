using System.ServiceModel;
using System.ServiceModel.Channels;

namespace BankGateway.Services.Wcf.Helper
{
    public static class IPHelper
    {
        public static string IPPort
        {
            get
            {
                //OperationContext oOperationContext = OperationContext.Current;
                //MessageProperties oMessageProperties = oOperationContext.IncomingMessageProperties;
                //RemoteEndpointMessageProperty oRemoteEndpointMessageProperty = (RemoteEndpointMessageProperty)oMessageProperties[RemoteEndpointMessageProperty.Name];
                //string sourceAddress = oRemoteEndpointMessageProperty.Address;
                //int sourcePort = oRemoteEndpointMessageProperty.Port;
                //return $"{sourceAddress} : {sourcePort}";

                OperationContext context = OperationContext.Current;
                MessageProperties properties = context.IncomingMessageProperties;
                RemoteEndpointMessageProperty endpoint = properties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                string address = string.Empty;
                //http://www.simosh.com/article/ddbggghj-get-client-ip-address-using-wcf-4-5-remoteendpointmessageproperty-in-load-balanc.html
                if (properties.Keys.Contains(HttpRequestMessageProperty.Name))
                {
                    HttpRequestMessageProperty endpointLoadBalancer = properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
                    if (endpointLoadBalancer != null && endpointLoadBalancer.Headers["X-Forwarded-For"] != null)
                        return endpointLoadBalancer.Headers["X-Forwarded-For"];
                    return "";
                }

                return endpoint.Address;

            }
        }
    }
}