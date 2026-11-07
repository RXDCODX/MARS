using System.Diagnostics;
using System.Text;
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

            if (options.Fix)
            {
                var splitResult = SplitFiles(repositoryRoot, files);
                files = files.Concat(splitResult.GeneratedPaths)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                Console.WriteLine(
                    splitResult.NewFileCount == 0
                        ? "Auto-fix: no files required splitting."
                        : $"Auto-fix created {splitResult.NewFileCount} class file(s)."
                );
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
        var fix = false;
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
                                + "[--max-files <count>] [--fix] (0 = unlimited)."
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

                case "--fix":
                    if (fix)
                    {
                        throw new ArgumentException("--fix can only be specified once.");
                    }

                    fix = true;
                    break;

                default:
                    throw new ArgumentException(
                        $"Unknown argument '{args[index]}'. Usage: "
                            + "SingleClassPerFile [--all | --changed-from <base-sha>] "
                            + "[--max-files <count>] [--fix] (0 = unlimited)."
                    );
            }
        }

        return new AnalyzerArguments(baseSha, maxFilesToDisplay, fix);
    }

    private static IReadOnlyList<string> GetFiles(string repositoryRoot, AnalyzerArguments options)
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
    ) => maxFilesToDisplay is int limit ? violations.Take(limit).ToArray() : violations;

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
            throw new InvalidOperationException(
                "Analyzer self-test 'unlimited file output' failed."
            );
        }

        FileViolation[] sampleViolations = [new("first.cs", []), new("second.cs", [])];
        if (
            LimitViolations(sampleViolations, 1).Count != 1
            || LimitViolations(sampleViolations, null).Count != sampleViolations.Length
        )
        {
            throw new InvalidOperationException("Analyzer self-test 'file output limit' failed.");
        }

        var genericArityPlan = CreateSplitPlan(
            "Services/OperationResult.cs",
            """
            public class OperationResult { }
            public class OperationResult<TData> : OperationResult { }
            """
        );
        Assert(
            genericArityPlan.GeneratedFiles.Single().Path
                == "Services/OperationResult" + (char)96 + "1.cs",
            "generic types with the same name use their CLR arity in the filename"
        );

        var fileScopedPlan = CreateSplitPlan(
            "Services/Combined.cs",
            """
            using System;
            namespace Example;

            public sealed class First { }

            /// <summary>Moved record documentation.</summary>
            public record Second(string Name);

            [Obsolete]
            public class Third { }
            """
        );

        Assert(
            fileScopedPlan.GeneratedFiles.Select(file => file.Path).SequenceEqual(
                ["Services/Second.cs", "Services/Third.cs"]
            ),
            "file-scoped namespaces produce class-named files"
        );
        Assert(
            FindTopLevelClasses(fileScopedPlan.OriginalContent, "Combined.cs")
                .Select(declaration => declaration.Name)
                .SequenceEqual(["First"]),
            "the first declaration remains in the original file"
        );
        Assert(
            fileScopedPlan.GeneratedFiles.All(file =>
                FindTopLevelClasses(file.Content, file.Path).Count == 1
            ),
            "each generated file contains one top-level class"
        );
        Assert(
            fileScopedPlan.GeneratedFiles[0].Content.Contains(
                "using System;",
                StringComparison.Ordinal
            )
                && fileScopedPlan.GeneratedFiles[0].Content.Contains(
                    "namespace Example;",
                    StringComparison.Ordinal
                )
                && fileScopedPlan.GeneratedFiles[0].Content.Contains(
                    "Moved record documentation.",
                    StringComparison.Ordinal
                ),
            "file-scoped namespace, using directives, and XML documentation are preserved"
        );
        Assert(
            fileScopedPlan.GeneratedFiles[1].Content.Contains("[Obsolete]", StringComparison.Ordinal),
            "class attributes are preserved"
        );

        var blockNamespacePlan = CreateSplitPlan(
            "Services/BlockCombined.cs",
            """
            namespace First
            {
                public class Alpha { }
            }

            namespace Second
            {
                using System;
                public class Beta { }
            }
            """
        );
        Assert(
            blockNamespacePlan.GeneratedFiles.Single().Content.Contains(
                "namespace Second",
                StringComparison.Ordinal
            )
                && blockNamespacePlan.GeneratedFiles.Single().Content.Contains(
                    "using System;",
                    StringComparison.Ordinal
                ),
            "block namespace context and namespace usings are preserved"
        );

        var nestedPlan = CreateSplitPlan(
            "Services/NestedCombined.cs",
            """
            public class First
            {
                public class Nested { }
            }

            public class Second { }
            """
        );
        Assert(
            nestedPlan.GeneratedFiles.Count == 1
                && FindTopLevelClasses(nestedPlan.GeneratedFiles[0].Content, "Second.cs")
                    .Select(declaration => declaration.Name)
                    .SequenceEqual(["Second"]),
            "nested declarations are not extracted"
        );

        AssertThrows(
            () => ValidateGeneratedPaths(
                "/repo",
                [fileScopedPlan],
                path => path.EndsWith("Second.cs", StringComparison.Ordinal)
            ),
            "already exists",
            "existing destinations are not overwritten"
        );

        var duplicateNamePlan = CreateSplitPlan(
            "Services/Duplicates.cs",
            """
            public class First { }
            public class Second { }
            public class Second { }
            """
        );
        AssertThrows(
            () => ValidateGeneratedPaths("/repo", [duplicateNamePlan], _ => false),
            "more than one declaration",
            "duplicate class names fail safely"
        );

        var parsedFixArguments = ParseArguments(["--all", "--max-files", "3", "--fix"]);
        if (
            parsedFixArguments.BaseSha is not null
            || parsedFixArguments.MaxFilesToDisplay != 3
            || !parsedFixArguments.Fix
        )
        {
            throw new InvalidOperationException("Analyzer self-test 'fix and max-files options' failed.");
        }

        Assert(
            LimitViolations(
                [new FileViolation("first.cs", []), new FileViolation("second.cs", [])],
                1
            ).Count == 1,
            "file display limit remains functional"
        );

        Console.WriteLine($"Analyzer self-tests passed ({cases.Length + 14} cases).");
    }


    private static SplitResult SplitFiles(string repositoryRoot, IEnumerable<string> files)
    {
        var plans = new List<SourceSplitPlan>();

        foreach (var relativePath in files)
        {
            var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));

            if (!File.Exists(fullPath))
            {
                continue;
            }

            var plan = CreateSplitPlan(relativePath, File.ReadAllText(fullPath));

            if (plan.GeneratedFiles.Count > 0)
            {
                plans.Add(plan);
            }
        }

        ValidateGeneratedPaths(repositoryRoot, plans, File.Exists);

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        foreach (var plan in plans)
        {
            foreach (var generatedFile in plan.GeneratedFiles)
            {
                var generatedPath = Path.Combine(repositoryRoot, generatedFile.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(generatedPath)!);
                File.WriteAllText(generatedPath, generatedFile.Content, encoding);
            }

            File.WriteAllText(
                Path.Combine(repositoryRoot, plan.OriginalPath),
                plan.OriginalContent,
                encoding
            );
        }

        var generatedPaths = plans
            .SelectMany(plan => plan.GeneratedFiles)
            .Select(file => file.Path)
            .ToArray();

        return new SplitResult(
            plans.Sum(plan => plan.GeneratedFiles.Count),
            generatedPaths
        );
    }

    private static SourceSplitPlan CreateSplitPlan(string path, string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, path);
        var syntaxError = syntaxTree
            .GetDiagnostics()
            .FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        if (syntaxError is not null)
        {
            throw new InvalidOperationException(
                $"Cannot safely split '{path}' because it contains a C# syntax error: "
                    + syntaxError.GetMessage()
            );
        }

        var root = (CompilationUnitSyntax)syntaxTree.GetRoot();
        var declarations = FindTopLevelClassNodes(root);

        if (declarations.Count <= 1)
        {
            return new SourceSplitPlan(path, source, []);
        }

        var generatedFiles = new List<GeneratedFile>();

        foreach (var declaration in declarations.Skip(1))
        {
            var aritySuffix =
                declaration.Arity == 0 ? string.Empty : ((char)96) + declaration.Arity.ToString();
            var fileName = declaration.Name + aritySuffix + ".cs";
            var directory = Path.GetDirectoryName(path);
            var generatedPath = string.IsNullOrEmpty(directory)
                ? fileName
                : Path.Combine(directory, fileName);

            var generatedRoot = root
                .WithMembers(KeepOnlyTargetPath(root.Members, declaration.Declaration))
                .WithAttributeLists(default)
                .WithEndOfFileToken(SyntaxFactory.Token(SyntaxKind.EndOfFileToken));

            generatedFiles.Add(new GeneratedFile(generatedPath, generatedRoot.ToFullString()));
        }

        var declarationsToMove = declarations
            .Skip(1)
            .Select(declaration => declaration.Declaration)
            .ToArray();

        var originalRoot = root.RemoveNodes(
            declarationsToMove,
            SyntaxRemoveOptions.KeepNoTrivia
        ) as CompilationUnitSyntax;

        if (originalRoot is null)
        {
            throw new InvalidOperationException($"Could not rewrite the original file '{path}'.");
        }

        return new SourceSplitPlan(path, originalRoot.ToFullString(), generatedFiles);
    }

    private static IReadOnlyList<TopLevelClass> FindTopLevelClassNodes(CompilationUnitSyntax root)
    {
        var declarations = new List<TopLevelClass>();
        VisitTopLevelClassNodes(root.Members, declarations);
        return declarations;
    }

    private static void VisitTopLevelClassNodes(
        SyntaxList<MemberDeclarationSyntax> members,
        ICollection<TopLevelClass> declarations
    )
    {
        foreach (var member in members)
        {
            switch (member)
            {
                case ClassDeclarationSyntax classDeclaration:
                    declarations.Add(
                        new TopLevelClass(
                            classDeclaration.Identifier.ValueText,
                            classDeclaration.TypeParameterList?.Parameters.Count ?? 0,
                            classDeclaration
                        )
                    );
                    break;

                case RecordDeclarationSyntax recordDeclaration
                    when !recordDeclaration.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword):
                    declarations.Add(
                        new TopLevelClass(
                            recordDeclaration.Identifier.ValueText,
                            recordDeclaration.TypeParameterList?.Parameters.Count ?? 0,
                            recordDeclaration
                        )
                    );
                    break;

                case BaseNamespaceDeclarationSyntax namespaceDeclaration:
                    VisitTopLevelClassNodes(namespaceDeclaration.Members, declarations);
                    break;
            }
        }
    }

    private static SyntaxList<MemberDeclarationSyntax> KeepOnlyTargetPath(
        SyntaxList<MemberDeclarationSyntax> members,
        SyntaxNode target
    )
    {
        var retainedMembers = new List<MemberDeclarationSyntax>();

        foreach (var member in members)
        {
            if (member.RawKind == target.RawKind && member.Span == target.Span)
            {
                retainedMembers.Add(member);
                continue;
            }

            switch (member)
            {
                case NamespaceDeclarationSyntax namespaceDeclaration:
                {
                    var nestedMembers = KeepOnlyTargetPath(namespaceDeclaration.Members, target);

                    if (nestedMembers.Count > 0)
                    {
                        retainedMembers.Add(namespaceDeclaration.WithMembers(nestedMembers));
                    }

                    break;
                }

                case FileScopedNamespaceDeclarationSyntax fileScopedNamespace:
                {
                    var nestedMembers = KeepOnlyTargetPath(fileScopedNamespace.Members, target);

                    if (nestedMembers.Count > 0)
                    {
                        retainedMembers.Add(fileScopedNamespace.WithMembers(nestedMembers));
                    }

                    break;
                }
            }
        }

        return SyntaxFactory.List(retainedMembers);
    }

    private static void ValidateGeneratedPaths(
        string repositoryRoot,
        IEnumerable<SourceSplitPlan> plans,
        Func<string, bool> fileExists
    )
    {
        var reservedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var plan in plans)
        {
            var originalPath = Path.GetFullPath(Path.Combine(repositoryRoot, plan.OriginalPath));

            foreach (var generatedFile in plan.GeneratedFiles)
            {
                var generatedPath = Path.GetFullPath(
                    Path.Combine(repositoryRoot, generatedFile.Path)
                );

                if (string.Equals(originalPath, generatedPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Cannot split '{plan.OriginalPath}': destination '{generatedFile.Path}' "
                            + "is the original file. Rename the original file or move the declaration manually."
                    );
                }

                if (!reservedPaths.Add(generatedPath))
                {
                    throw new InvalidOperationException(
                        $"Cannot split classes because more than one declaration targets "
                            + $"'{generatedFile.Path}'. Resolve duplicate class names manually."
                    );
                }

                if (fileExists(generatedPath))
                {
                    throw new InvalidOperationException(
                        $"Cannot split '{plan.OriginalPath}': destination '{generatedFile.Path}' "
                            + "already exists. No files were changed; merge or rename it manually."
                    );
                }
            }
        }
    }


    private static void Assert(bool condition, string testName)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Analyzer self-test '{testName}' failed.");
        }
    }

    private static void AssertThrows(Action action, string expectedMessage, string testName)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException exception)
        {
            if (exception.Message.Contains(expectedMessage, StringComparison.Ordinal))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Analyzer self-test '{testName}' expected message '{expectedMessage}', "
                    + $"got '{exception.Message}'."
            );
        }

        throw new InvalidOperationException(
            $"Analyzer self-test '{testName}' expected an InvalidOperationException."
        );
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

    private sealed record AnalyzerArguments(string? BaseSha, int? MaxFilesToDisplay, bool Fix);


    private sealed record TopLevelClass(string Name, int Arity, SyntaxNode Declaration);

    private sealed record GeneratedFile(string Path, string Content);

    private sealed record SourceSplitPlan(
        string OriginalPath,
        string OriginalContent,
        IReadOnlyList<GeneratedFile> GeneratedFiles
    );

    private sealed record SplitResult(int NewFileCount, IReadOnlyList<string> GeneratedPaths);

    private sealed record AnalyzerTestCase(string Name, string Source, int ExpectedCount);

    private sealed record ClassDeclarationInfo(string Name, int Line);

    private sealed record FileViolation(
        string Path,
        IReadOnlyList<ClassDeclarationInfo> Declarations
    );
}
