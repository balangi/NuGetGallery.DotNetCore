// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using NuGetGallery.Auditing.Obfuscation;
using NuGetGallery.Authentication;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Claims;

namespace NuGetGallery.Auditing;

public class AuditActor
{
    private static string? _localIpAddress;
    private static DateTime _localIpAddressExpiration;
    private const int LocalIpAddressExpirationInMinutes = 10;
    private static readonly object SyncLock = new();

    public string? MachineName { get; set; }

    [Obfuscate(ObfuscationType.IP)]
    public string? MachineIP { get; set; }

    [Obfuscate(ObfuscationType.UserName)]
    public string? UserName { get; set; }
    public string? AuthenticationType { get; set; }
    public string? CredentialKey { get; set; }
    public DateTime TimestampUtc { get; set; }

    public AuditActor? OnBehalfOf { get; set; }

    public AuditActor(string? machineName, string? machineIP, string? userName, string? authenticationType, string? credentialKey, DateTime timeStampUtc)
        : this(machineName, machineIP, userName, authenticationType, credentialKey, timeStampUtc, null) { }

    public AuditActor(string? machineName, string? machineIP, string? userName, string? authenticationType, string? credentialKey, DateTime timeStampUtc, AuditActor? onBehalfOf)
    {
        MachineName = machineName;
        MachineIP = machineIP;
        UserName = userName;
        AuthenticationType = authenticationType;
        TimestampUtc = timeStampUtc;
        OnBehalfOf = onBehalfOf;
        CredentialKey = credentialKey;
    }

    /// <summary>
    /// ایجاد Actor بر اساس HttpContext جاری.
    /// </summary>
    public static Task<AuditActor> GetAspNetOnBehalfOfAsync(HttpContext? context)
    {
        if (context == null)
        {
            // در صورتی که HttpContext در دسترس نباشد، از اطلاعات ماشین استفاده می‌شود
            return GetCurrentMachineActorAsync();
        }

        string? clientIpAddress = null;

        // بررسی هدر X-Forwarded-For (در صورت عبور از Reverse Proxy / Load Balancer)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedForValues))
        {
            var rawIp = forwardedForValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(rawIp))
            {
                // اولین آی‌پی معمولا کلاینت واقعی است
                clientIpAddress = rawIp.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            }
        }

        // در صورت نبود هدر، استفاده از RemoteIpAddress اتصال جاری
        if (string.IsNullOrEmpty(clientIpAddress))
        {
            clientIpAddress = context.Connection.RemoteIpAddress?.ToString();
        }

        clientIpAddress = Obfuscator.ObfuscateIp(clientIpAddress);

        string? user = null;
        string? authType = null;
        string? credentialKey = null;

        if (context.User?.Identity is { IsAuthenticated: true } identity)
        {
            user = identity.Name;
            authType = identity.AuthenticationType;

            if (identity is ClaimsIdentity claimsIdentity)
            {
                credentialKey = claimsIdentity.FindFirst(NuGetClaims.CredentialKey)?.Value;
            }
        }

        return Task.FromResult(new AuditActor(
            machineName: null,
            machineIP: clientIpAddress,
            userName: user,
            authenticationType: authType,
            credentialKey: credentialKey,
            timeStampUtc: DateTime.UtcNow));
    }

    public static Task<AuditActor> GetCurrentMachineActorAsync()
    {
        return GetCurrentMachineActorAsync(null);
    }

    public static async Task<AuditActor> GetCurrentMachineActorAsync(AuditActor? onBehalfOf)
    {
        string? ipAddress = await GetLocalIpAddressAsync();

        return new AuditActor(
            machineName: Environment.MachineName,
            machineIP: ipAddress,
            userName: $@"{Environment.UserDomainName}\{Environment.UserName}",
            authenticationType: "MachineUser",
            credentialKey: string.Empty,
            timeStampUtc: DateTime.UtcNow,
            onBehalfOf: onBehalfOf);
    }

    /// <summary>
    /// دریافت IP محلی سیستم با مکانیزم Cache
    /// </summary>
    public static async Task<string?> GetLocalIpAddressAsync()
    {
        if (string.IsNullOrEmpty(_localIpAddress) || DateTime.UtcNow >= _localIpAddressExpiration)
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                var entry = await Dns.GetHostEntryAsync(Dns.GetHostName());
                if (entry != null && entry.AddressList.Length > 0)
                {
                    lock (SyncLock)
                    {
                        _localIpAddress =
                            TryGetAddress(entry.AddressList, AddressFamily.InterNetworkV6) ??
                            TryGetAddress(entry.AddressList, AddressFamily.InterNetwork);

                        _localIpAddressExpiration = DateTime.UtcNow.AddMinutes(LocalIpAddressExpirationInMinutes);
                    }
                }
            }
        }

        return _localIpAddress;
    }

    private static string? TryGetAddress(IEnumerable<IPAddress> addresses, AddressFamily family)
    {
        return addresses.Where(a => a.AddressFamily == family).Select(a => a.ToString()).FirstOrDefault();
    }
}
