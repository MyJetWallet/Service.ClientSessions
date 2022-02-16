using System.ServiceModel;
using System.Threading.Tasks;
using Service.ClientSessions.Grpc.Models;

namespace Service.ClientSessions.Grpc
{
    [ServiceContract]
    public interface IHelloService
    {
        [OperationContract]
        Task<HelloMessage> SayHelloAsync(HelloRequest request);
    }
}