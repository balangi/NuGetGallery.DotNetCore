// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Azure.Messaging.ServiceBus;
using NuGet.Services.ServiceBus;
using System.Text.Json;

namespace NuGet.Services.Messaging;

public class ServiceBusMessageSerializer : IServiceBusMessageSerializer
{
    private const string EmailMessageSchemaName = "EmailMessageData";

    // GDPR: EmailMessageData cannot live for more than two days because it can contain PII.
    private static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromDays(2);

    public EmailMessageData DeserializeEmailMessageData(ServiceBusReceivedMessage message)
    {
        // خواندن بدنه پیام به صورت جیسون و تبدیل به مدل داده‌ای
        var bodyBytes = message.Body.ToArray();
        var data = JsonSerializer.Deserialize<EmailMessageData1>(bodyBytes)
                   ?? throw new InvalidOperationException("Failed to deserialize EmailMessageData.");

        return new EmailMessageData(
            data.Subject,
            data.PlainTextBody,
            data.HtmlBody,
            data.Sender,
            data.To,
            data.CC,
            data.Bcc,
            data.ReplyTo,
            data.MessageTrackingId,
            (int)message.DeliveryCount);
    }

    public EmailMessageData DeserializeEmailMessageData(IReceivedBrokeredMessage message)
    {
        throw new NotImplementedException();
    }

    public ServiceBusMessage SerializeEmailMessageData(EmailMessageData message)
    {
        var dataModel = new EmailMessageData1
        {
            Subject = message.Subject,
            PlainTextBody = message.PlainTextBody,
            HtmlBody = message.HtmlBody,
            Sender = message.Sender,
            To = message.To,
            CC = message.CC,
            Bcc = message.Bcc,
            ReplyTo = message.ReplyTo,
            MessageTrackingId = message.MessageTrackingId
        };

        var jsonString = JsonSerializer.Serialize(dataModel);
        var serviceBusMessage = new ServiceBusMessage(jsonString)
        {
            ContentType = "application/json",
            TimeToLive = DefaultTimeToLive
        };

        // ذخیره اسکما در صورت نیاز در ApplicationProperties
        serviceBusMessage.ApplicationProperties["SchemaName"] = EmailMessageSchemaName;
        serviceBusMessage.ApplicationProperties["SchemaVersion"] = 1;

        return serviceBusMessage;
    }

    IBrokeredMessage IServiceBusMessageSerializer.SerializeEmailMessageData(EmailMessageData message)
    {
        throw new NotImplementedException();
    }

    private class EmailMessageData1
    {
        public string Subject { get; set; }
        public string PlainTextBody { get; set; }
        public string HtmlBody { get; set; }
        public string Sender { get; set; }
        public Guid MessageTrackingId { get; set; }
        public IReadOnlyList<string> To { get; set; }
        public IReadOnlyList<string> CC { get; set; }
        public IReadOnlyList<string> Bcc { get; set; }
        public IReadOnlyList<string> ReplyTo { get; set; }
    }
}
