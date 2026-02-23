using System.Threading.Tasks;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    Void2610.Unity.Analyzers.EnumSummaryAnalyzer,
    Void2610.Unity.Analyzers.EnumSummaryCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Void2610.Unity.Analyzers.Tests
{
    public class EnumSummaryCodeFixTests
    {
        [Fact]
        public async Task SingleEnumMember_SummaryAdded()
        {
            // summaryなし → summaryテンプレート挿入
            var test = @"
public enum GameState
{
    {|#0:Idle|}
}";
            var fixedCode = @"
public enum GameState
{
    /// <summary>  </summary>
    Idle
}";
            var expected = Verify.Diagnostic("VUA4001")
                .WithLocation(0)
                .WithArguments("Idle");
            await Verify.VerifyCodeFixAsync(test, expected, fixedCode);
        }
    }
}
