using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

return SingleClassPerFile.Run(args);

internal static class SingleClassPerFile
{
    public static int Run(string[] args)
    {
        try
        {
            RunSelfTests();

            var repositoryRoot = FindRepositoryRoot();
            var files = GetFiles(repositoryRoot, args);

            if (files.Count == 0)
            {
                Console.WriteLine("No changed C# files to check.");
                return 0;
            }

            var violations = new List<FileViolation>();

            foreach (var relativePath in files)
            {
                var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));

                if (!File.Exists(fullPath))
                {
                    continue;
                }

                var declarations = FindTopLevelClasses(File.ReadAllText(fullPath), relativePath);

                if (declarations.Count > 1)
                {
                    violations.Add(new FileViolation(relativePath, declarations));
                }
            }

            if (violations.Count == 0)
            {
                Console.WriteLine(
                    $"Single-class-per-file check passed ({files.Count} C# file(s) inspected)."
                );
                return 0;
            }

            Console.Error.WriteLine(
                $"Found {violations.Sum(violation => violation.Declarations.Count) - violations.Count} "
                    + "extra top-level class declaration(s):"
            );

            foreach (var violation in violations)
            {
                Console.Error.WriteLine($"\n{violation.Path}:");

                foreach (var declaration in violation.Declarations)
                {
                    Console.Error.WriteLine($"  line {declaration.Line}: {declaration.Name}");
                }
            }

            Console.Error.WriteLine(
                "\nKeep at most one top-level class or class record in each .cs file. "
                    + "Nested classes are not counted."
            );

            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Single-class-per-file check failed: {exception.Message}");
            return 2;
        }
    }

    private static IReadOnlyList<string> GetFiles(string repositoryRoot, string[] args)
    {
        if (args.Length == 0 || args[0] == "--all")
        {
            return RunGit(repositoryRoot, "ls-files", "-z", "--", "*.cs")
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        if (args.Length != 2 || args[0] != "--changed-from")
        {
            throw new ArgumentException(
                "Usage: SingleClassPerFile [--all | --changed-from <base-sha>]"
            );
        }

        var baseSha = args[1];

        var changedFiles =
            string.IsNullOrWhiteSpace(baseSha) || baseSha.All(c => c == '0')
                ? RunGit(
                    repositoryRoot,
                    "diff-tree",
                    "--root",
                    "--no-commit-id",
                    "--name-only",
                    "-r",
                    "-z",
                    "HEAD",
                    "--",
                    "*.cs"
                )
                : RunGit(
                    repositoryRoot,
                    "diff",
                    "--name-only",
                    "-z",
                    "--diff-filter=ACMR",
                    baseSha,
                    "HEAD",
                    "--",
                    "*.cs"
                );

        return changedFiles
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<ClassDeclarationInfo> FindTopLevelClasses(
        string source,
        string path
    )
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, path);
        var declarations = new List<ClassDeclarationInfo>();

        VisitMembers(((CompilationUnitSyntax)syntaxTree.GetRoot()).Members, declarations);

        return declarations;
    }

    private static void VisitMembers(
        SyntaxList<MemberDeclarationSyntax> members,
        ICollection<ClassDeclarationInfo> declarations
    )
    {
        foreach (var member in members)
        {
            switch (member)
            {
                case ClassDeclarationSyntax classDeclaration:
                    declarations.Add(CreateInfo(classDeclaration.Identifier));
                    break;

                case RecordDeclarationSyntax recordDeclaration
                    when !recordDeclaration.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword):
                    declarations.Add(CreateInfo(recordDeclaration.Identifier));
                    break;

                case BaseNamespaceDeclarationSyntax namespaceDeclaration:
                    VisitMembers(namespaceDeclaration.Members, declarations);
                    break;
            }
        }
    }

    private static ClassDeclarationInfo CreateInfo(SyntaxToken identifier)
    {
        var line = identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        return new ClassDeclarationInfo(identifier.ValueText, line);
    }

    private static void RunSelfTests()
    {
        var cases = new[]
        {
            new AnalyzerTestCase(
                "nested classes are ignored",
                """
                namespace Example;
                public class Outer
                {
                    private sealed class Nested { }
                }
                """,
                1
            ),
            new AnalyzerTestCase(
                "classes in separate namespaces are counted",
                """
                namespace First { public class Alpha { } }
                namespace Second { public class Beta { } }
                """,
                2
            ),
            new AnalyzerTestCase(
                "comments and string literals are ignored by the parser",
                """
                namespace Example;
                public class Outer
                {
                    // class FromLineComment { }
                    /* class FromBlockComment { } */
                    private const string Text = "class FromString { }";
                }
                """,
                1
            ),
            new AnalyzerTestCase(
                "class records are counted, record structs are not",
                """
                public record Person(string Name);
                public record struct Point(int X, int Y);
                public class Service { }
                """,
                2
            ),
            new AnalyzerTestCase(
                "generic class constraints are not declarations",
                """
                public class Container<T> where T : class
                {
                    public class Nested { }
                }
                """,
                1
            ),
        };

        foreach (var testCase in cases)
        {
            var actual = FindTopLevelClasses(testCase.Source, testCase.Name).Count;

            if (actual != testCase.ExpectedCount)
            {
                throw new InvalidOperationException(
                    $"Analyzer self-test '{testCase.Name}' expected "
                        + $"{testCase.ExpectedCount} declaration(s), found {actual}."
                );
            }
        }

        Console.WriteLine($"Analyzer self-tests passed ({cases.Length} cases).");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(Environment.CurrentDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MARS.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the repository root (MARS.slnx). Run this tool from the repository."
        );
    }

    private static string[] RunGit(string repositoryRoot, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process =
            Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start git.");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"git {string.Join(' ', arguments)} failed: {error.Trim()}"
            );
        }

        return output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    private sealed record AnalyzerTestCase(string Name, string Source, int ExpectedCount);

    private sealed record ClassDeclarationInfo(string Name, int Line);

    private sealed record FileViolation(
        string Path,
        IReadOnlyList<ClassDeclarationInfo> Declarations
    );
}
