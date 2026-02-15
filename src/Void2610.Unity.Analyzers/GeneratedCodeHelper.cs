using Microsoft.CodeAnalysis;

namespace Void2610.Unity.Analyzers
{
    /// <summary>
    /// Roslynが標準で検出できない生成コードパターンを補助的に判定するヘルパー
    /// </summary>
    internal static class GeneratedCodeHelper
    {
        /// <summary>
        /// SyntaxTreeのファイルパスからソースジェネレータ出力かどうかを判定
        /// </summary>
        internal static bool IsGenerated(SyntaxTree tree)
        {
            var filePath = tree.FilePath;
            if (string.IsNullOrEmpty(filePath))
                return false;

            return filePath.EndsWith("_Gen.cs") || filePath.Contains("Generator");
        }

        /// <summary>
        /// シンボルの定義元がソースジェネレータ出力かどうかを判定
        /// </summary>
        internal static bool IsGenerated(ISymbol symbol)
        {
            foreach (var location in symbol.Locations)
            {
                if (location.SourceTree != null && IsGenerated(location.SourceTree))
                    return true;
            }
            return false;
        }
    }
}
