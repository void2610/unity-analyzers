using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Void2610.Unity.Analyzers.MotionHandleTryCancelAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Void2610.Unity.Analyzers.Tests
{
    public class MotionHandleTryCancelAnalyzerTests
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
        public async Task IfIsActiveThenCancel_WithBlock_VUA0007()
        {
            // if(handle.IsActive()) { handle.Cancel(); } → 検出
            var test = MotionHandleStub + @"
public class TestClass
{
    private LitMotion.MotionHandle _handle;
    public void Method()
    {
        {|#0:if (_handle.IsActive()) { _handle.Cancel(); }|}
    }
}";
            var expected = Verify.Diagnostic("VUA0007")
                .WithLocation(0)
                .WithArguments("_handle");
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task IfIsActiveThenCancel_WithoutBlock_VUA0007()
        {
            // if(handle.IsActive()) handle.Cancel(); → 検出
            var test = MotionHandleStub + @"
public class TestClass
{
    private LitMotion.MotionHandle _handle;
    public void Method()
    {
        {|#0:if (_handle.IsActive()) _handle.Cancel();|}
    }
}";
            var expected = Verify.Diagnostic("VUA0007")
                .WithLocation(0)
                .WithArguments("_handle");
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task TryCancel_NoDiagnostic()
        {
            // TryCancelを使用 → 検出なし
            var test = MotionHandleStub + @"
public class TestClass
{
    private LitMotion.MotionHandle _handle;
    public void Method()
    {
        _handle.TryCancel();
    }
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task IfIsActiveThenCancelWithElse_NoDiagnostic()
        {
            // elseがある場合 → 検出なし
            var test = MotionHandleStub + @"
public class TestClass
{
    private LitMotion.MotionHandle _handle;
    private int _count;
    public void Method()
    {
        if (_handle.IsActive()) _handle.Cancel();
        else _count++;
    }
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task IfIsActiveThenOtherAction_NoDiagnostic()
        {
            // Cancel以外の処理 → 検出なし
            var test = MotionHandleStub + @"
public class TestClass
{
    private LitMotion.MotionHandle _handle;
    private int _count;
    public void Method()
    {
        if (_handle.IsActive()) _count++;
    }
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task IfIsActiveThenCancelDifferentTarget_NoDiagnostic()
        {
            // 異なるオブジェクトへのCancel → 検出なし
            var test = MotionHandleStub + @"
public class TestClass
{
    private LitMotion.MotionHandle _handle1;
    private LitMotion.MotionHandle _handle2;
    public void Method()
    {
        if (_handle1.IsActive()) _handle2.Cancel();
    }
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task IfIsActiveThenMultipleStatements_NoDiagnostic()
        {
            // 複数のステートメントがある場合 → 検出なし
            var test = MotionHandleStub + @"
public class TestClass
{
    private LitMotion.MotionHandle _handle;
    private int _count;
    public void Method()
    {
        if (_handle.IsActive())
        {
            _handle.Cancel();
            _count++;
        }
    }
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task NonMotionHandleIsActiveCancel_VUA0007()
        {
            // MotionHandle以外でも同パターンは検出（構文ベース）
            var test = @"
public class MyHandle
{
    public bool IsActive() => false;
    public void Cancel() { }
}
public class TestClass
{
    private MyHandle _handle;
    public void Method()
    {
        {|#0:if (_handle.IsActive()) _handle.Cancel();|}
    }
}";
            var expected = Verify.Diagnostic("VUA0007")
                .WithLocation(0)
                .WithArguments("_handle");
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}
