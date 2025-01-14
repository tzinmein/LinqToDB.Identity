// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test
{
	public class InMemoryEFUserStoreTestWithGenerics :
		UserManagerTestBase<IdentityUserWithGenerics, MyIdentityRole, string>, IDisposable, IClassFixture<InMemoryStorage>
	{
		public InMemoryEFUserStoreTestWithGenerics(InMemoryStorage storage)
		{
			var services = new ServiceCollection();
			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
			//services.AddDbContext<InMemoryContextWithGenerics>(options => options.UseInMemoryDatabase());
//            _context = services.BuildServiceProvider().GetRequiredService<InMemoryContextWithGenerics>();
			_storage = storage;
		}

		public void Dispose()
		{
		}

//        private readonly InMemoryContextWithGenerics _context;
		private UserStoreWithGenerics _store;

		private readonly InMemoryStorage _storage;

		protected override TestConnectionFactory CreateTestContext()
		{
			var connectionString = _storage.ConnectionString; //"Data Source=:memory:;";

			var factory = new TestConnectionFactory(new SQLiteDP(ProviderName.SQLite), "InMemoryEFUserStoreTestWithGenerics",
				connectionString);
			CreateTables(factory, connectionString);
			return factory;
		}

		protected override void AddUserStore(IServiceCollection services, TestConnectionFactory context = null)
		{
			_store = new UserStoreWithGenerics(context ?? CreateTestContext(), "TestContext");
			services.AddSingleton<IUserStore<IdentityUserWithGenerics>>(_store);
		}

		protected override void AddRoleStore(IServiceCollection services, TestConnectionFactory context = null)
		{
			services.AddSingleton<IRoleStore<MyIdentityRole>>(
				new RoleStoreWithGenerics(context ?? CreateTestContext(), "TestContext"));
		}

		protected override IdentityUserWithGenerics CreateTestUser(string namePrefix = "", string email = "",
			string phoneNumber = "",
			bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = default(DateTimeOffset?),
			bool useNamePrefixAsUserName = false)
		{
			return new IdentityUserWithGenerics
			{
				UserName = useNamePrefixAsUserName ? namePrefix : string.Format("{0}{1}", namePrefix, Guid.NewGuid()),
				Email = email,
				PhoneNumber = phoneNumber,
				LockoutEnabled = lockoutEnabled,
				LockoutEnd = lockoutEnd
			};
		}

		protected override MyIdentityRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false)
		{
			var roleName = useRoleNamePrefixAsRoleName
				? roleNamePrefix
				: string.Format("{0}{1}", roleNamePrefix, Guid.NewGuid());
			return new MyIdentityRole(roleName);
		}

		protected override void SetUserPasswordHash(IdentityUserWithGenerics user, string hashedPassword)
		{
			user.PasswordHash = hashedPassword;
		}

		protected override Expression<Func<IdentityUserWithGenerics, bool>> UserNameEqualsPredicate(string userName)
		{
			return u => u.UserName == userName;
		}

		protected override Expression<Func<MyIdentityRole, bool>> RoleNameEqualsPredicate(string roleName)
		{
			return r => r.Name == roleName;
		}

		protected override Expression<Func<IdentityUserWithGenerics, bool>> UserNameStartsWithPredicate(string userName)
		{
			return u => u.UserName.StartsWith(userName);
		}

		protected override Expression<Func<MyIdentityRole, bool>> RoleNameStartsWithPredicate(string roleName)
		{
			return r => r.Name.StartsWith(roleName);
		}

		[Fact]
		public async Task CanAddRemoveUserClaimWithIssuer()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			Claim[] claims =
				{new Claim("c1", "v1", null, "i1"), new Claim("c2", "v2", null, "i2"), new Claim("c2", "v3", null, "i3")};
			foreach (var c in claims)
				IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));

			var userId = await manager.GetUserIdAsync(user);
			var userClaims = await manager.GetClaimsAsync(user);
			Assert.Equal(3, userClaims.Count);
			Assert.Equal(3, userClaims.Intersect(claims, ClaimEqualityComparer.Default).Count());

			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[0]));
			userClaims = await manager.GetClaimsAsync(user);
			Assert.Equal(2, userClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[1]));
			userClaims = await manager.GetClaimsAsync(user);
			Assert.Equal(1, userClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[2]));
			userClaims = await manager.GetClaimsAsync(user);
			Assert.Equal(0, userClaims.Count);
		}

		[Fact]
		public async Task CanReplaceUserClaimWithIssuer()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, new Claim("c", "a", "i")));
			var userClaims = await manager.GetClaimsAsync(user);
			Assert.Equal(1, userClaims.Count);
			var claim = new Claim("c", "b", "i");
			var oldClaim = userClaims.FirstOrDefault();
			IdentityResultAssert.IsSuccess(await manager.ReplaceClaimAsync(user, oldClaim, claim));
			var newUserClaims = await manager.GetClaimsAsync(user);
			Assert.Equal(1, newUserClaims.Count);
			var newClaim = newUserClaims.FirstOrDefault();
			Assert.Equal(claim.Type, newClaim.Type);
			Assert.Equal(claim.Value, newClaim.Value);
			Assert.Equal(claim.Issuer, newClaim.Issuer);
		}

		[Fact]
		public async Task RemoveClaimWithIssuerOnlyAffectsUser()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			var user2 = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
			Claim[] claims =
				{new Claim("c", "v", null, "i1"), new Claim("c2", "v2", null, "i2"), new Claim("c2", "v3", null, "i3")};
			foreach (var c in claims)
			{
				IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
				IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user2, c));
			}
			var userClaims = await manager.GetClaimsAsync(user);
			Assert.Equal(3, userClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[0]));
			userClaims = await manager.GetClaimsAsync(user);
			Assert.Equal(2, userClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[1]));
			userClaims = await manager.GetClaimsAsync(user);
			Assert.Equal(1, userClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[2]));
			userClaims = await manager.GetClaimsAsync(user);
			Assert.Equal(0, userClaims.Count);
			var userClaims2 = await manager.GetClaimsAsync(user2);
			Assert.Equal(3, userClaims2.Count);
		}
	}

	public class ClaimEqualityComparer : IEqualityComparer<Claim>
	{
		public static IEqualityComparer<Claim> Default = new ClaimEqualityComparer();

		public bool Equals(Claim x, Claim y)
		{
			return x.Value == y.Value && x.Type == y.Type && x.Issuer == y.Issuer;
		}

		public int GetHashCode(Claim obj)
		{
			return 1;
		}
	}


	#region Generic Type defintions

	public class IdentityUserWithGenerics : LinqToDB.Identity.IdentityUser<string, IdentityUserClaimWithIssuer, IdentityUserRoleWithDate,
		IdentityUserLoginWithContext>
	{
		public IdentityUserWithGenerics()
		{
			Id = Guid.NewGuid().ToString();
		}
	}

	public class UserStoreWithGenerics : UserStore<string, IdentityUserWithGenerics, MyIdentityRole,
		IdentityUserClaimWithIssuer, IdentityUserRoleWithDate, IdentityUserLoginWithContext,
		IdentityUserTokenWithStuff>
	{
		public UserStoreWithGenerics(TestConnectionFactory factory, string loginContext)
			: base(factory)
		{
			LoginContext = loginContext;
			factory.CreateTable<IdentityUserClaimWithIssuer>();
			factory.CreateTable<IdentityUserRoleWithDate>();
			factory.CreateTable<IdentityUserLoginWithContext>();
			factory.CreateTable<IdentityUserTokenWithStuff>();
		}

		public string LoginContext { get; set; }

		protected override IdentityUserRoleWithDate CreateUserRole(IdentityUserWithGenerics user, MyIdentityRole role)
		{
			return new IdentityUserRoleWithDate
			{
				RoleId = role.Id,
				UserId = user.Id,
				Created = DateTime.UtcNow
			};
		}

		protected override IdentityUserClaimWithIssuer CreateUserClaim(IdentityUserWithGenerics user, Claim claim)
		{
			return new IdentityUserClaimWithIssuer
			{
				UserId = user.Id,
				ClaimType = claim.Type,
				ClaimValue = claim.Value,
				Issuer = claim.Issuer
			};
		}

		protected override IdentityUserLoginWithContext CreateUserLogin(IdentityUserWithGenerics user, UserLoginInfo login)
		{
			return new IdentityUserLoginWithContext
			{
				UserId = user.Id,
				ProviderKey = login.ProviderKey,
				LoginProvider = login.LoginProvider,
				ProviderDisplayName = login.ProviderDisplayName,
				Context = LoginContext
			};
		}

		protected override IdentityUserTokenWithStuff CreateUserToken(IdentityUserWithGenerics user, string loginProvider,
			string name, string value)
		{
			return new IdentityUserTokenWithStuff
			{
				UserId = user.Id,
				LoginProvider = loginProvider,
				Name = name,
				Value = value,
				Stuff = "stuff"
			};
		}
	}

	public class RoleStoreWithGenerics : RoleStore<string, MyIdentityRole, LinqToDB.Identity.IdentityRoleClaim<string>>
	{
		private string _loginContext;

		public RoleStoreWithGenerics(TestConnectionFactory factory, string loginContext) : base(factory)
		{
			_loginContext = loginContext;
			factory.CreateTable<IdentityRoleClaim<string>>();
			factory.CreateTable<IdentityUserRoleWithDate>();
		}

		protected override LinqToDB.Identity.IdentityRoleClaim<string> CreateRoleClaim(MyIdentityRole role, Claim claim)
		{
			return new LinqToDB.Identity.IdentityRoleClaim<string> {RoleId = role.Id, ClaimType = claim.Type, ClaimValue = claim.Value};
		}
	}

	public class IdentityUserClaimWithIssuer : LinqToDB.Identity.IdentityUserClaim<string>
	{
		public string Issuer { get; set; }

		public override Claim ToClaim()
		{
			return new Claim(ClaimType, ClaimValue, null, Issuer);
		}

		public override void InitializeFromClaim(Claim other)
		{
			ClaimValue = other.Value;
			ClaimType = other.Type;
			Issuer = other.Issuer;
		}
	}

	public class IdentityUserRoleWithDate : LinqToDB.Identity.IdentityUserRole<string>
	{
		public DateTime Created { get; set; }
	}

	public class MyIdentityRole : LinqToDB.Identity.IdentityRole<string, IdentityUserRoleWithDate, LinqToDB.Identity.IdentityRoleClaim<string>>
	{
		public MyIdentityRole()
		{
			Id = Guid.NewGuid().ToString();
		}

		public MyIdentityRole(string roleName) : this()
		{
			Name = roleName;
		}
	}

	public class IdentityUserTokenWithStuff : LinqToDB.Identity.IdentityUserToken<string>
	{
		public string Stuff { get; set; }
	}

	public class IdentityUserLoginWithContext : LinqToDB.Identity.IdentityUserLogin<string>
	{
		public string Context { get; set; }
	}

	//public class InMemoryContextWithGenerics : InMemoryContext<IdentityUserWithGenerics, MyIdentityRole, string, IdentityUserClaimWithIssuer, IdentityUserRoleWithDate, IdentityUserLoginWithContext, IdentityRoleClaim<string>, IdentityUserTokenWithStuff>
	//{

	//}

	#endregion
}