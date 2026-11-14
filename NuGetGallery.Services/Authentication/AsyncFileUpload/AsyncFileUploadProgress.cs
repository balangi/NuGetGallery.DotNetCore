// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.Serialization;
using System.Text.Json.Serialization;   // ← جای System.Web.Script.Serialization

namespace NuGetGallery.AsyncFileUpload;

[DataContract]
public class AsyncFileUploadProgress
{
    internal AsyncFileUploadProgress(int contentLength)
    {
        ContentLength = contentLength;
    }

    [JsonIgnore]
    [DataMember]
    public int ContentLength { get; set; }

    [JsonIgnore]
    [DataMember]
    public int TotalBytesRead { get; set; }

    [DataMember]
    public string? FileName { get; set; }   // nullable برای NRT در .NET 8/9/10

    /// <summary>درصد پیشرفت (فقط خواندنی؛ در JSON به‌صورت محاسبه‌شده ارسال می‌شود)</summary>
    public int Progress
    {
        get
        {
            if (ContentLength == 0)
            {
                return 0;
            }

            return (int)((long)TotalBytesRead * 100 / ContentLength);
        }
    }

    [JsonIgnore]
    public int BytesRemaining => ContentLength - TotalBytesRead;
}
