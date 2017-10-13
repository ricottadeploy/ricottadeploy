using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet.Versioning
{
    public class NuGetPackageVersion
    {
        public static IEnumerable<string> SortAscending(IEnumerable<string> versionList)
        {
            var semverList = new List<SemanticVersion>();
            foreach (var ver in versionList)
            {
                semverList.Add(SemanticVersion.Parse(ver));
            }
            var semverListSorted = semverList.OrderBy(x => x).ToList();
            var versionListSorted = semverListSorted.Select(x => x.ToFullString());
            return versionListSorted;
        }

        public static IEnumerable<string> SortDescending(IEnumerable<string> versionList)
        {
            var semverList = new List<SemanticVersion>();
            foreach (var ver in versionList)
            {
                semverList.Add(SemanticVersion.Parse(ver));
            }
            var semverListSorted = semverList.OrderByDescending(x => x).ToList();
            var versionListSorted = semverListSorted.Select(x => x.ToFullString());
            return versionListSorted;
        }

        public static string GetLatestVersion(IEnumerable<string> versionList)
        {
            return SortDescending(versionList).FirstOrDefault();
        }

        /// <summary>
        /// Expected directory structure:
        /// repositoryPath/module1/1.0.0
        ///                       /1.1.0
        ///                       /1.2.0
        ///               /module2/1.0.0
        ///                       /1.0.1
        /// </summary>
        /// <param name="path">Module directory path in repository. Eg: /path/to/repositoryPath/module1</param>
        /// <returns></returns>
        public static string GetLatestVersion(string modulePath)
        {
            if (!Directory.Exists(modulePath))
            {
                return null;
            }
            var versionDirectories = Directory.GetDirectories(modulePath).Select(x => new DirectoryInfo(x).Name);
            return GetLatestVersion(versionDirectories);
        }

        public static string GetPackagePath(string modulePath, string version)
        {
            if (!Directory.Exists(modulePath))
            {
                return null;
            }
            var versionDirectoryPath = Path.Combine(modulePath, version);
            var versionPackagePath = Directory.GetFiles(versionDirectoryPath, "*.nupkg").SingleOrDefault();
            return versionPackagePath;
        }

        public static string GetLatestVersionPackagePath(string modulePath)
        {
            if (!Directory.Exists(modulePath))
            {
                return null;
            }
            var latestVersion = GetLatestVersion(modulePath);
            if (latestVersion == null)
            {
                return null;
            }
            return GetPackagePath(modulePath, latestVersion);
        }
    }
}
