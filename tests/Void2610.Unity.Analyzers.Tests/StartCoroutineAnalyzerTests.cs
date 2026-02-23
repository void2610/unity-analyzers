using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Void2610.Unity.Analyzers.StartCoroutineAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Void2610.Unity.Analyzers.Tests
{
    public class StartCoroutineAnalyzerTests
    {
        // テスト用のMonoBehaviour定義
        private const string MonoBehaviourStub = @"
using System.Collections;
namespace UnityEngine
{
    public class Object { }
    public class Component : Object { }
    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator routine) => null;
        public Coroutine StartCoroutine(string methodName) => null;
    }
    public class Coroutine { }
}
";

        [Fact]
        public async Task StartCoroutineWithMemberAccess_VUA0009()
        {
            // this.StartCoroutine(...) → 検出
            var test = MonoBehaviourStub + @"
public class TestClass : UnityEngine.MonoBehaviour
{
    public void Method()
    {
        {|#0:this.StartCoroutine(MyCoroutine())|};
    }
    private IEnumerator MyCoroutine() { yield break; }
}";
            var expected = Verify.Diagnostic("VUA0009")
                .WithLocation(0);
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task StartCoroutineWithoutThis_VUA0009()
        {
            // StartCoroutine(...) (thisなし) → 検出
            var test = MonoBehaviourStub + @"
public class TestClass : UnityEngine.MonoBehaviour
{
    public void Method()
    {
        {|#0:StartCoroutine(MyCoroutine())|};
    }
    private IEnumerator MyCoroutine() { yield break; }
}";
            var expected = Verify.Diagnostic("VUA0009")
                .WithLocation(0);
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task StartCoroutineWithStringArg_VUA0009()
        {
            // StartCoroutine("MethodName") → 検出
            var test = MonoBehaviourStub + @"
public class TestClass : UnityEngine.MonoBehaviour
{
    public void Method()
    {
        {|#0:StartCoroutine(""MyCoroutine"")|};
    }
    private IEnumerator MyCoroutine() { yield break; }
}";
            var expected = Verify.Diagnostic("VUA0009")
                .WithLocation(0);
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task StartCoroutineOnOtherObject_VUA0009()
        {
            // other.StartCoroutine(...) → 検出
            var test = MonoBehaviourStub + @"
public class TestClass : UnityEngine.MonoBehaviour
{
    private UnityEngine.MonoBehaviour _other;
    public void Method()
    {
        {|#0:_other.StartCoroutine(MyCoroutine())|};
    }
    private IEnumerator MyCoroutine() { yield break; }
}";
            var expected = Verify.Diagnostic("VUA0009")
                .WithLocation(0);
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task OtherMethodCall_NoDiagnostic()
        {
            // StartCoroutine以外のメソッド呼び出し → 検出なし
            var test = MonoBehaviourStub + @"
public class TestClass : UnityEngine.MonoBehaviour
{
    public void Method()
    {
        ToString();
    }
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task UserDefinedStartCoroutine_VUA0009()
        {
            // MonoBehaviour以外のクラスでもStartCoroutineという名前なら検出
            var test = @"
public class MyClass
{
    public void StartCoroutine(string name) { }
    public void Method()
    {
        {|#0:StartCoroutine(""test"")|};
    }
}";
            var expected = Verify.Diagnostic("VUA0009")
                .WithLocation(0);
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}
