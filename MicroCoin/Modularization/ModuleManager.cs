using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace MicroCoin.Modularization
{
    public class ModuleManager
    {
        public void LoadModules(ServiceCollection serviceCollection)
        {
            Type imoduleType = typeof(IModule);
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            foreach (var file in Directory.GetFiles(assemblyPath, "MicroCoin*.dll"))
            {
                AssemblyLoadContext.Default.Resolving += ResolveAssembly;
                var types = AssemblyLoadContext.Default.LoadFromAssemblyPath(file).GetExportedTypes().Where(p => imoduleType.IsAssignableFrom(p) && p.IsClass);
                if (types.Count() > 0)
                {
                    foreach(var t in types)
                    {
                        IModule module = (IModule) Activator.CreateInstance(t);
                        module.RegisterModule(serviceCollection);
                        LogManager.GetCurrentClassLogger().Debug("{0} loaded", module.Name);
                    }
                }
            }
        }

        private Assembly ResolveAssembly(AssemblyLoadContext arg1, AssemblyName arg2)
        {
            return Assembly.LoadFrom(arg2.Name + ".dll");
        }
    }
}
