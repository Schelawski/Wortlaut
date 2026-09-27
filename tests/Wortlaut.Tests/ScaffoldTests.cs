namespace Wortlaut.Tests;

public class ScaffoldTests
{
    [Fact]
    public void AppAssemblyIsNamedWortlaut()
    {
        Assert.Equal("Wortlaut", typeof(Program).Assembly.GetName().Name);
    }
}
