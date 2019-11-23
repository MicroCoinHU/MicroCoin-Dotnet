using MicroCoin.CheckPoints;
using MicroCoin.Modularization;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MicroCoin.SQLite
{
    public class SQLiteModule : IModule
    {
        public string Name => "MicroCoin SQLite Module";

        public void InitModule(IServiceProvider serviceProvider)
        {
        }

        public void RegisterModule(ServiceCollection serviceCollection)
        {
            using var mc = new MicroCoinDBContext();
            mc.Database.EnsureCreated();
            serviceCollection
                .AddSingleton<IAccountStorage, AccountSQLiteStorage>();
        }
    }
}
