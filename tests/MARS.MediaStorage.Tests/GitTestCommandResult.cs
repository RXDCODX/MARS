using System.Diagnostics;
using System.Text;

namespace MARS.MediaStorage.Tests;

internal readonly record struct GitTestCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError
)
{
    public bool Success => ExitCode == 0;
}
