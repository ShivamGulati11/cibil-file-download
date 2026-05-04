using Azure.Storage.Blobs;

try
{
    return await Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unexpected error: {ex.Message}");
    return 1;
}

static async Task<int> Run()
{
Console.WriteLine("=== Azure Blob Storage File Downloader ===");
Console.WriteLine();

// Prompt for connection string
Console.Write("Enter Azure Storage connection string: ");
string? connectionString = Console.ReadLine()?.Trim();
if (string.IsNullOrEmpty(connectionString))
{
    Console.Error.WriteLine("Error: Connection string cannot be empty.");
    return 1;
}

// Prompt for container name
Console.Write("Enter container name: ");
string? containerName = Console.ReadLine()?.Trim();
if (string.IsNullOrEmpty(containerName))
{
    Console.Error.WriteLine("Error: Container name cannot be empty.");
    return 1;
}

// Prompt for output directory
Console.Write("Enter local output directory (leave blank for current directory): ");
string? outputDir = Console.ReadLine()?.Trim();
if (string.IsNullOrEmpty(outputDir))
{
    outputDir = Directory.GetCurrentDirectory();
}

// Prompt for file paths
Console.WriteLine("Enter blob file paths to download (one per line).");
Console.WriteLine("Type a blank line when done:");
var blobPaths = new List<string>();
while (true)
{
    string? line = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(line))
        break;
    blobPaths.Add(line);
}

if (blobPaths.Count == 0)
{
    Console.Error.WriteLine("Error: No file paths provided.");
    return 1;
}

// Create the BlobServiceClient
BlobServiceClient blobServiceClient;
try
{
    blobServiceClient = new BlobServiceClient(connectionString);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error creating BlobServiceClient: {ex.Message}");
    return 1;
}

BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

Console.WriteLine();
Console.WriteLine($"Downloading {blobPaths.Count} file(s) to: {outputDir}");
Console.WriteLine();

int successCount = 0;
int failCount = 0;

// Resolve the output directory to its full canonical path to guard against traversal
string resolvedOutputDir = Path.GetFullPath(outputDir);

foreach (string blobPath in blobPaths)
{
    // Determine local file path, preserving folder structure from the blob path
    string localFilePath = Path.GetFullPath(
        Path.Combine(resolvedOutputDir, blobPath.Replace('/', Path.DirectorySeparatorChar)));

    // Prevent path traversal: the relative path from the output dir must not start with ".."
    string relativePath = Path.GetRelativePath(resolvedOutputDir, localFilePath);
    if (relativePath.StartsWith("..", StringComparison.Ordinal))
    {
        Console.WriteLine($"  SKIPPED '{blobPath}' (path traversal detected — outside output directory)");
        failCount++;
        continue;
    }

    string? localDir = Path.GetDirectoryName(localFilePath);
    if (!string.IsNullOrEmpty(localDir) && !Directory.Exists(localDir))
    {
        Directory.CreateDirectory(localDir);
    }

    BlobClient blobClient = containerClient.GetBlobClient(blobPath);
    Console.Write($"  Downloading '{blobPath}' ... ");
    try
    {
        await blobClient.DownloadToAsync(localFilePath);
        Console.WriteLine($"OK -> {localFilePath}");
        successCount++;
    }
    catch (Exception ex)
    {
        Console.WriteLine("FAILED");
        Console.Error.WriteLine($"    Error: {ex.Message}");
        failCount++;
    }
}

Console.WriteLine();
Console.WriteLine($"Done. {successCount} succeeded, {failCount} failed.");
return failCount > 0 ? 1 : 0;
}
