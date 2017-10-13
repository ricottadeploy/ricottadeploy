using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Ricotta.Agent
{
    public class ModuleCache
    {
        private string _moduleCachePath;
        private NuGetRepository _moduleRepository;
        private Dictionary<string, Assembly> _assemblies;   // <ModuleName, Assembly>

        public ModuleCache(string moduleCachePath, NuGetRepository moduleRepository)
        {
            _moduleCachePath = moduleCachePath;
            _moduleRepository = moduleRepository;
            _assemblies = new Dictionary<string, Assembly>();
        }

        public bool ModuleLoaded(string fullModuleName)
        {
            return _assemblies.ContainsKey(fullModuleName);
        }

        public bool LoadModule(string fullModuleName)
        {
            var exists = _moduleRepository.ExistsLocally(fullModuleName);
            if (!exists)
            {
                exists = _moduleRepository.Download(fullModuleName);
            }
            if (exists)
            {
                var modulePackagePath = _moduleRepository.GetPackagePath(fullModuleName);
                ExtractModulePackage(modulePackagePath, fullModuleName);
                var moduleDllFilename = $"{fullModuleName}.dll";
                var modulePathInCache = Path.Combine(_moduleCachePath, fullModuleName);
                var moduleDllFilePath = Directory.GetFiles(modulePathInCache, moduleDllFilename, SearchOption.AllDirectories).SingleOrDefault();
                var moduleAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(moduleDllFilePath);
                _assemblies.Add(fullModuleName, moduleAssembly);
                return true;
            }
            return false;
        }

        public void Invoke(string fullModuleName, string methodName, object[] arguments)
        {
            var exists = ModuleLoaded(fullModuleName);
            if (!exists)
            {
                exists = LoadModule(fullModuleName);
            }
            if (!exists)
            {
                throw new Exception($"Module {fullModuleName} does not exist");
            }
            var assembly = _assemblies[fullModuleName];
            Invoke(assembly, fullModuleName, methodName, arguments);
        }

        private void Invoke(Assembly assembly, string fullModuleName, string methodName, object[] arguments)
        {
            var moduleClass = assembly.GetType(fullModuleName);
            object[] constructorArgs = null;
            if (fullModuleName == "Package")
            {
                constructorArgs = new object[] { Log.Logger, null };    // TODO: Logger and FileRepository
            }
            else
            {
                constructorArgs = new object[] { Log.Logger };
            }
            var instance = Activator.CreateInstance(moduleClass, constructorArgs);
            var method = moduleClass.GetMethod(methodName);
            method.Invoke(instance, arguments);
        }

        private void ExtractModulePackage(string packagePath, string moduleName)
        {
            var modulePathInCache = Path.Combine(_moduleCachePath, moduleName);
            Directory.CreateDirectory(modulePathInCache);
            ZipFile.ExtractToDirectory(packagePath, modulePathInCache, true);
        }
    }
}
