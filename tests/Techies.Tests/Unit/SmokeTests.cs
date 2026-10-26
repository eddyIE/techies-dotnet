namespace Techies.Tests.Unit;

public class SmokeTests
{
    [Fact]
    public void Test_project_references_web_project() =>
        Assert.Equal("Techies.Web", typeof(Program).Assembly.GetName().Name);
}
