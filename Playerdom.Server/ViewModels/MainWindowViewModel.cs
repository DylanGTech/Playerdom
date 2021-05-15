using Playerdom.Models;
using Playerdom.Server.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Playerdom.Shared;

namespace Playerdom.Server.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string Greeting => "Welcome to the Playerdom Server!";

        private GameServer server;
        public GameServer Server
        {
            get => server;
            set => this.RaiseAndSetIfChanged(ref server, value);
        }

        private string serverCommand;
        public string ServerCommand
        {
            get => serverCommand;
            set => this.RaiseAndSetIfChanged(ref serverCommand, value);
        }


        private List<ushort> dimensions;
        public List<ushort> Dimensions
        {
            get => dimensions;
            set => this.RaiseAndSetIfChanged(ref dimensions, value);
        }

        private ushort selectedDimension = 0;
        public ushort SelectedDimension
        {
            get => selectedDimension;
            set => this.RaiseAndSetIfChanged(ref selectedDimension, value);
        }

        private uint numPlayers = 0;
        public uint NumPlayers
        {
            get => numPlayers;
            set => this.RaiseAndSetIfChanged(ref numPlayers, value);
        }

        private uint numLoadedChunks = 0;
        public uint NumLoadedChunks
        {
            get => numLoadedChunks;
            set => this.RaiseAndSetIfChanged(ref numLoadedChunks, value);
        }

        private uint numLoadedObjects = 0;
        public uint NumLoadedObjects
        {
            get => numLoadedObjects;
            set => this.RaiseAndSetIfChanged(ref numLoadedObjects, value);
        }

        private ObservableCollection<string> logs = new ObservableCollection<string>();
        public ObservableCollection<string> Logs
        {
            get => logs;
            set => this.RaiseAndSetIfChanged(ref logs, value);
        }


        public ReactiveCommand<Unit, Unit> StartServerCommand { get; }
        public ReactiveCommand<Unit, Unit> ExecuteCommand { get; }


        public MainWindowViewModel()
        {
            string savePath = Path.GetFullPath(Path.Combine("..", "Save", "Test"));
            if(!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

            server = new GameServer(PrintLog, savePath);

            StartServerCommand = ReactiveCommand.Create(() =>
            {
                if (!Server.IsRunning)
                {
                    Server.Start();

                    new Thread(() => {
                        while (IsRunning) //Poll stats
                        {
                            NumPlayers = (uint)Server.Clients.Count;

                            if (Dimensions != null && Dimensions.Count == 0 && Server.Dimensions.Count > 0)
                            {
                                Dimensions = Server.Dimensions.Keys.ToList();

                                if(!Server.Dimensions.Keys.Contains(SelectedDimension))
                                    SelectedDimension = Server.Dimensions.Keys.First();
                            }
                            Dimensions = Server.Dimensions.Keys.ToList();

                            NumLoadedChunks = (uint)Server.Dimensions[SelectedDimension].Map.LoadedChunks.Count;
                            NumLoadedObjects = (uint)Server.Dimensions[SelectedDimension].Map.LoadedObjects.Count;

                            this.RaisePropertyChanged("IsRunning");
                            Thread.Sleep(1000);
                        }
                    }).Start();
                }
            });

            ExecuteCommand = ReactiveCommand.Create(() =>
            {
                server.MessageQueue.Enqueue(new ChatMessage() { DimensionSent = SelectedDimension, TimeSent = DateTime.Now, MessageScope = ChatMessageScopes.Global, MessageType = ChatMessageTypes.Server, PlaceSent = (0, 0), Content = ServerCommand, Sender = "Server", SenderObjectId = Guid.Empty });
            });
            
            
            _numPlayersString = this.WhenAnyValue(x => x.NumPlayers).Select(num => string.Format("Number of players: {0}", num)).ToProperty(this, x => x.NumPlayersString);
            _numLoadedChunksString = this.WhenAnyValue(x => x.NumLoadedChunks).Select(num => string.Format("Number of loaded chunks: {0}", num)).ToProperty(this, x => x.NumLoadedChunksString);
            _numLoadedObjectsString = this.WhenAnyValue(x => x.NumLoadedObjects).Select(num => string.Format("Number of loaded objects: {0}", num)).ToProperty(this, x => x.NumLoadedObjectsString);

        }

        public bool IsRunning => server.IsRunning;

        private readonly ObservableAsPropertyHelper<string> _numPlayersString;
        public string NumPlayersString => _numPlayersString.Value;
        private readonly ObservableAsPropertyHelper<string> _numLoadedObjectsString;
        public string NumLoadedObjectsString => _numLoadedObjectsString.Value;
        private readonly ObservableAsPropertyHelper<string> _numLoadedChunksString;
        public string NumLoadedChunksString => _numLoadedChunksString.Value;

        public void PrintLog(string s)
        {
            Dispatcher.UIThread.InvokeAsync((Action)(() =>
            {
                if (Logs.Count >= 32)
                {
                    Logs.RemoveAt(0);
                }
                Logs.Add(s);
                this.RaisePropertyChanged("Logs");
            }));
        }
    }
}
