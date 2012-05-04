using NUnit.Framework;

namespace EditorUtils.UnitTest
{
    /// <summary>
    /// Standard test base for vim services which wish to do standard error monitoring like
    ///   - No silent swallowed MEF errors
    /// </summary>
    [TestFixture]
    public abstract class EditorTestBase : EditorHost
    {
        [SetUp]
        public virtual void SetupBase()
        {

        }

        [TearDown]
        public virtual void TearDownBase()
        {

        }
    }
}

