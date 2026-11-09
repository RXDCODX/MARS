namespace MARS.Admin.Api.Contracts;

public class ValidateFolderResponse
{
    public bool Exists { get; set; }
    public bool Accessible { get; set; }
    public int VideoFilesCount { get; set; }
    public List<string> SampleFiles { get; set; } = [];
}
