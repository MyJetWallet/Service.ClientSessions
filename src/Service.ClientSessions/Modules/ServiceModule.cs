using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using MyJetWallet.Sdk.Authorization.NoSql;
using MyJetWallet.Sdk.Authorization.ServiceBus;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.Abstractions;
using Service.ClientAuditLog.Domain.Models;
using Service.ClientSessions.Services;

namespace Service.ClientSessions.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterMyNoSqlWriter<RootSessionNoSqlEntity>(() => Program.Settings.MyNoSqlWriterUrl,
                RootSessionNoSqlEntity.TableName);
            builder.RegisterMyNoSqlWriter<ShortRootSessionNoSqlEntity>(() => Program.Settings.MyNoSqlWriterUrl,
                ShortRootSessionNoSqlEntity.TableName);
            builder.RegisterMyNoSqlWriter<RootSessionDeviceUidNoSqlEntity>(() => Program.Settings.MyNoSqlWriterUrl,
                RootSessionDeviceUidNoSqlEntity.TableName);
            
            var serviceBusClient = builder.RegisterMyServiceBusTcpClient(Program.ReloadedSettings(e=>e.AuthServiceBusHostPort), Program.LogFactory);
            var queueName = "ClientSessions";

            builder.RegisterMyServiceBusSubscriberBatch<SessionAuditEvent>(serviceBusClient,
                SessionAuditEvent.TopicName, queueName, TopicQueueType.PermanentWithSingleConnection);

            builder.RegisterMyServiceBusPublisher<SessionAuditEvent>(serviceBusClient, SessionAuditEvent.TopicName, true);
            builder.RegisterMyServiceBusPublisher<ClientAuditLogModel>(serviceBusClient, ClientAuditLogModel.TopicName, true);

            builder.RegisterType<SessionCleaningJob>().AsSelf().AutoActivate().SingleInstance();
        }
    }
}