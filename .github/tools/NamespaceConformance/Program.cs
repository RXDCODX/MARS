using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

return NamespaceConformance.Run(args);

internal static class NamespaceConformance
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

            var projectMap = BuildProjectMap(repositoryRoot);
            var violations = new List<string>();

            // Partial grouping: (project, typeName) -> list of (filePath, namespace, expected)
            var partialGroups = new Dictionary<
                (string project, string typeName),
                List<PartialPart>
            >(new ValueTupleComparer());

            var fileAnalyses = new Dictionary<string, FileAnalysis>(
                StringComparer.OrdinalIgnoreCase
            );

            foreach (var relativePath in files)
            {
                var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                var content = File.ReadAllText(fullPath);
                var analysis = AnalyzeFile(repositoryRoot, relativePath, content, projectMap);
                fileAnalyses[relativePath] = analysis;

                if (analysis.IsOrphan)
                {
                    violations.Add($"ORPHAN {relativePath}: no enclosing .csproj");
                    continue;
                }

                if (analysis.IsEmpty)
                {
                    violations.Add($"EMPTY {relativePath}: file has no content");
                    continue;
                }

                foreach (var part in analysis.PartialParts)
                {
                    var key = (analysis.ProjectName, part.TypeName);
                    if (!partialGroups.TryGetValue(key, out var list))
                    {
                        list = new List<PartialPart>();
                        partialGroups[key] = list;
                    }
                    list.Add(
                        new PartialPart(relativePath, part.Namespace, analysis.ExpectedNamespace)
                    );
                }
            }

            // Validate partial groups: all parts share one namespace == expected of at least one part.
            var partialExempt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in partialGroups)
            {
                if (group.Value.Count < 2)
                {
                    continue;
                }

                var namespaces = group
                    .Value.Select(p => p.Namespace)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                if (namespaces.Count != 1)
                {
                    foreach (var part in group.Value)
                    {
                        violations.Add(
                            $"PARTIAL-MISMATCH {part.Path}: partial '{group.Key.typeName}' parts disagree on namespace "
                                + $"({string.Join(", ", namespaces)})"
                        );
                    }
                    continue;
                }

                var sharedNs = namespaces[0];
                var anyExpectedMatches = group.Value.Any(p =>
                    string.Equals(p.Expected, sharedNs, StringComparison.Ordinal)
                );
                if (!anyExpectedMatches)
                {
                    foreach (var part in group.Value)
                    {
                        violations.Add(
                            $"WRONG-NS {part.Path}: declared '{sharedNs}', expected one of "
                                + string.Join(", ", group.Value.Select(p => p.Expected).Distinct())
                        );
                    }
                    continue;
                }

                foreach (var part in group.Value)
                {
                    partialExempt.Add(part.Path);
                }
            }

            foreach (var relativePath in files)
            {
                if (!fileAnalyses.TryGetValue(relativePath, out var analysis))
                {
                    continue;
                }

                if (analysis.IsOrphan || analysis.IsEmpty)
                {
                    continue;
                }

                if (partialExempt.Contains(relativePath))
                {
                    continue;
                }

                if (analysis.TypeCount == 0)
                {
                    // usings-only file: exempt
                    continue;
                }

                if (analysis.DeclaredNamespaces.Count == 0)
                {
                    violations.Add(
                        $"NO-NS {relativePath}: expected '{analysis.ExpectedNamespace}'"
                    );
                    continue;
                }

                if (analysis.DeclaredNamespaces.Count > 1)
                {
                    violations.Add(
                        $"MULTI-NS {relativePath}: {string.Join(", ", analysis.DeclaredNamespaces)}"
                    );
                    continue;
                }

                if (
                    !string.Equals(
                        analysis.DeclaredNamespaces[0],
                        analysis.ExpectedNamespace,
                        StringComparison.Ordinal
                    )
                )
                {
                    violations.Add(
                        $"WRONG-NS {relativePath}: declared '{analysis.DeclaredNamespaces[0]}', "
                            + $"expected '{analysis.ExpectedNamespace}'"
                    );
                }
            }

            if (violations.Count == 0)
            {
                Console.WriteLine(
                    $"Namespace conformance check passed ({files.Count} C# file(s) inspected)."
                );
                return 0;
            }

            var displayed = violations;
            if (
                options.MaxFilesToDisplay.HasValue
                && options.MaxFilesToDisplay.Value < displayed.Count
            )
            {
                displayed = displayed.Take(options.MaxFilesToDisplay.Value).ToList();
            }

            Console.Error.WriteLine(
                $"Found {violations.Count} namespace conformance violation(s)."
            );
            Console.Error.WriteLine($"Displaying {displayed.Count} of {violations.Count}.");
            foreach (var v in displayed)
            {
                Console.Error.WriteLine($"  {v}");
            }

            if (displayed.Count < violations.Count)
            {
                Console.Error.WriteLine(
                    $"\n{violations.Count - displayed.Count} additional violation(s) omitted. "
                        + "Increase --max-files or use --max-files 0 to show all."
                );
            }

            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Namespace conformance check failed: {exception.Message}");
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
        "Usage: NamespaceConformance [--all | --changed-from <base-sha>] "
        + "[--max-files <count>] [--fix] (0 = unlimited).";

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "MARS.slnx")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        // Fallback: walk up from cwd
        current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "MARS.slnx")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new InvalidOperationException("Could not find repository root (MARS.slnx).");
    }

    private static IReadOnlyList<string> GetFiles(string repositoryRoot, AnalyzerArguments options)
    {
        if (options.BaseSha is null)
        {
            return RunGit(repositoryRoot, "ls-files", "-z", "--", "*.cs")
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .Where(path =>
                    !path.StartsWith(".github/tools/", StringComparison.OrdinalIgnoreCase)
                )
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
            .Where(path => !path.StartsWith(".github/tools/", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Dictionary<string, string> BuildProjectMap(string repositoryRoot)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (
            var csproj in Directory.GetFiles(
                repositoryRoot,
                "*.csproj",
                SearchOption.AllDirectories
            )
        )
        {
            if (
                csproj.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || csproj.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || csproj.Contains(
                    $"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}"
                )
            )
            {
                continue;
            }

            var projectDir = Path.GetDirectoryName(csproj)!;
            var relDir = Path.GetRelativePath(repositoryRoot, projectDir).Replace('\\', '/');
            var projectName = Path.GetFileNameWithoutExtension(csproj);

            string rootNamespace = projectName;
            try
            {
                var xml = System.Xml.Linq.XDocument.Load(csproj);
                var rn = xml.Descendants("RootNamespace").FirstOrDefault()?.Value;
                if (!string.IsNullOrWhiteSpace(rn))
                {
                    rootNamespace = rn.Trim();
                }
            }
            catch
            {
                // keep default
            }

            map[relDir] = $"{projectName}\u001f{rootNamespace}";
        }
        return map;
    }

    private static (string projectName, string rootNamespace)? FindEnclosingProject(
        string relativeDir,
        Dictionary<string, string> projectMap
    )
    {
        var dir = relativeDir.Replace('\\', '/');
        while (!string.IsNullOrEmpty(dir))
        {
            if (projectMap.TryGetValue(dir, out var val))
            {
                var parts = val.Split('\u001f');
                return (parts[0], parts[1]);
            }
            var idx = dir.LastIndexOf('/');
            if (idx < 0)
            {
                break;
            }
            dir = dir.Substring(0, idx);
        }
        return null;
    }

    private sealed class FileAnalysis
    {
        public required string ProjectName { get; init; }
        public required string RootNamespace { get; init; }
        public required string ExpectedNamespace { get; init; }
        public required List<string> DeclaredNamespaces { get; init; }
        public required int TypeCount { get; init; }
        public required bool IsOrphan { get; init; }
        public required bool IsEmpty { get; init; }
        public required List<PartialTypeInfo> PartialParts { get; init; }
    }

    private sealed class PartialTypeInfo
    {
        public required string TypeName { get; init; }
        public required string Namespace { get; init; }
    }

    private sealed class PartialPart
    {
        public string Path { get; }
        public string Namespace { get; }
        public string Expected { get; }

        public PartialPart(string path, string ns, string expected)
        {
            Path = path;
            Namespace = ns;
            Expected = expected;
        }
    }

    private static FileAnalysis AnalyzeFile(
        string repositoryRoot,
        string relativePath,
        string content,
        Dictionary<string, string> projectMap
    )
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            var proj = TryProject(relativePath, projectMap);
            return new FileAnalysis
            {
                ProjectName = proj?.projectName ?? string.Empty,
                RootNamespace = proj?.rootNamespace ?? string.Empty,
                ExpectedNamespace = proj is null ? string.Empty : proj.Value.rootNamespace,
                DeclaredNamespaces = new List<string>(),
                TypeCount = 0,
                IsOrphan = proj is null,
                IsEmpty = true,
                PartialParts = new List<PartialTypeInfo>(),
            };
        }

        var relDir = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? string.Empty;
        var enclosing = FindEnclosingProject(relDir, projectMap);

        if (enclosing is null)
        {
            return new FileAnalysis
            {
                ProjectName = string.Empty,
                RootNamespace = string.Empty,
                ExpectedNamespace = string.Empty,
                DeclaredNamespaces = new List<string>(),
                TypeCount = 0,
                IsOrphan = true,
                IsEmpty = false,
                PartialParts = new List<PartialTypeInfo>(),
            };
        }

        var (projectName, rootNamespace) = enclosing.Value;
        var subDir =
            string.IsNullOrEmpty(relDir) || relDir == projectName
                ? string.Empty
                : (
                    relDir.Length
                    > (
                        Path.GetDirectoryName(relativePath)?.Replace('\\', '/') is { } d
                            ? (FindProjectDir(relDir, projectMap) ?? string.Empty).Length
                            : 0
                    )
                        ? relDir
                            .Substring((FindProjectDir(relDir, projectMap) ?? string.Empty).Length)
                            .TrimStart('/')
                        : string.Empty
                );

        // Simpler: compute subDir by stripping project dir prefix
        var projectDir = FindProjectDir(relDir, projectMap) ?? string.Empty;
        subDir = string.IsNullOrEmpty(projectDir)
            ? relDir
            : (
                relDir.Length > projectDir.Length
                    ? relDir.Substring(projectDir.Length).TrimStart('/')
                    : string.Empty
            );

        var expected = rootNamespace;
        if (!string.IsNullOrEmpty(subDir))
        {
            expected =
                rootNamespace
                + "."
                + string.Join(
                    ".",
                    subDir.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(SanitizeSegment)
                );
        }

        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(content, parseOptions, relativePath);
        var root = (CompilationUnitSyntax)syntaxTree.GetRoot();

        var namespaceDecls = root.Members.OfType<BaseNamespaceDeclarationSyntax>().ToList();
        var declaredNamespaces = namespaceDecls.Select(n => n.Name.ToString()).ToList();

        // Top-level types: direct children of CompilationUnit or of a top-level namespace.
        var types = new List<BaseTypeDeclarationSyntax>();
        types.AddRange(root.Members.OfType<BaseTypeDeclarationSyntax>());
        foreach (var ns in namespaceDecls)
        {
            types.AddRange(ns.Members.OfType<BaseTypeDeclarationSyntax>());
        }

        var partialParts = new List<PartialTypeInfo>();
        foreach (var t in types)
        {
            var isPartial = t.Modifiers.Any(m => m.Text == "partial");
            if (isPartial)
            {
                // determine namespace this type lives in
                string nsName;
                if (t.Parent is BaseNamespaceDeclarationSyntax parentNs)
                {
                    nsName = parentNs.Name.ToString();
                }
                else
                {
                    nsName = declaredNamespaces.Count == 1 ? declaredNamespaces[0] : string.Empty;
                }
                partialParts.Add(
                    new PartialTypeInfo { TypeName = t.Identifier.Text, Namespace = nsName }
                );
            }
        }

        // If a file has partial types, mark them; group validation happens later.
        // For non-partial files, partialParts stays empty.

        return new FileAnalysis
        {
            ProjectName = projectName,
            RootNamespace = rootNamespace,
            ExpectedNamespace = expected,
            DeclaredNamespaces = declaredNamespaces,
            TypeCount = types.Count,
            IsOrphan = false,
            IsEmpty = false,
            PartialParts = partialParts,
        };
    }

    private static (string projectName, string rootNamespace)? TryProject(
        string relativePath,
        Dictionary<string, string> projectMap
    )
    {
        var relDir = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? string.Empty;
        var enclosing = FindEnclosingProject(relDir, projectMap);
        if (enclosing is null)
        {
            return null;
        }
        return enclosing;
    }

    private static string? FindProjectDir(string relDir, Dictionary<string, string> projectMap)
    {
        var dir = relDir;
        while (!string.IsNullOrEmpty(dir))
        {
            if (projectMap.ContainsKey(dir))
            {
                return dir;
            }
            var idx = dir.LastIndexOf('/');
            if (idx < 0)
            {
                break;
            }
            dir = dir.Substring(0, idx);
        }
        return null;
    }

    private static string SanitizeSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment))
        {
            return segment;
        }
        var result = new StringBuilder();
        if (char.IsDigit(segment[0]))
        {
            result.Append('_');
        }
        foreach (var c in segment)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                result.Append(c);
            }
            else
            {
                result.Append('_');
            }
        }
        return result.ToString();
    }

    private sealed class FixResult
    {
        public bool Success { get; init; }
        public IReadOnlyList<string> GeneratedFiles { get; init; } = Array.Empty<string>();

        public static FixResult Succeeded(IReadOnlyList<string> generatedFiles) =>
            new() { Success = true, GeneratedFiles = generatedFiles };

        public static FixResult Failed => new() { Success = false };
    }

    private static (HashSet<string> exempt, List<string> violations) ComputePartialInfo(
        string repositoryRoot,
        IReadOnlyList<string> files,
        Dictionary<string, string> projectMap
    )
    {
        var exempt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var violations = new List<string>();

        var partialGroups = new Dictionary<(string project, string typeName), List<PartialPart>>(
            new ValueTupleComparer()
        );

        var fileAnalyses = new Dictionary<string, FileAnalysis>(StringComparer.OrdinalIgnoreCase);
        foreach (var relativePath in files)
        {
            var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));
            if (!File.Exists(fullPath))
            {
                continue;
            }
            var content = File.ReadAllText(fullPath);
            var analysis = AnalyzeFile(repositoryRoot, relativePath, content, projectMap);
            fileAnalyses[relativePath] = analysis;
            if (analysis.IsOrphan || analysis.IsEmpty)
            {
                continue;
            }
            foreach (var part in analysis.PartialParts)
            {
                var key = (analysis.ProjectName, part.TypeName);
                if (!partialGroups.TryGetValue(key, out var list))
                {
                    list = new List<PartialPart>();
                    partialGroups[key] = list;
                }
                list.Add(new PartialPart(relativePath, part.Namespace, analysis.ExpectedNamespace));
            }
        }

        foreach (var group in partialGroups)
        {
            if (group.Value.Count < 2)
            {
                continue;
            }
            var namespaces = group
                .Value.Select(p => p.Namespace)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (namespaces.Count != 1)
            {
                foreach (var part in group.Value)
                {
                    violations.Add(
                        $"PARTIAL-MISMATCH {part.Path}: partial '{group.Key.typeName}' parts disagree on namespace "
                            + $"({string.Join(", ", namespaces)})"
                    );
                }
                continue;
            }
            var sharedNs = namespaces[0];
            var anyExpectedMatches = group.Value.Any(p =>
                string.Equals(p.Expected, sharedNs, StringComparison.Ordinal)
            );
            if (!anyExpectedMatches)
            {
                foreach (var part in group.Value)
                {
                    violations.Add(
                        $"WRONG-NS {part.Path}: declared '{sharedNs}', expected one of "
                            + string.Join(", ", group.Value.Select(p => p.Expected).Distinct())
                    );
                }
                continue;
            }
            foreach (var part in group.Value)
            {
                exempt.Add(part.Path);
            }
        }

        return (exempt, violations);
    }

    private static FixResult ApplyFixes(string repositoryRoot, IReadOnlyList<string> files)
    {
        var projectMap = BuildProjectMap(repositoryRoot);
        var (partialExempt, _) = ComputePartialInfo(repositoryRoot, files, projectMap);

        // First pass: collect (file, oldNs, newNs, typeNames)
        var renames =
            new List<(
                string path,
                string oldNs,
                string newNs,
                List<string> types,
                HashSet<string> partialTypes
            )>();

        foreach (var relativePath in files)
        {
            var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));
            if (!File.Exists(fullPath))
            {
                continue;
            }

            if (partialExempt.Contains(relativePath))
            {
                continue;
            }

            var content = File.ReadAllText(fullPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var relDir = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? string.Empty;
            var enclosing = FindEnclosingProject(relDir, projectMap);
            if (enclosing is null)
            {
                continue;
            }

            var (projectName, rootNamespace) = enclosing.Value;
            var projectDir = FindProjectDir(relDir, projectMap) ?? string.Empty;
            var subDir = string.IsNullOrEmpty(projectDir)
                ? relDir
                : (
                    relDir.Length > projectDir.Length
                        ? relDir.Substring(projectDir.Length).TrimStart('/')
                        : string.Empty
                );

            var expected = rootNamespace;
            if (!string.IsNullOrEmpty(subDir))
            {
                expected =
                    rootNamespace
                    + "."
                    + string.Join(
                        ".",
                        subDir
                            .Split('/', StringSplitOptions.RemoveEmptyEntries)
                            .Select(SanitizeSegment)
                    );
            }

            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(
                LanguageVersion.Preview
            );
            var syntaxTree = CSharpSyntaxTree.ParseText(content, parseOptions, relativePath);
            var root = (CompilationUnitSyntax)syntaxTree.GetRoot();

            var nsDecls = root.Members.OfType<BaseNamespaceDeclarationSyntax>().ToList();
            if (nsDecls.Count != 1)
            {
                continue;
            }

            var oldNs = nsDecls[0].Name.ToString();
            if (string.Equals(oldNs, expected, StringComparison.Ordinal))
            {
                continue;
            }

            // type names
            var types = new List<string>();
            var partialTypes = new HashSet<string>(StringComparer.Ordinal);
            var topLevelTypes = new List<BaseTypeDeclarationSyntax>();
            topLevelTypes.AddRange(root.Members.OfType<BaseTypeDeclarationSyntax>());
            foreach (var ns in nsDecls)
            {
                topLevelTypes.AddRange(ns.Members.OfType<BaseTypeDeclarationSyntax>());
            }
            foreach (var t in topLevelTypes)
            {
                types.Add(t.Identifier.Text);
                if (t.Modifiers.Any(m => m.Text == "partial"))
                {
                    partialTypes.Add(t.Identifier.Text);
                }
            }

            renames.Add((relativePath, oldNs, expected, types, partialTypes));
        }

        if (renames.Count == 0)
        {
            Console.WriteLine("Auto-fix: no namespace changes needed.");
            return FixResult.Succeeded(Array.Empty<string>());
        }

        // Detect conflicts: two renames targeting same (newNs, typeName)
        var targetKeys = new Dictionary<string, bool>(StringComparer.Ordinal); // key -> isPartial
        foreach (var r in renames)
        {
            foreach (var t in r.types)
            {
                var key = $"{r.newNs}|{t}";
                var isPartial = r.partialTypes.Contains(t);
                if (targetKeys.TryGetValue(key, out var existingPartial))
                {
                    // Allow collision only if both sides are partial (legitimate partial parts).
                    if (!(existingPartial && isPartial))
                    {
                        Console.Error.WriteLine(
                            $"Conflict: type '{t}' would land in '{r.newNs}' from multiple sources."
                        );
                        return FixResult.Failed;
                    }
                }
                else
                {
                    targetKeys[key] = isPartial;
                }
            }
        }

        // Apply declaration rewrites
        var filesToCheck = new List<string>();
        foreach (var r in renames)
        {
            var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, r.path));
            var content = File.ReadAllText(fullPath);
            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(
                LanguageVersion.Preview
            );
            var syntaxTree = CSharpSyntaxTree.ParseText(content, parseOptions, r.path);
            var root = (CompilationUnitSyntax)syntaxTree.GetRoot();

            var nsDecl = root.Members.OfType<BaseNamespaceDeclarationSyntax>().First();
            var newNsNode = SyntaxFactory.ParseName(r.newNs);
            var newRoot = root.ReplaceNode(nsDecl.Name, newNsNode);
            File.WriteAllText(fullPath, newRoot.ToFullString(), new UTF8Encoding(false));
            filesToCheck.Add(r.path);
            Console.WriteLine($"  {r.path}: {r.oldNs} -> {r.newNs}");
        }

        // Determine retired namespaces (old ns no longer declared by any file after fix)
        var declaredAfter = new HashSet<string>(StringComparer.Ordinal);
        foreach (
            var relativePath in GetFiles(repositoryRoot, new AnalyzerArguments(null, null, false))
        )
        {
            var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));
            if (!File.Exists(fullPath))
            {
                continue;
            }
            var content = File.ReadAllText(fullPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }
            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(
                LanguageVersion.Preview
            );
            var syntaxTree = CSharpSyntaxTree.ParseText(content, parseOptions, relativePath);
            var root = (CompilationUnitSyntax)syntaxTree.GetRoot();
            foreach (var ns in root.Members.OfType<BaseNamespaceDeclarationSyntax>())
            {
                declaredAfter.Add(ns.Name.ToString());
            }
        }

        // Group renames by old ns -> list of new ns + types
        var byOld = renames.GroupBy(r => r.oldNs).ToList();

        // Sort old names by length desc for safe prefix replacement
        var orderedOlds = byOld.Select(g => g.Key).OrderByDescending(s => s.Length).ToList();

        foreach (var oldNs in orderedOlds)
        {
            var group = byOld.First(g => g.Key == oldNs);
            var retired = !declaredAfter.Contains(oldNs);
            var movedTypes = group.SelectMany(r => r.types).Distinct().ToList();
            var newNsForType = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var r in group)
            {
                foreach (var t in r.types)
                {
                    newNsForType[t] = r.newNs;
                }
            }

            var allTracked = GetFiles(repositoryRoot, new AnalyzerArguments(null, null, false));

            if (retired)
            {
                // Blanket replace oldNs -> (per-occurrence: if multiple new ns for same old, ambiguous -> skip blanket, do per-type)
                var distinctNew = group.Select(r => r.newNs).Distinct().ToList();
                if (distinctNew.Count == 1)
                {
                    var newNs = distinctNew[0];
                    foreach (var relativePath in allTracked)
                    {
                        var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));
                        if (!File.Exists(fullPath))
                        {
                            continue;
                        }
                        var content = File.ReadAllText(fullPath);
                        if (!content.Contains(oldNs, StringComparison.Ordinal))
                        {
                            continue;
                        }
                        // Replace using directives and qualified names.
                        var lines = content.Split('\n');
                        var changed = false;
                        for (var i = 0; i < lines.Length; i++)
                        {
                            var line = lines[i];
                            if (line.Contains(oldNs, StringComparison.Ordinal))
                            {
                                // using OldNs; / using OldNs.
                                if (
                                    Regex.IsMatch(
                                        line,
                                        @"^\s*using\s+" + Regex.Escape(oldNs) + @"(\s*;|\s)"
                                    )
                                )
                                {
                                    // dedupe: if newNs using already present elsewhere, just remove this line's using
                                    // Simplest: replace oldNs token with newNs token
                                    lines[i] = line.Replace(oldNs, newNs);
                                    changed = true;
                                }
                                else
                                {
                                    lines[i] = line.Replace(oldNs, newNs);
                                    changed = true;
                                }
                            }
                        }
                        if (changed)
                        {
                            File.WriteAllText(
                                fullPath,
                                string.Join("\n", lines),
                                new UTF8Encoding(false)
                            );
                        }
                    }
                }
                else
                {
                    // Multiple new ns for one old (e.g. flat -> suffixed per file): per-type handling below
                    DoPerTypeRewrite(repositoryRoot, allTracked, oldNs, newNsForType, movedTypes);
                }
            }
            else
            {
                // Non-retired: per-type (add new using where type is referenced)
                DoPerTypeRewrite(repositoryRoot, allTracked, oldNs, newNsForType, movedTypes);
            }
        }

        // Dedupe using directives that became duplicated (best-effort: remove exact duplicate lines)
        foreach (
            var relativePath in filesToCheck
                .Concat(GetFiles(repositoryRoot, new AnalyzerArguments(null, null, false)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
        )
        {
            var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));
            if (!File.Exists(fullPath))
            {
                continue;
            }
            var content = File.ReadAllText(fullPath);
            var lines = content.Split('\n');
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var deduped = new List<string>();
            var changed = false;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                // Only dedupe real namespace-import usings, not `using var` / `using (...)`.
                var isNamespaceUsing =
                    trimmed.StartsWith("using ", StringComparison.Ordinal)
                    && trimmed.EndsWith(";", StringComparison.Ordinal)
                    && !trimmed.Contains(" var ", StringComparison.Ordinal)
                    && !trimmed.Contains("(", StringComparison.Ordinal);
                if (isNamespaceUsing)
                {
                    if (!seen.Add(trimmed))
                    {
                        changed = true;
                        continue;
                    }
                }
                deduped.Add(line);
            }
            if (changed)
            {
                File.WriteAllText(fullPath, string.Join("\n", deduped), new UTF8Encoding(false));
            }
        }

        Console.WriteLine($"Auto-fix completed: changed {renames.Count} file(s) namespace(s).");

        return FixResult.Succeeded(filesToCheck);
    }

    private static void DoPerTypeRewrite(
        string repositoryRoot,
        IReadOnlyList<string> allTracked,
        string oldNs,
        Dictionary<string, string> newNsForType,
        List<string> movedTypes
    )
    {
        foreach (var relativePath in allTracked)
        {
            var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));
            if (!File.Exists(fullPath))
            {
                continue;
            }
            var content = File.ReadAllText(fullPath);
            if (!content.Contains(oldNs, StringComparison.Ordinal))
            {
                continue;
            }

            var needsNewUsing = false;
            var newNsToAdd = new HashSet<string>(StringComparer.Ordinal);
            foreach (var t in movedTypes)
            {
                if (
                    content.Contains(t, StringComparison.Ordinal)
                    && newNsForType.TryGetValue(t, out var nn)
                )
                {
                    needsNewUsing = true;
                    newNsToAdd.Add(nn);
                }
            }

            if (!needsNewUsing)
            {
                continue;
            }

            // Replace qualified oldNs.TypeName -> newNs.TypeName
            foreach (var t in movedTypes)
            {
                if (newNsForType.TryGetValue(t, out var nn))
                {
                    content = content.Replace($"{oldNs}.{t}", $"{nn}.{t}");
                }
            }

            // Add using directives for new namespaces if missing
            foreach (var nn in newNsToAdd)
            {
                var usingLine = $"using {nn};";
                if (!content.Contains(usingLine, StringComparison.Ordinal))
                {
                    // insert after last using directive at top, or at start
                    var lines = content.Split('\n').ToList();
                    var lastUsingIdx = -1;
                    for (var i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].TrimStart().StartsWith("using ", StringComparison.Ordinal))
                        {
                            lastUsingIdx = i;
                        }
                        else if (lastUsingIdx >= 0 && !string.IsNullOrWhiteSpace(lines[i]))
                        {
                            break;
                        }
                    }
                    if (lastUsingIdx >= 0)
                    {
                        lines.Insert(lastUsingIdx + 1, usingLine);
                    }
                    else
                    {
                        lines.Insert(0, usingLine);
                    }
                    content = string.Join("\n", lines);
                }
            }

            File.WriteAllText(fullPath, content, new UTF8Encoding(false));
        }
    }

    private static IEnumerable<string> RunGit(string repositoryRoot, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException(
                $"git {string.Join(" ", arguments)} failed: {error}"
            );
        }

        return output
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
    }

    private static void RunSelfTests()
    {
        // Basic smoke: ensure SanitizeSegment works
        if (SanitizeSegment("10_Foo") != "_10_Foo")
        {
            throw new InvalidOperationException("Self-test failed: SanitizeSegment digit prefix.");
        }
        if (SanitizeSegment("Foo-Bar") != "Foo_Bar")
        {
            throw new InvalidOperationException("Self-test failed: SanitizeSegment invalid char.");
        }
        if (SanitizeSegment("Foo") != "Foo")
        {
            throw new InvalidOperationException("Self-test failed: SanitizeSegment passthrough.");
        }
    }

    private sealed class AnalyzerArguments
    {
        public string? BaseSha { get; }
        public int? MaxFilesToDisplay { get; }
        public bool Fix { get; }

        public AnalyzerArguments(string? baseSha, int? maxFilesToDisplay, bool fix)
        {
            BaseSha = baseSha;
            MaxFilesToDisplay = maxFilesToDisplay;
            Fix = fix;
        }
    }

    private sealed class ValueTupleComparer : IEqualityComparer<(string project, string typeName)>
    {
        public bool Equals(
            (string project, string typeName) x,
            (string project, string typeName) y
        ) =>
            string.Equals(x.project, y.project, StringComparison.Ordinal)
            && string.Equals(x.typeName, y.typeName, StringComparison.Ordinal);

        public int GetHashCode((string project, string typeName) obj) =>
            HashCode.Combine(obj.project, obj.typeName);
    }
}
