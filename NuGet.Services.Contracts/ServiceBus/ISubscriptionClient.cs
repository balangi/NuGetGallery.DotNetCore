// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.ServiceBus;

public interface ISubscriptionClient
{
    Task StartProcessingAsync(Func<IReceivedBrokeredMessage, Task> onMessageAsync);

    Task StartProcessingAsync(Func<IReceivedBrokeredMessage, Task> onMessageAsync, IOnMessageOptions options);

    Task CloseAsync();
}