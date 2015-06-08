using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;


namespace SilverlightChatDuplexService
{
    public class PollingDuplexServiceHostFactory : ServiceHostFactoryBase
    {
        public override ServiceHostBase CreateServiceHost(string constructorString,
            Uri[] baseAddresses)
        {
            return new PollingDuplexSimplexServiceHost(baseAddresses);
        }
    }

    class PollingDuplexSimplexServiceHost : ServiceHost
    {
        public PollingDuplexSimplexServiceHost(params System.Uri[] addresses)
        {
            base.InitializeDescription(typeof(SilverlightChatDuplexService1), new UriSchemeKeyedCollection(addresses));
        }

        protected override void InitializeRuntime()
        {
            // Define the binding and set time-outs.
            PollingDuplexBindingElement pdbe = new PollingDuplexBindingElement()
            {
                ServerPollTimeout = TimeSpan.FromSeconds(3),
                InactivityTimeout = TimeSpan.FromMinutes(1)
            };

            // Add an endpoint for the given service contract.
            this.AddServiceEndpoint(
                typeof(ISilverlightChatDuplexService),
                new CustomBinding(
                    pdbe,
                    new TextMessageEncodingBindingElement(
                        MessageVersion.Soap11,
                        System.Text.Encoding.UTF8),
                    new HttpTransportBindingElement()),
                    "");

            base.InitializeRuntime();
        }
    }
}
