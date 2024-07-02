namespace TheGame.Tests.TestUtils
{
  internal class XunitTestProvider
  {
    /// <summary>
    /// Test Category. Used by CI/CD pipeline to filter Unit and Integration tests.
    /// </summary>
    public const string Category = "Category";

    /// <summary>
    /// Unit Tests
    /// </summary>
    public const string Unit = "Unit";

    /// <summary>
    /// Integration Tests
    /// </summary>
    /// <remarks>
    /// Tests marked as Integration have deterministic output and are safe to run in CI/CD pipeline.
    /// Failing tests usually indicate some sort of regression or outdated specs.
    /// </remarks>
    public const string Integration = "Integration";

    /// <summary>
    /// Dev Tests
    /// </summary>
    /// <remarks>
    /// Tests marked as Dev are non-deterministic and are NOT safe to run in CI/CD pipeline.
    /// Dev tests are intended to help the development process and often require data setup and cleanup before execution.
    /// Consider refactoring dev tests to promote them to integration tests if possible.
    /// </remarks>
    public const string DevTest = "DevTest";
  }
}
