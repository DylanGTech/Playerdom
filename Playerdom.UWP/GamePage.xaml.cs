using Playerdom.Shared;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Playerdom.UWP;

public sealed partial class GamePage : Page
{
    readonly PlayerdomGame _game;

    public GamePage()
    {
        this.InitializeComponent();

        // Create the game.
        var launchArguments = string.Empty;
        _game = MonoGame.Framework.XamlGame<PlayerdomGame>.Create(launchArguments, Window.Current.CoreWindow, swapChainPanel);
    }
}