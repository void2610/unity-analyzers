using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Void2610.Unity.Analyzers.SerializeFieldNamingAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Void2610.Unity.Analyzers.Tests
{
    public class SerializeFieldNamingAnalyzerTests
    {
        // テスト用のUnity属性定義
        private const string SerializeFieldAttribute = @"
namespace UnityEngine
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class SerializeField : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class SerializeReference : System.Attribute { }
}
";

        [Fact]
        public async Task PrivateFieldWithUnderscore_NoDiagnostic()
        {
            var test = SerializeFieldAttribute + @"
public class TestClass
{
    private int _health;
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task PrivateFieldWithoutUnderscore_VUA2002()
        {
            var test = SerializeFieldAttribute + @"
public class TestClass
{
    private int {|#0:health|};
}";
            var expected = Verify.Diagnostic("VUA2002")
                .WithLocation(0)
                .WithArguments("health");
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task SerializeFieldWithoutUnderscore_NoDiagnostic()
        {
            var test = SerializeFieldAttribute + @"
public class TestClass
{
    [UnityEngine.SerializeField] private int maxHealth;
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task SerializeFieldWithUnderscore_VUA2001()
        {
            var test = SerializeFieldAttribute + @"
public class TestClass
{
    [UnityEngine.SerializeField] private int {|#0:_maxHealth|};
}";
            var expected = Verify.Diagnostic("VUA2001")
                .WithLocation(0)
                .WithArguments("_maxHealth");
            await Verify.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task ConstField_NoDiagnostic()
        {
            var test = SerializeFieldAttribute + @"
public class TestClass
{
    private const int MaxValue = 100;
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task StaticField_NoDiagnostic()
        {
            var test = SerializeFieldAttribute + @"
public class TestClass
{
    private static int counter;
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task PublicField_NoDiagnostic()
        {
            var test = SerializeFieldAttribute + @"
public class TestClass
{
    public int health;
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task InternalField_NoDiagnostic()
        {
            var test = SerializeFieldAttribute + @"
public class TestClass
{
    internal int health;
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task SerializeReferenceWithoutUnderscore_NoDiagnostic()
        {
            var test = SerializeFieldAttribute + @"
public class TestClass
{
    [UnityEngine.SerializeReference] private object myRef;
}";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task SerializeReferenceWithUnderscore_VUA2001()
        {
            var test = SerializeFieldAttribute + @"
public class TestClass
{
    [UnityEngine.SerializeReference] private object {|#0:_myRef|};
}";
            var expected = Verify.Diagnostic("VUA2001")
                .WithLocation(0)
                .WithArguments("_myRef");
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}
