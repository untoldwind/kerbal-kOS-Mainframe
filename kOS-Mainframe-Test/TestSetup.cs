using NUnit.Framework;
using kOSMainframe;

namespace kOSMainframeTest {
    [SetUpFixture]
    public class TestSetup {
        [OneTimeSetUp]
        public void DisableLogging() {
            Logging.enabled = false;
        }
    }
}
