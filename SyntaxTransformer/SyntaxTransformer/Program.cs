using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SyntaxTransformer.Transformers;

namespace SyntaxTransformer
{
    class Program
    {
        static void Main(string[] args)
        {
            Compilation compilation = CreateCompilation(args.Length > 0 ? args[0] : ".");

            foreach (SyntaxTree sourceTree in compilation.SyntaxTrees)
            {
                SemanticModel model = compilation.GetSemanticModel(sourceTree);

                foreach (CSharpSyntaxRewriter rewriter in Transformers(model))
                {
                    SyntaxNode newSource = rewriter.Visit(sourceTree.GetRoot());

                    if (newSource != sourceTree.GetRoot())
                        File.WriteAllText(sourceTree.FilePath, newSource.ToFullString());
                }
            }
        }

        private static Compilation CreateCompilation(string directory)
        {
            IEnumerable<SyntaxTree> sourceTrees = ParseSourceFiles(directory);

            MetadataReference mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            MetadataReference codeAnalysis = MetadataReference.CreateFromFile(typeof(SyntaxTree).Assembly.Location);
            MetadataReference csharpCodeAnalysis = MetadataReference.CreateFromFile(typeof(CSharpSyntaxTree).Assembly.Location);

            MetadataReference[] references = { mscorlib, codeAnalysis, csharpCodeAnalysis };

            return CSharpCompilation.Create("Transformer", sourceTrees, references, new CSharpCompilationOptions(OutputKind.ConsoleApplication));
        }

        private static IEnumerable<SyntaxTree> ParseSourceFiles(string directory)
        {
            if (!File.Exists(directory) && !Directory.Exists(directory))
                throw new ArgumentException("Source not found / does not exist!");

            if (File.Exists(directory))
            {
                yield return CSharpSyntaxTree.ParseText(File.ReadAllText(directory)).WithFilePath(directory);
            }
            else
            {
                // TODO: Run in parallel?
                foreach (string fileName in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
                    yield return CSharpSyntaxTree.ParseText(File.ReadAllText(fileName)).WithFilePath(fileName);
            }

        }

        private static List<CSharpSyntaxRewriter> Transformers(SemanticModel model)
        {
            return new List<CSharpSyntaxRewriter>{
                new ReplaceVarTransformer(model),
                new APIAttributeTransformer(model)
            };
        }
    }
}
