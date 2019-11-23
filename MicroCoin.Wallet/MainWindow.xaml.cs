using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using MicroCoin.Chain;
using MicroCoin.CheckPoints;
using MicroCoin.Modularization;
using System;
using System.Collections.Generic;

namespace MicroCoin.Wallet
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();            
            if (Application.Current !=null && Application.Current.ApplicationLifetime != null && Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
            {
                if (!Design.IsDesignMode)
                {
                    accounts = ServiceLocator.GetService<ICheckPointService>().GetAccounts();
                    var dark = new StyleInclude(new Uri("resm:Styles?assembly=ControlCatalog"))
                    {
                        Source = new Uri("resm:Avalonia.Themes.Default.Accents.BaseDark.xaml?assembly=Avalonia.Themes.Default")
                    };
                    Styles.Add(dark);
                }
            }
            else
            {
                accounts = new List<Account>();
            }
            DataContext = this;
        }

        private readonly IReadOnlyList<Account> accounts;

        public decimal TotalBalance { get {
                return ServiceLocator.GetService<ICheckPointService>().GetTotalBalance();    
            }
        }

        public IReadOnlyList<Account> Accounts
        {
            get
            {
                return accounts;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}