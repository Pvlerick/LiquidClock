#tool "nuget:?package=GitVersion.CommandLine"

var target = Argument("target", "Build");

Task("Build")
  .DoesForEach(GetFiles("src/**/*.*proj"), file =>
  {
    DotNetCoreClean(file.FullPath);
    GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = true
    });
    DotNetCoreRestore(file.FullPath);
    DotNetCoreBuild(file.FullPath);
  });

Task("Test")
  .IsDependentOn("Build")
  .DoesForEach(GetFiles("test/**/*.*proj"), file =>
  {
    DotNetCoreTest(file.FullPath);
  });

Task("Pack")
  .DoesForEach(GetFiles("src/**/*.*proj"), file =>
  {
    DotNetCorePack(
      file.FullPath, 
      new DotNetCorePackSettings()
      {
        ArgumentCustomization = args => args.Append("/p:Version=" + GitVersion().NuGetVersion)
      });
  });

Task("Default")
  .IsDependentOn("Build")
  .IsDependentOn("Test")
  .IsDependentOn("Pack");

RunTarget("Default");