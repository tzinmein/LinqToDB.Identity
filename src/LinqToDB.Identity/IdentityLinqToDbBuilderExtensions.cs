// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	///     Contains extension methods to <see cref="IdentityBuilder" /> for adding linq2db stores.
	/// </summary>
	public static class IdentityLinqToDbBuilderExtensions
	{
		/// <summary>
		///     Adds an linq2db plementation of identity information stores.
		/// </summary>
		/// <param name="builder">The <see cref="IdentityBuilder" /> instance this method extends.</param>
		/// <param name="factory">
		///     <see cref="IConnectionFactory" />
		/// </param>
		/// <returns>The <see cref="IdentityBuilder" /> instance this method extends.</returns>
		// ReSharper disable once InconsistentNaming
		public static IdentityBuilder AddLinqToDBStores(this IdentityBuilder builder, IConnectionFactory factory)
		{
			builder.Services.AddSingleton(factory);

			builder.Services.TryAdd(GetDefaultServices(
				typeof(string), 
				builder.UserType, 
				typeof(IdentityUserClaim<string>), 
				typeof(IdentityUserRole<string>), 
				typeof(IdentityUserLogin<string>), 
				typeof(IdentityUserToken<string>), 
				builder.RoleType, 
				typeof(IdentityRoleClaim<string>)));
			return builder;
		}

		/// <summary>
		///     Adds an linq2db implementation of identity information stores.
		/// </summary>
		/// <typeparam name="TKey">The type of the primary key used for the users and roles.</typeparam>
		/// <param name="builder">The <see cref="IdentityBuilder" /> instance this method extends.</param>
		/// <param name="factory">
		///     <see cref="IConnectionFactory" />
		/// </param>
		/// <returns>The <see cref="IdentityBuilder" /> instance this method extends.</returns>
		// ReSharper disable once InconsistentNaming
		public static IdentityBuilder AddLinqToDBStores<TKey>(this IdentityBuilder builder, IConnectionFactory factory)
			where TKey : IEquatable<TKey>
		{
			builder.Services.AddSingleton(factory);

			builder.Services.TryAdd(GetDefaultServices(
				typeof(TKey),
				builder.UserType,
				typeof(IdentityUserClaim<TKey>),
				typeof(IdentityUserRole<TKey>),
				typeof(IdentityUserLogin<TKey>),
				typeof(IdentityUserToken<TKey>),
				builder.RoleType,
				typeof(IdentityRoleClaim<TKey>))); return builder;
		}

		private static IServiceCollection GetDefaultServices(Type keyType, Type userType, Type userClaimType, Type userRoleType, Type userLoginType, Type userTokenType, Type roleType, Type roleClaimType)
		{
			//UserStore<TKey, TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken>
			var userStoreType = typeof(UserStore<,,,,,,>).MakeGenericType(keyType, userType, roleType, userClaimType, userRoleType, userLoginType, userTokenType);
			// RoleStore<TKey, TRole, TRoleClaim>
			var roleStoreType = typeof(RoleStore<,,>).MakeGenericType(keyType, roleType, roleClaimType);

			var services = new ServiceCollection();
			services.AddScoped(
				typeof(IUserStore<>).MakeGenericType(userType),
				userStoreType);
			services.AddScoped(
				typeof(IRoleStore<>).MakeGenericType(roleType),
				roleStoreType);
			return services;
		}
	}
}