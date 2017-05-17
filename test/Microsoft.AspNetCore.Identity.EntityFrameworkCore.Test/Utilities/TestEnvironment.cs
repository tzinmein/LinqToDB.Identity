// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test.Utilities
{
	public static class TestEnvironment
	{
		static TestEnvironment()
		{
			var configBuilder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("config.json", true)
				.AddJsonFile("config.test.json", true)
				.AddEnvironmentVariables();

			Config = configBuilder.Build();
		}

		public static IConfiguration Config { get; }
	}
}