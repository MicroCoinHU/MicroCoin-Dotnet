using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MicroCoin.Wallet
{
    public class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
