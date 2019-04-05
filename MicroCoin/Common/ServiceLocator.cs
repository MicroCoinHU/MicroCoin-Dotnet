using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroCoin.Common
{
    internal static class ServiceLocator
    {
        public static ServiceProvider ServiceProvider { get; set; }
        public static T GetService<T>() => ServiceProvider.GetService<T>();
        public static IEventAggregator EventAggregator => ServiceProvider.GetService<IEventAggregator>();
    }
}
