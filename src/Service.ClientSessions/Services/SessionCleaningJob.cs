﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Authorization.NoSql;
using MyJetWallet.Sdk.Authorization.ServiceBus;
using MyJetWallet.Sdk.ServiceBus;
using MyNoSqlServer.Abstractions;
using Service.ClientSessions.Grpc;
using Service.ClientSessions.Grpc.Models;
using Service.ClientSessions.Settings;

namespace Service.ClientSessions.Services
{
    public class SessionCleaningJob
    {
        private readonly ILogger<SessionCleaningJob> _logger;

        private readonly IMyNoSqlServerDataWriter<RootSessionNoSqlEntity> _sessionWriter;
        private readonly IMyNoSqlServerDataWriter<ShortRootSessionNoSqlEntity> _rootSessionWriter;
        private readonly IMyNoSqlServerDataWriter<RootSessionDeviceUidNoSqlEntity> _sessionDeviceUidWriter;
        private readonly IServiceBusPublisher<SessionAuditEvent> _auditPublisher;

        public SessionCleaningJob(
            ILogger<SessionCleaningJob> logger, 
            IMyNoSqlServerDataWriter<RootSessionDeviceUidNoSqlEntity> sessionDeviceUidWriter, 
            IMyNoSqlServerDataWriter<ShortRootSessionNoSqlEntity> rootSessionWriter, 
            IMyNoSqlServerDataWriter<RootSessionNoSqlEntity> sessionWriter, 
            ISubscriber<IReadOnlyList<SessionAuditEvent>> subscriber, 
            IServiceBusPublisher<SessionAuditEvent> auditPublisher)
        {
            _logger = logger;
            _sessionDeviceUidWriter = sessionDeviceUidWriter;
            _rootSessionWriter = rootSessionWriter;
            _sessionWriter = sessionWriter;
            _auditPublisher = auditPublisher;
            subscriber.Subscribe(HandleEvent);
        }

        private async ValueTask HandleEvent(IReadOnlyList<SessionAuditEvent> messages)
        {
            foreach (var message in messages)
            {
                await CleanSessions(message);
                await CleanDeviceUid(message);
            }
        }

        private async Task CleanSessions(SessionAuditEvent message)
        {
            try
            {
                var count = await _rootSessionWriter.GetCountAsync(
                    ShortRootSessionNoSqlEntity.GeneratePartitionKey(message.Session.TraderId));
                if (count > Program.Settings.MaxSessionCount)
                {
                    var sessions =
                        await _sessionWriter.GetAsync(
                            ShortRootSessionNoSqlEntity.GeneratePartitionKey(message.Session.TraderId));
                    var oldestSession = sessions.OrderBy(s => s.CreateTime).First();
                    await Logout(message.Session.TraderId, oldestSession.RootSessionId,
                        $"Drop session by max count. New Session: {message.Session.RootSessionId}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "When cleaning excess sessions");
                throw;
            }
        }

        private async Task CleanDeviceUid(SessionAuditEvent message)
        {
            try
            {
                if (!string.IsNullOrEmpty(message.Session.DeviceUid) &&
                    message.Action != SessionAuditEvent.SessionAction.Refresh)
                {
                    var oldSession = await _sessionDeviceUidWriter.GetAsync(
                        RootSessionDeviceUidNoSqlEntity.GeneratePartitionKey(),
                        RootSessionDeviceUidNoSqlEntity.GenerateRowKey(message.Session.DeviceUid));
                    if (oldSession != null)
                    {
                        await Logout(oldSession.TraderId, oldSession.RootSessionId,
                            $"Drop session by DeviceUid. New Session: {message.Session.RootSessionId}");
                    }

                    var deviceUidEntity = RootSessionDeviceUidNoSqlEntity.Create(message.Session.DeviceUid,
                        message.Session.RootSessionId, message.Session.TraderId);
                    //deviceUidEntity.Expires = message.Session.Expires?.AddMinutes(5);
                    await _sessionDeviceUidWriter.InsertOrReplaceAsync(deviceUidEntity).AsTask();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "When removing sessions with same deviceUid");
                throw;
            }
        }
        
        public async Task Logout(string traderId, Guid rootSessionId, string debugComment)
        {
            var task1 = _sessionWriter.DeleteAsync(RootSessionNoSqlEntity.GeneratePartitionKey(traderId), RootSessionNoSqlEntity.GenerateRowKey(rootSessionId)).AsTask();
            var task2 = _rootSessionWriter.DeleteAsync(RootSessionNoSqlEntity.GeneratePartitionKey(traderId), RootSessionNoSqlEntity.GenerateRowKey(rootSessionId)).AsTask();
            
            await Task.WhenAll(task1, task2);
            
            if (task1.Result != null)
            {
                if (!string.IsNullOrEmpty(task1.Result.DeviceUid))
                {
                    await _sessionDeviceUidWriter.DeleteAsync(RootSessionDeviceUidNoSqlEntity.GeneratePartitionKey(), RootSessionDeviceUidNoSqlEntity.GenerateRowKey(task1.Result.DeviceUid));
                }
                await _auditPublisher.PublishAsync(SessionAuditEvent.Create(SessionAuditEvent.SessionAction.Logout, task1.Result, debugComment));
            }
            else
            {
                await _auditPublisher.PublishAsync(SessionAuditEvent.Create(SessionAuditEvent.SessionAction.Logout, traderId, rootSessionId, debugComment));
            }
        }
    }
}
