using System.Threading.Tasks;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    Void2610.Unity.Analyzers.MotionHandleTryCancelAnalyzer,
    Void2610.Unity.Analyzers.MotionHandleTryCancelCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Void2610.Unity.Analyzers.Tests
{
    public class MotionHandleTryCancelCodeFixTests
    {
        // テスト用のMotionHandle定義
        private const string MotionHandleStub = @"
namespace LitMotion
{
    public struct MotionHandle
    {
        public bool IsActive() => false;
        public void Cancel() { }
        public bool TryCancel() => false;
    }
}
";

        [Fact]
        public async Task WithBlock_ReplacedWithTryCancel()
        {
            // if(handle.IsActive()) { handle.Cancel(); } → handle.TryCancel();
            var test = MotionHandleStub + @"
public class TestClass
{
    private LitMotion.MotionHandle _handle;
    public void Method()
    {
        {|#0:if (_handle.IsActive()) { _handle.Cancel(); }|}
    }
}";
            var fixedCode = MotionHandleStub + @"
public class TestClass
{
    private LitMotion.MotionHandle _handle;
    public void Method()
    {
        _handle.TryCancel();
    }
}";
            var expected = Verify.Diagnostic("VUA0007")
                .WithLocation(0)
                .WithArguments("_handle");
            await Verify.VerifyCodeFixAsync(test, expected, fixedCode);
        }

        [Fact]
        public async Task WithoutBlock_ReplacedWithTryCancel()
        {
            // if(handle.IsActive()) handle.Cancel(); → handle.TryCancel();
            var test = MotionHandleStub + @"
public class TestClass
{
    private LitMotion.MotionHandle _handle;
    public void Method()
    {
        {|#0:if (_handle.IsActive()) _handle.Cancel();|}
    }
}";
            var fixedCode = MotionHandleStub + @"
public class TestClass
{
    private LitMotion.MotionHandle _handle;
    public void Method()
    {
        _handle.TryCancel();
    }
}";
            var expected = Verify.Diagnostic("VUA0007")
                .WithLocation(0)
                .WithArguments("_handle");
            await Verify.VerifyCodeFixAsync(test, expected, fixedCode);
        }
    }
}
