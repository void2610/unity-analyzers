using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Void2610.Unity.Analyzers.EventSystemAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Void2610.Unity.Analyzers.Tests
{
    public class EventSystemAnalyzerTests
    {
        [Fact]
        public async Task EventKeyword_VUA1002()
        {
            var test = @"
using System;
public class TestClass
{
    public event EventHandler {|#0:OnDamaged|};
}";
            var expected = Verify.Diagnostic("VUA1002")
                .WithLocation(0)
                .WithArguments("OnDamaged");
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task ActionField_VUA1002()
        {
            var test = @"
using System;
public class TestClass
{
    private Action {|#0:_onDamaged|};
}";
            var expected = Verify.Diagnostic("VUA1002")
                .WithLocation(0)
                .WithArguments("_onDamaged");
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task GenericActionField_VUA1002()
        {
            var test = @"
using System;
public class TestClass
{
    private Action<int> {|#0:_onHealthChanged|};
}";
            var expected = Verify.Diagnostic("VUA1002")
                .WithLocation(0)
                .WithArguments("_onHealthChanged");
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task FuncProperty_VUA1002()
        {
            var test = @"
using System;
public class TestClass
{
    public Func<int> {|#0:GetValue|} { get; set; }
}";
            var expected = Verify.Diagnostic("VUA1002")
                .WithLocation(0)
                .WithArguments("GetValue");
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task SubjectField_NoDiagnostic()
        {
            // R3のSubjectはAction/Funcではないため検出されない
            var test = @"
public class Subject<T> { }
public class TestClass
{
    private Subject<int> _onDamaged;
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task ActionParameter_NoDiagnostic()
        {
            // メソッドパラメータは除外
            var test = @"
using System;
public class TestClass
{
    public void Subscribe(Action callback) { }
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task ActionLocalVariable_NoDiagnostic()
        {
            // ローカル変数は除外
            var test = @"
using System;
public class TestClass
{
    public void Method()
    {
        Action callback = () => { };
    }
}";
            await Verify.VerifyAnalyzerAsync(test);
        }
    }
}
