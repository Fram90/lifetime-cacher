using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ObjectLifetimeCacher;

namespace UnitTests
{
    [TestFixture]
    class LifetimeCacheDecoratorTests
    {
        [Test]
        public void WhenRepeatedCallWithSameArgIsMade_SecondCallReturnsDataFromScopedCache()
        {
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                var scopedDecorator = LifetimeCacheDecorator<IInternal>.Create(new Internal());

                var call1Result = scopedDecorator.Write(1);
                var call2Result = scopedDecorator.Write(1);

                var consoleMessages = sw.ToString().Split("\r\n", StringSplitOptions.RemoveEmptyEntries).ToList();

                Assert.AreEqual(1, consoleMessages.Count);
                Assert.AreEqual("RealMethodCall", consoleMessages.Single());
                Assert.AreEqual("1", call1Result);
                Assert.AreEqual("1", call2Result);
            }
        }

        private static IEnumerable<TestCaseData> PositiveTestCaseData => new List<TestCaseData>()
        {
            new TestCaseData(new List<TestArg>() { new TestArg() { Name = "param1" } },new List<TestArg>() { new TestArg() { Name = "param1" } } ),
            new TestCaseData(new[] {1, 2}, new[] {1, 2})
        };

        [TestCaseSource(nameof(PositiveTestCaseData))]
        public void WhenRepeatedCallWithClassArgIsMade_SecondCallReturnsDataFromScopedCache(object input1, object input2)
        {
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                var scopedDecorator = LifetimeCacheDecorator<IInternal>.Create(new Internal());

                var call1Result = scopedDecorator.Write(input1);
                var call2Result = scopedDecorator.Write(input2);

                Console.SetOut(new StreamWriter(Console.OpenStandardError(Int32.MaxValue)));

                var consoleMessages = sw.ToString().Split("\r\n", StringSplitOptions.RemoveEmptyEntries).ToList();

                Assert.AreEqual(1, consoleMessages.Count);
                Assert.AreEqual("RealMethodCall", consoleMessages.Single());
                Assert.AreEqual(call1Result, call2Result);
            }
        }

        private static IEnumerable<TestCaseData> NegativeTestCaseData => new List<TestCaseData>()
        {
            new TestCaseData(new List<TestArg>() { new TestArg() { Name = "param1" } },new List<TestArg>() { new TestArg() { Name = "param2" } } ),
            new TestCaseData(new[] {1, 2}, new[] {2, 1})
        };

        [TestCaseSource(nameof(NegativeTestCaseData))]
        public void WhenArgsAreDifferent_IndependentCallIsMade(object input1, object input2)
        {
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                var scopedDecorator = LifetimeCacheDecorator<IInternal>.Create(new Internal());

                var call1Result = scopedDecorator.Write(input1);
                var call2Result = scopedDecorator.Write(input2);

                Console.SetOut(new StreamWriter(Console.OpenStandardError(Int32.MaxValue)));

                var consoleMessages = sw.ToString().Split("\r\n", StringSplitOptions.RemoveEmptyEntries).ToList();

                Assert.AreEqual(2, consoleMessages.Count);
                Assert.IsTrue(consoleMessages.All(x => x == "RealMethodCall"));
            }
        }

        #region testTypes

        private interface IInternal
        {
            string Write(object arg);
        }

        public class TestArg
        {
            public string Name { get; set; }
        }

        class Internal : IInternal
        {
            public string Write(object arg)
            {
                Console.WriteLine("RealMethodCall");

                if (arg.GetType().GetInterfaces().Any(x => x == typeof(IEnumerable)))
                {
                    var sb = new StringBuilder();
                    foreach (var item in (IEnumerable)arg)
                    {
                        sb.Append(item);
                    }

                    return sb.ToString();
                }

                return arg.ToString();
            }
        }

        #endregion
    }
}