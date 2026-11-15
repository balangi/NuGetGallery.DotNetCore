// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;

namespace NuGetGallery;

public static class CryptographyService
{
    public static string GenerateHash(
        Stream input,
        string hashAlgorithmId)
    {
        input.Position = 0;

        byte[] hashBytes;

        using (var hashAlgorithm = HashAlgorithm.Create(hashAlgorithmId))
        {
            hashBytes = hashAlgorithm.ComputeHash(input);
        }

        var hash = Convert.ToBase64String(hashBytes);
        return hash;
    }

    public static string GenerateCryptographicToken()
    {
        // تولید یک توکن ایمن با استفاده از تصادفی‌ساز کریپتوگرافیک
        byte[] randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return WebEncoders.Base64UrlEncode(randomBytes);
    }

#if NETFRAMEWORK
    public static string GenerateToken()
    {
        var data = new byte[0x10];

        using (var crypto = new RNGCryptoServiceProvider())
        {
            crypto.GetBytes(data);

            return HttpServerUtility.UrlTokenEncode(data);
        }
    }
#endif
}