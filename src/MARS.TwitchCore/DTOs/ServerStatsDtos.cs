namespace MARS.TwitchCore.DTOs;

public class ServerStatsResponse
{
    public double CpuUsagePercent { get; set; }
    public long MemoryWorkingSetBytes { get; set; }
    public long MemoryPrivateBytes { get; set; }
    public long MemoryGcHeapBytes { get; set; }
    public long MemoryTotalBytes { get; set; }
    public double UptimeSeconds { get; set; }
    public int ThreadCount { get; set; }
    public string OsVersion { get; set; } = string.Empty;
    public string RuntimeVersion { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public bool IsEventSubConnected { get; set; }
    public bool IsTwitchChatConnected { get; set; }
    public bool IsPuntoSwitcherEnabled { get; set; }
    public string? NearestWeddingAnniversaryName { get; set; }
    public DateTime? NearestWeddingAnniversaryDate { get; set; }
    public string? NearestWeddingAnniversaryUser { get; set; }
}
