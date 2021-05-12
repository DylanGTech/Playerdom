using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Playerdom.Models;
using Playerdom.Server.ViewModels;
using Playerdom.Server.Views;

namespace Playerdom.Server
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        GameServer gameServer = null;

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                gameServer = ((MainWindowViewModel)desktop.MainWindow.DataContext).Server;

                desktop.MainWindow.Closed += delegate {
                    gameServer.Dispose();
                    gameServer = null;
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
