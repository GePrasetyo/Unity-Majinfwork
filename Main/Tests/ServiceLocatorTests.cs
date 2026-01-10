using NUnit.Framework;
using Majinfwork;

namespace Majinfwork.Tests {
    public class ServiceLocatorTests {
        private interface ITestService { }
        private class TestService : ITestService { }

        [TearDown]
        public void TearDown() {
            ServiceLocator.Unregister<ITestService>(out _);
        }

        [Test]
        public void Register_And_Resolve_Returns_Same_Instance() {
            var service = new TestService();
            ServiceLocator.Register<ITestService>(service);

            var resolved = ServiceLocator.Resolve<ITestService>();

            Assert.AreSame(service, resolved);
        }

        [Test]
        public void Resolve_Without_Register_Returns_Null() {
            var resolved = ServiceLocator.Resolve<ITestService>();

            Assert.IsNull(resolved);
        }

        [Test]
        public void Unregister_Removes_Service() {
            var service = new TestService();
            ServiceLocator.Register<ITestService>(service);

            ServiceLocator.Unregister<ITestService>(out string message);
            var resolved = ServiceLocator.Resolve<ITestService>();

            Assert.IsNull(resolved);
        }
    }
}
