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
            var options = ParseArguments(args);
            var files = GetFiles(repositoryRoot, options);

            if (files.Count == 0)
            {
                Console.WriteLine("No C# files to check.");
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

            var displayedViolations = LimitViolations(violations, options.MaxFilesToDisplay);

            Console.Error.WriteLine(
                $"Found {violations.Count} violating .cs file(s) with "
                    + $"{violations.Sum(violation => violation.Declarations.Count) - violations.Count} "
                    + "extra top-level class declaration(s)."
            );
            Console.Error.WriteLine(
                $"Displaying {displayedViolations.Count} of {violations.Count} violating file(s)."
            );

            foreach (var violation in displayedViolations)
            {
                Console.Error.WriteLine($"\n{violation.Path}:");

                foreach (var declaration in violation.Declarations)
                {
                    Console.Error.WriteLine($"  line {declaration.Line}: {declaration.Name}");
                }
            }

            if (displayedViolations.Count < violations.Count)
            {
                Console.Error.WriteLine(
                    $"\n{violations.Count - displayedViolations.Count} additional file(s) "
                        + "were omitted. Increase --max-files or use --max-files 0 to show all."
                );
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

    private static AnalyzerArguments ParseArguments(string[] args)
    {
        string? baseSha = null;
        var selectionSpecified = false;
        var maxFilesOptionSpecified = false;
        int? maxFilesToDisplay = null;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--all":
                    if (selectionSpecified)
                    {
                        throw new ArgumentException("Specify only one file selection option.");
                    }

                    selectionSpecified = true;
                    break;

                case "--changed-from":
                    if (selectionSpecified || index + 1 >= args.Length)
                    {
                        throw new ArgumentException(
                            "Usage: SingleClassPerFile [--all | --changed-from <base-sha>] "
                                + "[--max-files <count>] (0 = unlimited)."
                        );
                    }

                    selectionSpecified = true;
                    baseSha = args[++index];
                    break;

                case "--max-files":
                    if (maxFilesOptionSpecified || index + 1 >= args.Length)
                    {
                        throw new ArgumentException(
                            "--max-files must be specified once with a non-negative integer."
                        );
                    }

                    if (!int.TryParse(args[++index], out var maxFiles) || maxFiles < 0)
                    {
                        throw new ArgumentException("--max-files must be a non-negative integer.");
                    }

                    maxFilesOptionSpecified = true;
                    maxFilesToDisplay = maxFiles == 0 ? null : maxFiles;
                    break;

                default:
                    throw new ArgumentException(
                        $"Unknown argument '{args[index]}'. Usage: "
                            + "SingleClassPerFile [--all | --changed-from <base-sha>] "
                            + "[--max-files <count>] (0 = unlimited)."
                    );
            }
        }

        return new AnalyzerArguments(baseSha, maxFilesToDisplay);
    }

    private static IReadOnlyList<string> GetFiles(
        string repositoryRoot,
        AnalyzerArguments options
    )
    {
        if (options.BaseSha is null)
        {
            return RunGit(repositoryRoot, "ls-files", "-z", "--", "*.cs")
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        var baseSha = options.BaseSha;

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

    private static IReadOnlyList<FileViolation> LimitViolations(
        IReadOnlyList<FileViolation> violations,
        int? maxFilesToDisplay
    ) =>
        maxFilesToDisplay is int limit ? violations.Take(limit).ToArray() : violations;

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

        var parsedArguments = ParseArguments(["--all", "--max-files", "3"]);
        if (parsedArguments.BaseSha is not null || parsedArguments.MaxFilesToDisplay != 3)
        {
            throw new InvalidOperationException("Analyzer self-test 'max-files option' failed.");
        }

        var unlimitedArguments = ParseArguments(["--max-files", "0"]);
        if (unlimitedArguments.MaxFilesToDisplay is not null)
        {
            throw new InvalidOperationException("Analyzer self-test 'unlimited file output' failed.");
        }

        FileViolation[] sampleViolations =
        [
            new("first.cs", []),
            new("second.cs", []),
        ];
        if (
            LimitViolations(sampleViolations, 1).Count != 1
            || LimitViolations(sampleViolations, null).Count != sampleViolations.Length
        )
        {
            throw new InvalidOperationException("Analyzer self-test 'file output limit' failed.");
        }

        Console.WriteLine($"Analyzer self-tests passed ({cases.Length + 3} cases).");
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

    private sealed record AnalyzerArguments(string? BaseSha, int? MaxFilesToDisplay);

    private sealed record AnalyzerTestCase(string Name, string Source, int ExpectedCount);

    private sealed record ClassDeclarationInfo(string Name, int Line);

    private sealed record FileViolation(
        string Path,
        IReadOnlyList<ClassDeclarationInfo> Declarations
    );
}
