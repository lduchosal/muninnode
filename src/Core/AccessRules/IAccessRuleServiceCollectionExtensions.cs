// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MuninNode.Server;

namespace MuninNode.AccessRules;

public static class AccessRuleServiceCollectionExtensions
{
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="addressListAllowFrom">The <see cref="IReadOnlyList{T}"/> indicates the read-only list of addresses allowed to access <see cref="MuninServer"/>.</param>
    public static IServiceCollection AddMuninNodeAccessRule(
        this IServiceCollection services,
        IReadOnlyList<IPAddress> addressListAllowFrom
    )
        => AddMuninNodeAccessRule(
            services: services ?? throw new ArgumentNullException(nameof(services)),
            accessRule: new AddressListAccessRule(
                addressListAllowFrom: addressListAllowFrom ??
                                      throw new ArgumentNullException(nameof(addressListAllowFrom))
            )
        );

    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="accessRule">The <see cref="IAccessRule"/> which defines access rules to <see cref="MuninServer"/>.</param>
    public static IServiceCollection AddMuninNodeAccessRule(
        this IServiceCollection services,
        IAccessRule accessRule
    )
    {
        services.TryAdd(
            ServiceDescriptor.Singleton(typeof(IAccessRule), accessRule)
        );

        return services;
    }
}