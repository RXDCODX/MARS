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

            if (options.Fix)
            {
                var fixResult = ApplyFixes(repositoryRoot, files);

                if (!fixResult.Success)
                {
                    return 2;
                }

                files = files
                    .Concat(fixResult.GeneratedFiles)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

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
                        throw new ArgumentException(Usage());
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
                        $"Unknown argument '{args[index]}'. Usage: {Usage()}"
                    );
            }
        }

        return new AnalyzerArguments(baseSha, maxFilesToDisplay, fix);
    }

    private static string Usage() =>
        "Usage: SingleClassPerFile [--all | --changed-from <base-sha>] "
        + "[--max-files <count>] [--fix] (0 = unlimited).";

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

    private static FixResult ApplyFixes(string repositoryRoot, IReadOnlyList<string> files)
    {
        var plans = new List<FileSplitPlan>();

        foreach (var relativePath in files)
        {
            var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));

            if (!File.Exists(fullPath))
            {
                continue;
            }

            var plan = CreateSplitPlan(relativePath, File.ReadAllText(fullPath));

            if (plan is not null)
            {
                plans.Add(plan);
            }
        }

        if (plans.Count == 0)
        {
            Console.WriteLine("Auto-split: no files need to be split.");
            return FixResult.Succeeded([]);
        }

        var conflicts = FindOutputConflicts(repositoryRoot, plans);

        if (conflicts.Count > 0)
        {
            Console.Error.WriteLine(
                "Auto-split was cancelled. No files were changed because output paths conflict:"
            );

            foreach (var conflict in conflicts)
            {
                Console.Error.WriteLine($"  {conflict}");
            }

            Console.Error.WriteLine("Resolve the conflicting file(s), then run the fixer again.");
            return FixResult.Failed;
        }

        var filesToCheck = new List<string>();

        foreach (var plan in plans)
        {
            var sourcePath = Path.GetFullPath(Path.Combine(repositoryRoot, plan.SourcePath));
            var retainedPath = Path.GetFullPath(
                Path.Combine(
                    repositoryRoot,
                    plan.RetainedPath.Replace('/', Path.DirectorySeparatorChar)
                )
            );

            if (!PathsEqual(plan.SourcePath, plan.RetainedPath))
            {
                File.WriteAllText(retainedPath, plan.UpdatedSource, new UTF8Encoding(false));
                File.Delete(sourcePath);
                filesToCheck.Add(plan.RetainedPath);
                Console.WriteLine($"  {plan.SourcePath} -> {plan.RetainedPath}");
            }
            else
            {
                File.WriteAllText(sourcePath, plan.UpdatedSource, new UTF8Encoding(false));
            }

            foreach (var generatedFile in plan.GeneratedFiles)
            {
                var targetPath = Path.GetFullPath(
                    Path.Combine(
                        repositoryRoot,
                        generatedFile.Path.Replace('/', Path.DirectorySeparatorChar)
                    )
                );
                File.WriteAllText(targetPath, generatedFile.Content, new UTF8Encoding(false));
                filesToCheck.Add(generatedFile.Path);
                Console.WriteLine($"  {plan.SourcePath} -> {generatedFile.Path}");
            }
        }

        var movedCount = plans.Sum(plan => plan.GeneratedFiles.Count);
        Console.WriteLine(
            $"Auto-split completed: moved {movedCount} class declaration(s) "
                + $"from {plans.Count} source file(s)."
        );

        return FixResult.Succeeded(filesToCheck);
    }

    private static IReadOnlyList<string> FindOutputConflicts(
        string repositoryRoot,
        IReadOnlyList<FileSplitPlan> plans
    )
    {
        var conflicts = new List<string>();
        var plannedTargets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trackedFiles = RunGit(repositoryRoot, "ls-files", "-z", "--", "*.cs")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        void ClaimPath(string relativePath, string description)
        {
            var fullPath = Path.GetFullPath(
                Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))
            );

            if (plannedTargets.TryGetValue(fullPath, out var previousDescription))
            {
                conflicts.Add(
                    $"{relativePath} is requested by both {previousDescription} and {description}."
                );
            }
            else
            {
                plannedTargets.Add(fullPath, description);
            }
        }

        foreach (var plan in plans)
        {
            if (!PathsEqual(plan.SourcePath, plan.RetainedPath))
            {
                var retainedPath = Path.GetFullPath(
                    Path.Combine(
                        repositoryRoot,
                        plan.RetainedPath.Replace('/', Path.DirectorySeparatorChar)
                    )
                );

                if (
                    File.Exists(retainedPath)
                    || Directory.Exists(retainedPath)
                    || trackedFiles.Contains(plan.RetainedPath)
                )
                {
                    conflicts.Add(
                        $"{plan.RetainedPath} already exists; refusing to move the retained class there."
                    );
                }

                ClaimPath(plan.RetainedPath, $"the retained class from {plan.SourcePath}");
            }

            foreach (var generatedFile in plan.GeneratedFiles)
            {
                var targetPath = Path.GetFullPath(
                    Path.Combine(
                        repositoryRoot,
                        generatedFile.Path.Replace('/', Path.DirectorySeparatorChar)
                    )
                );
                var isOriginalPath = PathsEqual(generatedFile.Path, plan.SourcePath);

                if (
                    !isOriginalPath
                    && (
                        File.Exists(targetPath)
                        || Directory.Exists(targetPath)
                        || trackedFiles.Contains(generatedFile.Path)
                    )
                )
                {
                    conflicts.Add(
                        $"{generatedFile.Path} already exists; refusing to overwrite it."
                    );
                }

                if (isOriginalPath && PathsEqual(plan.SourcePath, plan.RetainedPath))
                {
                    conflicts.Add(
                        $"{generatedFile.Path} is the source path, but the retained class has no alternative file name."
                    );
                }

                ClaimPath(generatedFile.Path, $"a class moved from {plan.SourcePath}");
            }
        }

        return conflicts;
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            left.Replace('\\', '/'),
            right.Replace('\\', '/'),
            StringComparison.OrdinalIgnoreCase
        );

    private static FileSplitPlan? CreateSplitPlan(string relativePath, string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, relativePath);
        var syntaxError = syntaxTree
            .GetDiagnostics()
            .FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        if (syntaxError is not null)
        {
            throw new InvalidOperationException(
                $"Cannot safely split '{relativePath}' because it contains a C# syntax error: "
                    + syntaxError.GetMessage()
            );
        }

        var root = (CompilationUnitSyntax)syntaxTree.GetRoot();
        var declarations = FindTopLevelClassNodes(root);

        if (declarations.Count <= 1)
        {
            return null;
        }

        EnsureDirectivesAreInsideClassDeclarations(root, declarations, relativePath);

        var generatedFiles = new List<GeneratedClassFile>();

        for (var index = 1; index < declarations.Count; index++)
        {
            var declaration = declarations[index];
            var fileStem = GetUniqueClassFileStem(declaration, declarations, index);
            ValidateClassFileName(fileStem, relativePath);

            generatedFiles.Add(
                new GeneratedClassFile(
                    GetClassFilePath(relativePath, fileStem),
                    BuildClassFile(root, declaration)
                )
            );
        }

        var updatedRoot =
            root.RemoveNodes(declarations.Skip(1), SyntaxRemoveOptions.KeepNoTrivia)
                as CompilationUnitSyntax
            ?? throw new InvalidOperationException(
                $"Could not remove moved declarations from '{relativePath}'."
            );

        var retainedPath = relativePath;

        // If a moved declaration already matches the source file name, let
        // that class keep the existing path and move the retained first class
        // to its own correctly named file.
        if (generatedFiles.Any(generatedFile => PathsEqual(generatedFile.Path, relativePath)))
        {
            var retainedStem = GetUniqueClassFileStem(declarations[0], declarations, 0);
            retainedPath = GetClassFilePath(relativePath, retainedStem);

            if (PathsEqual(retainedPath, relativePath))
            {
                throw new InvalidOperationException(
                    $"Cannot split '{relativePath}': the retained class and a moved class "
                        + "both require the same output path."
                );
            }
        }

        return new FileSplitPlan(
            relativePath,
            retainedPath,
            updatedRoot.ToFullString(),
            generatedFiles
        );
    }

    private static string GetUniqueClassFileStem(
        MemberDeclarationSyntax declaration,
        IReadOnlyList<MemberDeclarationSyntax> declarations,
        int declarationIndex
    )
    {
        var className = GetClassName(declaration);
        var sameName = declarations
            .Where(candidate =>
                string.Equals(GetClassName(candidate), className, StringComparison.Ordinal)
            )
            .ToArray();

        if (sameName.Length <= 1)
        {
            return className;
        }

        var typeParameters = GetTypeParameterNames(declaration);
        var sameSignatureCount = sameName.Count(candidate =>
            GetTypeParameterNames(candidate).SequenceEqual(typeParameters, StringComparer.Ordinal)
        );

        if (typeParameters.Count > 0 && sameSignatureCount == 1)
        {
            return $"{className}Of{string.Join("And", typeParameters)}";
        }

        return $"{className}.Part{declarationIndex + 1}";
    }

    private static IReadOnlyList<string> GetTypeParameterNames(
        MemberDeclarationSyntax declaration
    ) =>
        declaration switch
        {
            ClassDeclarationSyntax classDeclaration => classDeclaration
                .TypeParameterList?.Parameters.Select(parameter => parameter.Identifier.ValueText)
                .ToArray()
                ?? [],
            RecordDeclarationSyntax recordDeclaration => recordDeclaration
                .TypeParameterList?.Parameters.Select(parameter => parameter.Identifier.ValueText)
                .ToArray()
                ?? [],
            _ => [],
        };

    private static void EnsureDirectivesAreInsideClassDeclarations(
        CompilationUnitSyntax root,
        IReadOnlyList<MemberDeclarationSyntax> declarations,
        string path
    )
    {
        foreach (
            var directive in root.DescendantTrivia(descendIntoTrivia: true)
                .Where(trivia => trivia.IsDirective)
        )
        {
            if (declarations.Any(declaration => declaration.Span.Contains(directive.Span)))
            {
                continue;
            }

            var line = directive.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            throw new InvalidOperationException(
                $"Cannot safely split '{path}': preprocessor directive at line {line} "
                    + "is outside a class declaration. Move the directive into the class "
                    + "body or split this file manually."
            );
        }
    }

    private static void ValidateClassFileName(string className, string sourcePath)
    {
        const string windowsInvalidCharacters = "<>:\"/\\\\|?*";
        var invalidCharacters = Path.GetInvalidFileNameChars()
            .Concat(windowsInvalidCharacters)
            .ToHashSet();

        if (
            string.IsNullOrWhiteSpace(className)
            || className.IndexOfAny(invalidCharacters.ToArray()) >= 0
            || IsReservedWindowsFileName(className)
        )
        {
            throw new InvalidOperationException(
                $"Cannot create a file for class '{className}' in '{sourcePath}': "
                    + "the class name is not a safe file name."
            );
        }
    }

    private static bool IsReservedWindowsFileName(string name)
    {
        var normalizedName = name.TrimEnd('.').Split('.')[0];

        if (
            normalizedName.Equals("CON", StringComparison.OrdinalIgnoreCase)
            || normalizedName.Equals("PRN", StringComparison.OrdinalIgnoreCase)
            || normalizedName.Equals("AUX", StringComparison.OrdinalIgnoreCase)
            || normalizedName.Equals("NUL", StringComparison.OrdinalIgnoreCase)
            || normalizedName.Equals("CONIN$", StringComparison.OrdinalIgnoreCase)
            || normalizedName.Equals("CONOUT$", StringComparison.OrdinalIgnoreCase)
        )
        {
            return true;
        }

        return normalizedName.Length == 4
            && (
                normalizedName.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                || normalizedName.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)
            )
            && normalizedName[3] is >= '1' and <= '9';
    }

    private static string GetClassFilePath(string sourcePath, string fileStem)
    {
        var directory = Path.GetDirectoryName(sourcePath.Replace('/', Path.DirectorySeparatorChar));

        return string.IsNullOrEmpty(directory)
            ? $"{fileStem}.cs"
            : $"{directory.Replace('\\', '/')}/{fileStem}.cs";
    }

    private static string BuildClassFile(
        CompilationUnitSyntax sourceRoot,
        MemberDeclarationSyntax declaration
    )
    {
        MemberDeclarationSyntax member = declaration;

        // Ancestors() returns the nearest namespace first, so wrapping in this
        // order reconstructs nested namespace declarations from the inside out.
        foreach (
            var namespaceDeclaration in declaration
                .Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
        )
        {
            member = namespaceDeclaration.WithMembers(SyntaxFactory.SingletonList(member));
        }

        // Global usings remain in the original file and are compilation-wide.
        // Copy ordinary usings and extern aliases because they are file-scoped.
        var usings = sourceRoot.Usings.Where(usingDirective =>
            !usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)
        );

        var splitRoot = SyntaxFactory
            .CompilationUnit()
            .WithExterns(SyntaxFactory.List(sourceRoot.Externs))
            .WithUsings(SyntaxFactory.List(usings))
            .WithMembers(SyntaxFactory.SingletonList(member));

        return splitRoot.ToFullString();
    }

    private static IReadOnlyList<ClassDeclarationInfo> FindTopLevelClasses(
        string source,
        string path
    )
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, path);
        var root = (CompilationUnitSyntax)syntaxTree.GetRoot();

        return FindTopLevelClassNodes(root)
            .Select(declaration => CreateInfo(GetIdentifier(declaration)))
            .ToArray();
    }

    private static IReadOnlyList<MemberDeclarationSyntax> FindTopLevelClassNodes(
        CompilationUnitSyntax root
    )
    {
        var declarations = new List<MemberDeclarationSyntax>();
        VisitMembers(root.Members, declarations);
        return declarations;
    }

    private static void VisitMembers(
        SyntaxList<MemberDeclarationSyntax> members,
        ICollection<MemberDeclarationSyntax> declarations
    )
    {
        foreach (var member in members)
        {
            switch (member)
            {
                case ClassDeclarationSyntax classDeclaration:
                    declarations.Add(classDeclaration);
                    break;

                case RecordDeclarationSyntax recordDeclaration
                    when !recordDeclaration.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword):
                    declarations.Add(recordDeclaration);
                    break;

                case BaseNamespaceDeclarationSyntax namespaceDeclaration:
                    VisitMembers(namespaceDeclaration.Members, declarations);
                    break;
            }
        }
    }

    private static string GetClassName(MemberDeclarationSyntax declaration) =>
        GetIdentifier(declaration).ValueText;

    private static SyntaxToken GetIdentifier(MemberDeclarationSyntax declaration) =>
        declaration switch
        {
            ClassDeclarationSyntax classDeclaration => classDeclaration.Identifier,
            RecordDeclarationSyntax recordDeclaration => recordDeclaration.Identifier,
            _ => throw new ArgumentException(
                $"Unsupported class declaration node: {declaration.Kind()}",
                nameof(declaration)
            ),
        };

    private static ClassDeclarationInfo CreateInfo(SyntaxToken identifier)
    {
        var line = identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        return new ClassDeclarationInfo(identifier.ValueText, line);
    }

    private static IReadOnlyList<FileViolation> LimitViolations(
        IReadOnlyList<FileViolation> violations,
        int? maxFilesToDisplay
    ) => maxFilesToDisplay is int limit ? violations.Take(limit).ToArray() : violations;

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

        var parsedArguments = ParseArguments(["--all", "--max-files", "3", "--fix"]);
        if (
            parsedArguments.BaseSha is not null
            || parsedArguments.MaxFilesToDisplay != 3
            || !parsedArguments.Fix
        )
        {
            throw new InvalidOperationException(
                "Analyzer self-test 'fix and max-files options' failed."
            );
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

        var splitSelfTests = RunSplitSelfTests();
        Console.WriteLine(
            $"Analyzer self-tests passed ({cases.Length + 3 + splitSelfTests} cases)."
        );
    }

    private static int RunSplitSelfTests()
    {
        const string blockNamespaceSource = """
            using System;
            namespace Sample
            {
                using System.Collections.Generic;

                public class First { }

                /// <summary>Second class documentation.</summary>
                public class Second { }
            }
            """;

        var blockPlan =
            CreateSplitPlan("Models.cs", blockNamespaceSource)
            ?? throw new InvalidOperationException(
                "Block namespace split self-test did not create a plan."
            );

        AssertSplitPlan(
            blockPlan,
            "block namespace and namespace usings",
            "First",
            "Second",
            "using System;",
            "using System.Collections.Generic;",
            "namespace Sample",
            "/// <summary>Second class documentation.</summary>"
        );

        const string fileScopedSource = """
            using System;
            namespace FileScoped.Models;

            public record First(string Value);

            public record Second(string Value);
            """;

        var fileScopedPlan =
            CreateSplitPlan("Records.cs", fileScopedSource)
            ?? throw new InvalidOperationException(
                "File-scoped namespace split self-test did not create a plan."
            );

        AssertSplitPlan(
            fileScopedPlan,
            "file-scoped namespace and record classes",
            "First",
            "Second",
            "using System;",
            "namespace FileScoped.Models;"
        );

        const string globalUsingSource = """
            global using System.Text;
            using System;

            public class First { }
            public class Second { }
            """;

        var globalUsingPlan =
            CreateSplitPlan("Types.cs", globalUsingSource)
            ?? throw new InvalidOperationException(
                "Global using split self-test did not create a plan."
            );

        if (
            globalUsingPlan
                .GeneratedFiles[0]
                .Content.Contains("global using System.Text;", StringComparison.Ordinal)
            || !globalUsingPlan.UpdatedSource.Contains(
                "global using System.Text;",
                StringComparison.Ordinal
            )
        )
        {
            throw new InvalidOperationException(
                "Split self-test 'global usings are not duplicated' failed."
            );
        }

        const string sourceNameCollision = """
            namespace Controllers;
            public record CreateRequest(string Name);
            public class ControllersController { }
            """;

        var sourceNamePlan =
            CreateSplitPlan("Controllers/ControllersController.cs", sourceNameCollision)
            ?? throw new InvalidOperationException(
                "Source-name collision split self-test did not create a plan."
            );

        if (
            sourceNamePlan.RetainedPath != "Controllers/CreateRequest.cs"
            || sourceNamePlan.GeneratedFiles.Single().Path != "Controllers/ControllersController.cs"
        )
        {
            throw new InvalidOperationException(
                "Split self-test 'source file path is reused by its matching class' failed."
            );
        }

        const string sameTypeNameSource = """
            public class OperationResult { }
            public class OperationResult<TData> { }
            """;

        var sameTypeNamePlan =
            CreateSplitPlan("OperationResult.cs", sameTypeNameSource)
            ?? throw new InvalidOperationException(
                "Same type-name split self-test did not create a plan."
            );

        if (sameTypeNamePlan.GeneratedFiles.Single().Path != "OperationResultOfTData.cs")
        {
            throw new InvalidOperationException(
                "Split self-test 'generic and non-generic class file names are disambiguated' failed."
            );
        }

        const string nestedOnlySource = """
            public class Outer
            {
                public class Nested { }
            }
            """;

        if (CreateSplitPlan("Outer.cs", nestedOnlySource) is not null)
        {
            throw new InvalidOperationException(
                "Split self-test 'nested classes are ignored' failed."
            );
        }

        const string directiveSource = """
            #nullable enable
            public class First { }
            public class Second { }
            """;

        try
        {
            _ = CreateSplitPlan("Nullable.cs", directiveSource);
            throw new InvalidOperationException(
                "Split self-test 'unsafe file-level directives are rejected' failed."
            );
        }
        catch (InvalidOperationException exception)
            when (exception.Message.Contains("preprocessor directive", StringComparison.Ordinal))
        {
            // Expected: file-level directives must not silently lose their scope.
        }

        return 7;
    }

    private static void AssertSplitPlan(
        FileSplitPlan plan,
        string testName,
        string retainedClass,
        string movedClass,
        params string[] expectedGeneratedText
    )
    {
        var retainedDeclarations = FindTopLevelClasses(plan.UpdatedSource, plan.SourcePath);
        var generatedFile = plan.GeneratedFiles.Single();

        if (
            retainedDeclarations.Count != 1
            || retainedDeclarations[0].Name != retainedClass
            || generatedFile.Path != $"{movedClass}.cs"
            || FindTopLevelClasses(generatedFile.Content, generatedFile.Path).Count != 1
            || FindTopLevelClasses(generatedFile.Content, generatedFile.Path)[0].Name != movedClass
            || expectedGeneratedText.Any(expected =>
                !generatedFile.Content.Contains(expected, StringComparison.Ordinal)
            )
        )
        {
            throw new InvalidOperationException($"Split self-test '{testName}' failed.");
        }
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

    private sealed record AnalyzerTestCase(string Name, string Source, int ExpectedCount);

    private sealed record ClassDeclarationInfo(string Name, int Line);

    private sealed record FileViolation(
        string Path,
        IReadOnlyList<ClassDeclarationInfo> Declarations
    );

    private sealed record GeneratedClassFile(string Path, string Content);

    private sealed record FileSplitPlan(
        string SourcePath,
        string RetainedPath,
        string UpdatedSource,
        IReadOnlyList<GeneratedClassFile> GeneratedFiles
    );

    private sealed record FixResult(bool Success, IReadOnlyList<string> GeneratedFiles)
    {
        public static FixResult Failed { get; } = new(false, []);

        public static FixResult Succeeded(IReadOnlyList<string> generatedFiles) =>
            new(true, generatedFiles);
    }
}
