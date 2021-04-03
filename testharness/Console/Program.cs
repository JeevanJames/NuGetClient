using System;
using System.Collections.Generic;

using Jeevan.NuGetClient;

using Semver;

const string packageId = "Collections.NET";
const bool prerelease = false;

var client = new NuGetClient();

IReadOnlyList<SemVersion> versions = await client.GetPackageVersionsAsync(packageId, prerelease);
string versionsStr = string.Join(", ", versions);
Console.WriteLine($"Versions: {versionsStr}");

SemVersion? latestVersion = await client.GetLatestPackageVersionAsync(packageId, prerelease);
Console.WriteLine($"Latest version: {latestVersion ?? "Not found"}");

string? packagePath = await client.DownloadPackageToFileAsync(packageId, latestVersion ?? "0.1.0", @"D:\Temp\Package.nupkg.zip",
    overwrite: true);
if (packagePath is null)
    throw new Exception("Could not find package");
Console.WriteLine($"Package path: {packagePath}");
