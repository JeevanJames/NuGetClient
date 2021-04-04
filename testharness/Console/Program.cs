using System;
using System.Collections.Generic;
using System.Linq;

using Jeevan.NuGetClient;

using Semver;

const string packageId = "AutoMapper";
const bool prerelease = false;
NuGetVersion? version = null;

var client = new NuGetClient();

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
string? packagePath = await client.DownloadPackageAsync(packageId, version.Value,
    @$"D:\Temp\Packages\{packageId}.{version}.nupkg.zip", overwrite: true);
if (packagePath is null)
    throw new Exception("Could not find package");
Console.WriteLine($"Package path: {packagePath}");

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
