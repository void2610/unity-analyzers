using System.Threading.Tasks;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    Void2610.Unity.Analyzers.EventSystemAnalyzer,
    Void2610.Unity.Analyzers.EventSystemCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Void2610.Unity.Analyzers.Tests
{
    public class EventSystemCodeFixTests
    {
        private const string R3Stubs = @"

namespace R3
{
    public readonly struct Unit { }
    public class Subject<T> { }
}
";

        [Fact]
        public async Task ActionField_ReplacedWithSubject()
        {
            var test = @"
using System;" + R3Stubs + @"
public class TestClass
{
    private Action {|#0:_onDamaged|};
}";
            var fixedCode = @"
using System;" + R3Stubs + @"
public class TestClass
{
    private R3.Subject<R3.Unit> _onDamaged = new();
}";
            var expected = Verify.Diagnostic("VUA1002")
                .WithLocation(0)
                .WithArguments("_onDamaged");
            await Verify.VerifyCodeFixAsync(test, expected, fixedCode);
        }

        [Fact]
        public async Task ActionProperty_ReplacedWithSubject()
        {
            var test = @"
using System;" + R3Stubs + @"
public class TestClass
{
    public Action<int> {|#0:OnDamaged|} { get; set; }
}";
            var fixedCode = @"
using System;" + R3Stubs + @"
public class TestClass
{
    public R3.Subject<int> OnDamaged { get; } = new();
}";
            var expected = Verify.Diagnostic("VUA1002")
                .WithLocation(0)
                .WithArguments("OnDamaged");
            await Verify.VerifyCodeFixAsync(test, expected, fixedCode);
        }
    }
}
