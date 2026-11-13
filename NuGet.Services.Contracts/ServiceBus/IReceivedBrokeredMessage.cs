// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.ServiceBus;

public interface IReceivedBrokeredMessage
{
    int DeliveryCount { get; }
    DateTimeOffset ExpiresAtUtc { get; }
    TimeSpan TimeToLive { get; }
    IReadOnlyDictionary<string, object> Properties { get; }
    DateTimeOffset EnqueuedTimeUtc { get; }
    DateTimeOffset ScheduledEnqueueTimeUtc { get; }
    string MessageId { get; }
    Task CompleteAsync();
    Task AbandonAsync();
    string GetBody();
    TStream GetBody<TStream>();
    Stream GetRawBody();
}