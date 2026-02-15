using System.Threading.Tasks;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    Void2610.Unity.Analyzers.SerializeFieldNamingAnalyzer,
    Void2610.Unity.Analyzers.SerializeFieldNamingCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Void2610.Unity.Analyzers.Tests
{
    public class SerializeFieldNamingCodeFixTests
    {
        // テスト用のSerializeField属性定義
        private const string SerializeFieldAttribute = @"
namespace UnityEngine
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class SerializeField : System.Attribute { }
}
";

        [Fact]
        public async Task VUA0002_RemoveUnderscorePrefix()
        {
            // [SerializeField] _maxHealth → maxHealth
            var test = SerializeFieldAttribute + @"
public class TestClass
{
    [UnityEngine.SerializeField] private int {|#0:_maxHealth|};
}";
            var fixedCode = SerializeFieldAttribute + @"
public class TestClass
{
    [UnityEngine.SerializeField] private int maxHealth;
}";
            var expected = Verify.Diagnostic("VUA0002")
                .WithLocation(0)
                .WithArguments("_maxHealth");
            await Verify.VerifyCodeFixAsync(test, expected, fixedCode);
        }

        [Fact]
        public async Task VUA0008_AddUnderscorePrefix()
        {
            // private health → _health
            var test = SerializeFieldAttribute + @"
public class TestClass
{
    private int {|#0:health|};
}";
            var fixedCode = SerializeFieldAttribute + @"
public class TestClass
{
    private int _health;
}";
            var expected = Verify.Diagnostic("VUA0008")
                .WithLocation(0)
                .WithArguments("health");
            await Verify.VerifyCodeFixAsync(test, expected, fixedCode);
        }

        [Fact]
        public async Task VUA0008_AddUnderscorePrefix_WithInitializer()
        {
            // private count = 0 → _count = 0
            var test = SerializeFieldAttribute + @"
public class TestClass
{
    private int {|#0:count|} = 0;
}";
            var fixedCode = SerializeFieldAttribute + @"
public class TestClass
{
    private int _count = 0;
}";
            var expected = Verify.Diagnostic("VUA0008")
                .WithLocation(0)
                .WithArguments("count");
            await Verify.VerifyCodeFixAsync(test, expected, fixedCode);
        }
    }
}
