using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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

        public bool ModuleLoaded(string moduleName)
        {
            return _assemblies.ContainsKey(moduleName);
        }

        public bool LoadModule(string moduleName)
        {
            var exists = _moduleRepository.ExistsLocally(moduleName);
            if (!exists)
            {
                exists = _moduleRepository.Download(moduleName);
            }
            if (exists)
            {
                var modulePackagePath = _moduleRepository.GetPackagePath(moduleName);
                ExtractModulePackage(modulePackagePath, moduleName);
                var moduleDllFilename = $"Ricotta.Modules.{moduleName}.dll";
                var moduleDllFilePath = Path.Combine(_moduleCachePath, moduleName, moduleDllFilename);
                var moduleAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(moduleDllFilePath);
                _assemblies.Add(moduleName, moduleAssembly);
                return true;
            }
            return false;
        }

        public void Invoke(string moduleName, string methodName, object[] arguments)
        {
            var exists = ModuleLoaded(moduleName);
            if (!exists)
            {
                exists = LoadModule(moduleName);
            }
            if (!exists)
            {
                throw new Exception($"Module {moduleName} does not exist");
            }
            var assembly = _assemblies[moduleName];
            Invoke(assembly, moduleName, methodName, arguments);
        }

        private void Invoke(Assembly assembly, string moduleName, string methodName, object[] arguments)
        {
            var moduleClass = assembly.GetType($"Ricotta.Modules.{moduleName}");
            object[] constructorArgs = null;
            if (moduleName == "Package")
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
            var modulePath = Path.Combine(_moduleCachePath, moduleName);
            Directory.CreateDirectory(modulePath);
            ZipFile.ExtractToDirectory(packagePath, modulePath);
        }
    }
}
