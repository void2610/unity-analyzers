using System.Threading.Tasks;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    Void2610.Unity.Analyzers.MemberOrderAnalyzer,
    Void2610.Unity.Analyzers.MemberOrderCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Void2610.Unity.Analyzers.Tests
{
    public class MemberOrderCodeFixTests
    {
        [Fact]
        public async Task PublicPropertyAfterPrivateField_Reordered()
        {
            // publicプロパティがprivateフィールドより後 → 順序修正
            var test = @"
public class TestClass
{
    private int _count;

    public int {|#0:Value|} { get; set; }
}";
            var fixedCode = @"
public class TestClass
{

    public int Value { get; set; }
    private int _count;
}";
            var expected = Verify.Diagnostic("VUA0005")
                .WithLocation(0)
                .WithArguments("Value", "public properties", "private fields");
            await Verify.VerifyCodeFixAsync(test, expected, fixedCode);
        }

        [Fact]
        public async Task ConstructorAfterPublicMethod_Reordered()
        {
            // コンストラクタがpublicメソッドの後 → 順序修正
            var test = @"
public class TestClass
{
    private int _count;

    public int GetValue() => _count;

    public {|#0:TestClass|}(int count)
    {
        _count = count;
    }
}";
            var fixedCode = @"
public class TestClass
{
    private int _count;

    public TestClass(int count)
    {
        _count = count;
    }

    public int GetValue() => _count;
}";
            var expected = Verify.Diagnostic("VUA0005")
                .WithLocation(0)
                .WithArguments("TestClass", "constructors", "public methods (one line)");
            await Verify.VerifyCodeFixAsync(test, expected, fixedCode);
        }

        [Fact]
        public async Task UnityEventAfterCleanup_Reordered()
        {
            // Unity eventがcleanupの後 → 順序修正
            var test = @"
public class TestClass
{
    private int _count;

    private void OnDestroy()
    {
        _count = 0;
    }

    private void {|#0:Awake|}()
    {
        _count = 1;
    }
}";
            var fixedCode = @"
public class TestClass
{
    private int _count;

    private void Awake()
    {
        _count = 1;
    }

    private void OnDestroy()
    {
        _count = 0;
    }
}";
            var expected = Verify.Diagnostic("VUA0005")
                .WithLocation(0)
                .WithArguments("Awake", "Unity events", "cleanup");
            await Verify.VerifyCodeFixAsync(test, expected, fixedCode);
        }
    }
}
