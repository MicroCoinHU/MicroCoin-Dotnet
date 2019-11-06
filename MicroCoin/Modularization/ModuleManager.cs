//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// ModuleManager.cs - Copyright (c) 2019 Németh Péter
//-----------------------------------------------------------------------
// MicroCoin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MicroCoin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU General Public License for more details.
//-----------------------------------------------------------------------
// You should have received a copy of the GNU General Public License
// along with MicroCoin. If not, see <http://www.gnu.org/licenses/>.
//-----------------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace MicroCoin.Modularization
{
    public class ModuleManager
    {
        private readonly ICollection<IModule> modules = new List<IModule>();
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
                        modules.Add(module);
                    }
                }
            }
        }

        public void InitModules(IServiceProvider serviceProvider)
        {
            foreach (var module in modules) module.InitModule(serviceProvider);
        }

        private Assembly ResolveAssembly(AssemblyLoadContext arg1, AssemblyName arg2)
        {
            return Assembly.LoadFrom(arg2.Name + ".dll");
        }
    }
}
