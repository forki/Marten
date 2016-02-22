using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Baseline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Marten.Codegen
{
    public class AssemblyGenerator
    {
        private readonly IList<MetadataReference> _references = new List<MetadataReference>();

        public static string[] HintPaths { get; set; }

        public AssemblyGenerator()
        {
            ReferenceAssemblyContainingType<object>();
            ReferenceAssembly(typeof (Enumerable).Assembly);
        }

        public void ReferenceAssembly(Assembly assembly)
        {
            try
            {
                bool alreadyReferenced = _references.Any(x => x.Display == assembly.Location);
                if (alreadyReferenced)
                    return;

                try
                {
                    _references.Add(CreateAssemblyReference(assembly));
                    foreach (var assemblyName in assembly.GetReferencedAssemblies())
                    {
                        var referencedAssembly = Assembly.Load(assemblyName);
                        ReferenceAssembly(referencedAssembly);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not make an assembly reference to {assembly.FullName}\n\n{e}");
                }
            }
            catch (AssemblyReferenceException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AssemblyReferenceException(assembly, e);
            }
        }

        private static PortableExecutableReference CreateAssemblyReference(Assembly assembly)
        {
            if (string.IsNullOrEmpty(assembly.Location))
            {
                var path = GetPath(assembly);
                return path != null ? MetadataReference.CreateFromFile(path) : null;
            }
            return MetadataReference.CreateFromFile(assembly.Location);
        }

        private static String GetPath(Assembly assembly)
        {
            return HintPaths?
                .Select(hintPath => Path.Combine(hintPath, assembly.GetName().Name + ".dll"))
                .FirstOrDefault(File.Exists);
        }

        public void ReferenceAssemblyContainingType<T>()
        {
            ReferenceAssembly(typeof (T).Assembly);
        }

        public Assembly Generate(string code)
        {
            var assemblyName = Path.GetRandomFileName();
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var references = _references.ToArray();
            var compilation = CSharpCompilation.Create(assemblyName, new[] {syntaxTree}, references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


            using (var stream = new MemoryStream())
            {
                var result = compilation.Emit(stream);

                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);


                    var message = failures.Select(x => $"{x.Id}: {x.GetMessage()}").Join("\n");
                    throw new InvalidOperationException("Compilation failures!\n\n" + message + "\n\nCode:\n\n" + code);
                }

                stream.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(stream.ToArray());
            }
        }
    }
}