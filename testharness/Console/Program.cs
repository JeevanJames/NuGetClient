using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Jeevan.NuGetClient;

const string packageId = "Basics";
const bool prerelease = true;
NuGetVersion? version = null;

using var client = new NuGetClient();

// List versions
IReadOnlyList<NuGetVersion> versions = await client.GetPackageVersionsAsync(packageId, prerelease);
string versionsStr = string.Join(", ", versions);
Console.WriteLine($"Versions: {versionsStr}");

// Get latest version
NuGetVersion? latestVersion = await client.GetPackageLatestVersionAsync(packageId, prerelease);
Console.WriteLine($"Latest version: {latestVersion ?? "Not found"}");

version ??= latestVersion;
if (version is null)
    return;

Console.WriteLine($"Selected version: {version}");

// Download package
FileInfo? packagePath = await client.DownloadPackageAsync(packageId, version.Value, @"D:\Temp\Packages",
    overwrite: true);
if (packagePath is null)
    throw new Exception("Could not find package");
Console.WriteLine($"Package path: {packagePath.FullName}");

// List contents of TFM
//await foreach (TfmContent content in client.GetPackageContentsForTfmAsync(packageId, version, "netstandard2.0"))
//{
//    Console.WriteLine(content.Name);
//}

IEnumerable<string> tfms = await client.GetPackageTfmsAsync(packageId, version.Value);
foreach (string tfm in tfms)
    Console.WriteLine(tfm);

string? selectedTfm = tfms.FirstOrDefault();
if (selectedTfm is null)
{
    Console.WriteLine("No TFM's found in package.");
    return;
}

Console.WriteLine($"Selected TFM: {selectedTfm}");

await client.DownloadPackageContentsForTfmAsync(packageId, version.Value, selectedTfm, @"D:\Temp\Packages");
