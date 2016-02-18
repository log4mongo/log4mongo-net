using System;
using NUnit.Framework;
using SharpTestsEx;

namespace Log4Mongo.Tests
{
	[TestFixture]
	public class UnitResolverTest
	{
		private readonly string[] _invalidValues = new [] { "1b", "2abc", "xyz", null };

		[TestCase("1", 1)]
		[TestCase("2k", 2000)]
		[TestCase("5MB", 5242880)]
		public void should_resolve_units(string value, int expected)
		{
			var sut = new UnitResolver();

			var actual = sut.Resolve(value);

			actual.Should().Be(expected);
		}

		[TestCaseSource("_invalidValues")]
		public void should_not_throw(string value)
		{
			var sut = new UnitResolver();

			Action action = () => sut.Resolve(value);

			action.Should().NotThrow();
		}

		[TestCaseSource("_invalidValues")]
		public void should_return_0(string value)
		{
			var sut = new UnitResolver();

			var actual = sut.Resolve(value);

			actual.Should().Be(0);
		}
	}
}