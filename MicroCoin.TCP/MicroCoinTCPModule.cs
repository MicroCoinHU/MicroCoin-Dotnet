//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2019 Peter Nemeth
// MicroCoinTCPModule.cs - Copyright (c) 2019 Németh Péter
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
using System;
using MicroCoin.Modularization;
using MicroCoin.Net;
using Microsoft.Extensions.DependencyInjection;

namespace MicroCoin.TCP
{
    public class MicroCoinTCPModule : IModule
    {
        public string Name => "MicroCoin TCP/IP networking module";

        public void InitModule(IServiceProvider serviceProvider)
        {
        }

        public void RegisterModule(ServiceCollection serviceCollection)
        {
            serviceCollection
                .AddTransient<INetClient, NetClient>()
                .AddTransient<INetServer, NetServer>();
        }
    }
}
