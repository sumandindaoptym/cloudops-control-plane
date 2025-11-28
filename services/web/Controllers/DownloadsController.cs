using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace CloudOps.Web.Controllers;

[Route("downloads")]
public class DownloadsController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DownloadsController> _logger;
    private readonly string _projectRoot;

    public DownloadsController(IWebHostEnvironment environment, ILogger<DownloadsController> logger)
    {
        _environment = environment;
        _logger = logger;
        _projectRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", ".."));
    }

    [HttpGet("cloudops-agent-linux-x64.tar.gz")]
    public IActionResult DownloadLinuxAgent()
    {
        var artifactPath = Path.Combine(_projectRoot, "artifacts", "agents", "cloudops-agent-linux-x64.tar.gz");
        
        if (System.IO.File.Exists(artifactPath))
        {
            var fileBytes = System.IO.File.ReadAllBytes(artifactPath);
            return File(fileBytes, "application/gzip", "cloudops-agent-linux-x64.tar.gz");
        }
        
        return NotFound(new { error = "Linux agent binary not built yet. Run 'scripts/package-agent.sh' to build artifacts." });
    }

    [HttpGet("cloudops-agent-windows-x64.zip")]
    public IActionResult DownloadWindowsAgent()
    {
        var artifactPath = Path.Combine(_projectRoot, "artifacts", "agents", "cloudops-agent-windows-x64.zip");
        
        if (System.IO.File.Exists(artifactPath))
        {
            var fileBytes = System.IO.File.ReadAllBytes(artifactPath);
            return File(fileBytes, "application/zip", "cloudops-agent-windows-x64.zip");
        }
        
        return NotFound(new { error = "Windows agent binary not built yet. Run 'scripts/package-agent.sh' to build artifacts." });
    }

    [HttpGet("cloudops-agent-helm-chart.tgz")]
    public IActionResult DownloadHelmChart()
    {
        var helmChartDir = Path.Combine(_projectRoot, "deploy", "helm", "cloudops-agent");
        
        if (!Directory.Exists(helmChartDir))
        {
            return NotFound(new { error = "Helm chart directory not found." });
        }

        try
        {
            var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                AddDirectoryToArchive(archive, helmChartDir, "cloudops-agent");
            }
            
            memoryStream.Position = 0;
            return File(memoryStream, "application/gzip", "cloudops-agent-helm-chart.tgz");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Helm chart archive");
            return StatusCode(500, new { error = "Failed to create Helm chart archive." });
        }
    }

    [HttpGet("cloudops-agent-k8s-manifests.yaml")]
    public IActionResult DownloadK8sManifests()
    {
        var manifestPath = Path.Combine(_projectRoot, "deploy", "k8s", "cloudops-agent-k8s-manifests.yaml");
        
        if (System.IO.File.Exists(manifestPath))
        {
            var fileBytes = System.IO.File.ReadAllBytes(manifestPath);
            return File(fileBytes, "text/yaml", "cloudops-agent-k8s-manifests.yaml");
        }
        
        return NotFound(new { error = "Kubernetes manifests file not found." });
    }

    [HttpGet("Dockerfile")]
    public IActionResult DownloadDockerfile()
    {
        var dockerfilePath = Path.Combine(_projectRoot, "services", "agent", "Dockerfile");
        
        if (System.IO.File.Exists(dockerfilePath))
        {
            var fileBytes = System.IO.File.ReadAllBytes(dockerfilePath);
            return File(fileBytes, "text/plain", "Dockerfile");
        }
        
        return NotFound(new { error = "Dockerfile not found." });
    }

    [HttpGet("install-linux.sh")]
    public IActionResult DownloadLinuxInstallScript()
    {
        var scriptPath = Path.Combine(_projectRoot, "services", "agent", "scripts", "install-linux.sh");
        
        if (System.IO.File.Exists(scriptPath))
        {
            var fileBytes = System.IO.File.ReadAllBytes(scriptPath);
            return File(fileBytes, "text/x-shellscript", "install.sh");
        }
        
        return NotFound(new { error = "Linux install script not found." });
    }

    [HttpGet("install-windows.ps1")]
    public IActionResult DownloadWindowsInstallScript()
    {
        var scriptPath = Path.Combine(_projectRoot, "services", "agent", "scripts", "install-windows.ps1");
        
        if (System.IO.File.Exists(scriptPath))
        {
            var fileBytes = System.IO.File.ReadAllBytes(scriptPath);
            return File(fileBytes, "text/plain", "install.ps1");
        }
        
        return NotFound(new { error = "Windows install script not found." });
    }

    private void AddDirectoryToArchive(ZipArchive archive, string sourceDir, string entryPrefix)
    {
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var entryName = Path.Combine(entryPrefix, relativePath).Replace('\\', '/');
            archive.CreateEntryFromFile(file, entryName);
        }
    }
}
