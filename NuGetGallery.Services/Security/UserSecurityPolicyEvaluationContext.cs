// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using NuGet.Services.Entities;

namespace NuGetGallery.Security;

/// <summary>
/// Context providing user security policy handlers with resources necessary for policy evaluation.
/// </summary>
public class UserSecurityPolicyEvaluationContext
{
    private readonly Lazy<HttpContext> _httpContext;
    private readonly User _sourceAccount;
    private readonly User _targetAccount;

    /// <summary>
    /// Current http context. This has been required for some user security policies in order
    /// to get the current user and/or current request details.
    /// </summary>
    public HttpContext HttpContext => _httpContext.Value;

    /// <summary>
    /// Current user.
    /// </summary>
    public User CurrentUser => _currentUser;

    /// <summary>
    /// Account where the security policy came from.
    /// </summary>
    public User SourceAccount => _sourceAccount ?? CurrentUser;

    /// <summary>
    /// Account under policy evaluation.
    /// </summary>
    public User TargetAccount => _targetAccount ?? CurrentUser;

    /// <summary>
    /// Security policies to be evaluated.
    /// </summary>
    public IEnumerable<UserSecurityPolicy> Policies { get; }

    /// <summary>
    /// Create a policy (user) context, which uses the HttpContext or IHttpContextAccessor.
    /// </summary>
    public UserSecurityPolicyEvaluationContext(
        IEnumerable<UserSecurityPolicy> policies,
        HttpContext httpContext)
    {
        Policies = policies ?? throw new ArgumentNullException(nameof(policies));
        _httpContext = new Lazy<HttpContext>(() => httpContext);
    }

    /// <summary>
    /// Create a policy context using IHttpContextAccessor.
    /// </summary>
    public UserSecurityPolicyEvaluationContext(
        IEnumerable<UserSecurityPolicy> policies,
        IHttpContextAccessor httpContextAccessor)
    {
        Policies = policies ?? throw new ArgumentNullException(nameof(policies));
        _httpContext = new Lazy<HttpContext>(() => httpContextAccessor?.HttpContext);
    }

    private readonly User _currentUser;

    public UserSecurityPolicyEvaluationContext(
        IEnumerable<UserSecurityPolicy> policies,
        User currentUser,
        HttpContext httpContext)
    {
        Policies = policies ?? throw new ArgumentNullException(nameof(policies));
        _currentUser = currentUser;
        _httpContext = new Lazy<HttpContext>(() => httpContext);
    }


    /// <summary>
    /// Create a policy (organization) context, which requires the source (organization) and target (member) accounts for context.
    /// </summary>
    public UserSecurityPolicyEvaluationContext(
        IEnumerable<UserSecurityPolicy> policies,
        User sourceAccount,
        User targetAccount,
        HttpContext httpContext = null)
        : this(policies, httpContext)
    {
        _sourceAccount = sourceAccount;
        _targetAccount = targetAccount;
    }
}
