// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Markdig;

namespace NuGet.Services.Messaging.Email;

public class CoreMarkdownMessageService : IMessageService
{
    private static readonly ReadOnlyCollection<TimeSpan> RetryDelays =
        Array.AsReadOnly(new[]
        {
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(10)
        });

    public CoreMarkdownMessageService(
        SmtpClient mailSender,
        IMessageServiceConfiguration configuration)
    {
        MailSender = mailSender
            ?? throw new ArgumentNullException(nameof(mailSender));
        Configuration = configuration
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public SmtpClient MailSender { get; protected set; }

    public IMessageServiceConfiguration Configuration { get; protected set; }

    public async Task SendMessageAsync(
        IEmailBuilder emailBuilder,
        bool copySender = false,
        bool discloseSenderAddress = false)
    {
        ArgumentNullException.ThrowIfNull(emailBuilder);

        using var email = CreateMailMessage(emailBuilder);

        if (!email.To.Any())
        {
            return;
        }

        await SendMessageInternalAsync(email);

        if (copySender && !discloseSenderAddress)
        {
            await SendMessageToSenderAsync(email);
        }
    }

    protected virtual async Task SendMessageInternalAsync(
        MailMessage mailMessage)
    {
        // سه تأخیرِ RetryDelays یعنی حداکثر چهار تلاش برای ارسال.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await AttemptSendMessageAsync(mailMessage, attempt);
                return;
            }
            catch (SmtpException) when (attempt <= RetryDelays.Count)
            {
                await Task.Delay(RetryDelays[attempt - 1]);
            }
        }
    }

    protected virtual Task AttemptSendMessageAsync(
        MailMessage mailMessage,
        int attemptNumber)
    {
        return MailSender.SendMailAsync(mailMessage);
    }

    protected static MailMessage CreateMailMessage(
        IEmailBuilder emailBuilder)
    {
        ArgumentNullException.ThrowIfNull(emailBuilder);

        var mailMessage = new MailMessage
        {
            From = emailBuilder.Sender,
            Subject = emailBuilder.GetSubject(),
            SubjectEncoding = Encoding.UTF8
        };

        SetMarkdownBody(
            mailMessage,
            emailBuilder.GetBody(EmailFormat.Markdown) ?? string.Empty);

        var recipients = emailBuilder.GetRecipients();

        foreach (var address in recipients.To)
        {
            mailMessage.To.Add(address);
        }

        foreach (var address in recipients.CC)
        {
            mailMessage.CC.Add(address);
        }

        foreach (var address in recipients.Bcc)
        {
            mailMessage.Bcc.Add(address);
        }

        foreach (var address in recipients.ReplyTo)
        {
            mailMessage.ReplyToList.Add(address);
        }

        return mailMessage;
    }

    private static void SetMarkdownBody(
        MailMessage mailMessage,
        string markdown)
    {
        // نسخهٔ متنی برای کلاینت‌هایی که HTML نمایش نمی‌دهند.
        mailMessage.Body = markdown;
        mailMessage.BodyEncoding = Encoding.UTF8;
        mailMessage.IsBodyHtml = false;

        // نسخهٔ HTML برای نمایش قالب‌بندی Markdown.
        mailMessage.AlternateViews.Add(
            AlternateView.CreateAlternateViewFromString(
                Markdown.ToHtml(markdown),
                Encoding.UTF8,
                MediaTypeNames.Text.Html));
    }

    private async Task SendMessageToSenderAsync(
        MailMessage mailMessage)
    {
        // کد قبلی با First() در نبود Reply-To خطا می‌داد.
        if (mailMessage.ReplyToList.Count == 0)
        {
            return;
        }

        var replyTo = mailMessage.ReplyToList[0];

        using var senderCopy = new MailMessage(
            Configuration.GalleryOwner,
            replyTo)
        {
            Subject = mailMessage.Subject + " [Sender Copy]",
            SubjectEncoding = Encoding.UTF8
        };

        var copyBody = string.Format(
            CultureInfo.CurrentCulture,
            "You sent the following message via {0}: {1}{1}{2}",
            Configuration.GalleryOwner.DisplayName,
            Environment.NewLine,
            mailMessage.Body);

        SetMarkdownBody(senderCopy, copyBody);
        senderCopy.ReplyToList.Add(replyTo);

        await SendMessageInternalAsync(senderCopy);
    }
}
