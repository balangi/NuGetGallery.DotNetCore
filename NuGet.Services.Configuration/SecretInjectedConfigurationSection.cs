// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NuGet.Services.KeyVault;

namespace NuGet.Services.Configuration;

public class SecretInjectedConfigurationSection : SecretInjectedConfiguration, IConfigurationSection
{
    public SecretInjectedConfigurationSection(
        IConfigurationSection baseSection,
        ICachingSecretInjector secretInjector,
        ILogger logger)
        : base(baseSection, secretInjector, logger)
    {
    }

    private IConfigurationSection BaseSection => (IConfigurationSection)_baseConfiguration;

    public string Key => BaseSection.Key;

    public string Path => BaseSection.Path;

    public string Value
    {
        get => ConfigurationUtility.InjectCachedSecret(BaseSection.Value, _secretInjector, _logger);
        set => BaseSection.Value = value;
    }
}
