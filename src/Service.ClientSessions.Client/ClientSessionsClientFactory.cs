using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.ClientSessions.Grpc;

namespace Service.ClientSessions.Client
{
    [UsedImplicitly]
    public class ClientSessionsClientFactory: MyGrpcClientFactory
    {
        public ClientSessionsClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
        }

        public IHelloService GetHelloService() => CreateGrpcService<IHelloService>();
    }
}
