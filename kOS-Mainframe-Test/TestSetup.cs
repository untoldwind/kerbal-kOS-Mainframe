using NUnit.Framework;
using kOSMainframe;

namespace kOSMainframeTest {
    [SetUpFixture]
    public class TestSetup {
        [OneTimeSetUp]
        public void DisableLogging() {
            Logging.backend = new TestContextLoggingBackend();
        }
    }

    class TestContextLoggingBackend : ILoggingBackend {
        public void Log(string line) {
            TestContext.WriteLine(line);
        }
    }
}
