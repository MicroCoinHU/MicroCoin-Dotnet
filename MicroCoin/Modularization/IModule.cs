using Microsoft.Extensions.DependencyInjection;

namespace MicroCoin.Modularization
{
    public interface IModule
    {
        public string Name { get; }
        void RegisterModule(ServiceCollection serviceCollection);
        void InitModule();
    }
}
