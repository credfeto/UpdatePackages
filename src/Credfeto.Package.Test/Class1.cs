using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Package.Test;

public sealed class Class1 : TestBase
{
    [Fact]
    public void Test1()
    {
        Assert.True(condition: true, userMessage: "banana");
    }
}