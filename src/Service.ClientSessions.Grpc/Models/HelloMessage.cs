using System.Runtime.Serialization;
using Service.ClientSessions.Domain.Models;

namespace Service.ClientSessions.Grpc.Models
{
    [DataContract]
    public class HelloMessage : IHelloMessage
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }
    }
}