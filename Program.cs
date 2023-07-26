using System;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Linq;

using System.CommandLine;

using NuGet.Protocol;
using NuGet.Protocol.Core.Types;



namespace ChocoDependencies
{
    class Program
    {

        public static NuGet.Common.NullLogger Logger = new NuGet.Common.NullLogger();

        static void Main(string[] args)
        {

            var repoArgument = new Argument<string>(
                    name: "repository",
                    description: "Nuget V3 Repository (file or https)",
                    getDefaultValue: () => @"D:\nuget_drop");
//                    getDefaultValue: () => @"https://community.chocolatey.org/api/v2/");
            var packageArgument = new Argument<string>(
                    name: "package",
                    description: "package id or '' ",
                    getDefaultValue: () => @"");

            var rootCommand = new RootCommand("Display Nuget Dependencies");
            rootCommand.AddArgument(repoArgument);
            rootCommand.AddArgument(packageArgument);

            rootCommand.SetHandler((repoArgumentValue, packageArgumentValue) =>
            {
                CheckDependencies(repoArgumentValue, packageArgumentValue);
            }, repoArgument, packageArgument);

            rootCommand.Invoke(args);
        }


        static public void CheckDependencies(string repoArgumentValue, string packageArgumentValue)
        {

            var repo = Repository.Factory.GetCoreV3(repoArgumentValue);
            PackageSearchResource searchResource = repo.GetResource<PackageSearchResource>();
            IEnumerable<IPackageSearchMetadata> allLocalPackages = searchResource.SearchAsync(packageArgumentValue, new SearchFilter(includePrerelease: true), 0, 1000, Logger, CancellationToken.None).Result;
            foreach (IPackageSearchMetadata package in allLocalPackages)
            {
                OutputGraph(repo, allLocalPackages, package, new HashSet<string>(), 0);
                Console.WriteLine("");
            }
        }

        static void OutputGraph(SourceRepository repo, IEnumerable<IPackageSearchMetadata> allLocalPackages, IPackageSearchMetadata package, HashSet<string> dependencies, int depth)
        {
            var indent = new string(' ', depth);
            if (depth == 0)
            {
                Console.WriteLine($"{indent}{package.Identity.Id}  {package.Identity.Version}", new string(' ', depth));
            }
            else
            {
                Console.WriteLine($"{indent}found {package.Identity.Id}  {package.Identity.Version}", new string(' ', depth));
            }

            foreach (var dependencySet in package.DependencySets)
            {
                foreach (var depPackage in dependencySet.Packages)
                {
                    var indent1 = new string(' ', depth + 2);

                    Console.WriteLine($"{indent1}wants {depPackage.Id}  {depPackage.VersionRange}");

                    var item = allLocalPackages.FirstOrDefault(item => item.Identity.Id.ToLower() == depPackage.Id.ToLower());
                    if (item is not null)
                    {
                        if (dependencies.Contains(item.Identity.Id))
                        {
                            continue;
                        }
                        else
                        {
                            OutputGraph(repo, allLocalPackages, item, dependencies, depth + 2);
                            dependencies.Add(item.Identity.Id);
                        }
                    }
                    else
                    {
                        Console.Beep();
                        Console.Error.WriteLine($"{depPackage.Id} not found");
                    }
                }
            }
        }
    }
}

