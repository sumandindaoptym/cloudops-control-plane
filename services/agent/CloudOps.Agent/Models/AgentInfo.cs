namespace CloudOps.Agent.Models;

public class AgentInfo
{
    public string MachineName { get; set; } = Environment.MachineName;
    public string OperatingSystem { get; set; } = GetOperatingSystem();
    public string Architecture { get; set; } = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
    public string AgentVersion { get; set; } = typeof(AgentInfo).Assembly.GetName().Version?.ToString() ?? "1.0.0";
    public int LogicalCores { get; set; } = Environment.ProcessorCount;
    public double? MemoryGb { get; set; }
    public double? DiskSpaceGb { get; set; }
    
    private static string GetOperatingSystem()
    {
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsLinux()) return "Linux";
        if (OperatingSystem.IsMacOS()) return "macOS";
        return "Unknown";
    }
    
    public static AgentInfo Collect()
    {
        var info = new AgentInfo();
        
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            info.MemoryGb = Math.Round(gcInfo.TotalAvailableMemoryBytes / (1024.0 * 1024.0 * 1024.0), 2);
        }
        catch { }
        
        try
        {
            var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            var drive = new DriveInfo(currentDir.Root.FullName);
            info.DiskSpaceGb = Math.Round(drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 2);
        }
        catch { }
        
        return info;
    }
}
