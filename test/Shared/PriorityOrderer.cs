// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Identity.Test
{
	[AttributeUsage(AttributeTargets.Method)]
	public class TestPriorityAttribute : Attribute
	{
		public TestPriorityAttribute(int priority)
		{
			Priority = priority;
		}

		public int Priority { get; }
	}

	public class PriorityOrderer : ITestCaseOrderer
	{
		public IEnumerable<XunitTestCase> OrderTestCases<XunitTestCase>(IEnumerable<XunitTestCase> testCases)
			where XunitTestCase : ITestCase
		{
			var sortedMethods = new SortedDictionary<int, List<XunitTestCase>>();

			foreach (var testCase in testCases)
			{
				var priority = 0;

				foreach (var attr in testCase.TestMethod.Method.GetCustomAttributes(typeof(TestPriorityAttribute)
					.AssemblyQualifiedName))
					priority = attr.GetNamedArgument<int>("Priority");

				GetOrCreate(sortedMethods, priority).Add(testCase);
			}

			foreach (var list in sortedMethods.Keys.Select(priority => sortedMethods[priority]))
			{
				list.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.TestMethod.Method.Name, y.TestMethod.Method.Name));
				foreach (var testCase in list)
					yield return testCase;
			}
		}

		private static TValue GetOrCreate<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
		{
			TValue result;

			if (dictionary.TryGetValue(key, out result)) return result;

			result = new TValue();
			dictionary[key] = result;

			return result;
		}
	}
}