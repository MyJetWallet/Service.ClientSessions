using Autofac;
using Service.ClientSessions.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.ClientSessions.Client
{
    public static class AutofacHelper
    {
        public static void RegisterClientSessionsClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new ClientSessionsClientFactory(grpcServiceUrl);

            builder.RegisterInstance(factory.GetHelloService()).As<IHelloService>().SingleInstance();
        }
    }
}
