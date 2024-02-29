using Playerdom.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Playerdom.Shared;

namespace Playerdom.Server.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public string Greeting => "Welcome to the Playerdom Server!";

    private GameServer _server;
    public GameServer Server
    {
        get => _server;
        set => this.RaiseAndSetIfChanged(ref _server, value);
    }

    private string _serverCommand;
    public string ServerCommand
    {
        get => _serverCommand;
        set => this.RaiseAndSetIfChanged(ref _serverCommand, value);
    }


    private List<ushort> _dimensions;
    public List<ushort> Dimensions
    {
        get => _dimensions;
        set => this.RaiseAndSetIfChanged(ref _dimensions, value);
    }

    private ushort _selectedDimension;
    public ushort SelectedDimension
    {
        get => _selectedDimension;
        set => this.RaiseAndSetIfChanged(ref _selectedDimension, value);
    }

    private uint _numPlayers = 0;
    public uint NumPlayers
    {
        get => _numPlayers;
        set => this.RaiseAndSetIfChanged(ref _numPlayers, value);
    }

    private uint _numLoadedChunks = 0;
    public uint NumLoadedChunks
    {
        get => _numLoadedChunks;
        set => this.RaiseAndSetIfChanged(ref _numLoadedChunks, value);
    }

    private uint _numLoadedObjects = 0;
    public uint NumLoadedObjects
    {
        get => _numLoadedObjects;
        set => this.RaiseAndSetIfChanged(ref _numLoadedObjects, value);
    }

    private ObservableCollection<string> _logs = new ObservableCollection<string>();
    public ObservableCollection<string> Logs
    {
        get => _logs;
        set => this.RaiseAndSetIfChanged(ref _logs, value);
    }


    public ReactiveCommand<Unit, Unit> StartServerCommand { get; }
    public ReactiveCommand<Unit, Unit> ExecuteCommand { get; }


    public MainWindowViewModel()
    {
        string savePath = Path.GetFullPath(Path.Combine("..", "Save", "Test"));
        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

        Server = new GameServer(PrintLog, savePath);

        StartServerCommand = ReactiveCommand.Create(() =>
        {
            if (!Server.IsRunning)
            {
                Server.Start();

                new Thread(() =>
                {
                    while (IsRunning) //Poll stats
                    {
                        NumPlayers = (uint)Server.Clients.Count;
                        Dimensions = Server.Dimensions.Keys.ToList();
                        if (!Server.Dimensions.Keys.Contains(SelectedDimension))
                            SelectedDimension = Server.Dimensions.Keys.First();
                        else this.RaisePropertyChanged(nameof(SelectedDimension));

                        NumLoadedChunks = (uint)Server.Dimensions[SelectedDimension].Map.LoadedChunks.Count;
                        NumLoadedObjects = (uint)Server.Dimensions[SelectedDimension].Map.LoadedObjects.Count;

                        this.RaisePropertyChanged(nameof(IsRunning));
                        Thread.Sleep(1000);
                    }
                }).Start();
            }
        });

        ExecuteCommand = ReactiveCommand.Create(() =>
        {
            Server.MessageQueue.Enqueue(new ChatMessage() { DimensionSent = SelectedDimension, TimeSent = DateTime.Now, MessageScope = ChatMessageScopes.Global, MessageType = ChatMessageTypes.Server, PlaceSent = (0, 0), Content = ServerCommand, Sender = "Server", SenderObjectId = Guid.Empty });
        });


        _numPlayersString = this.WhenAnyValue(x => x.NumPlayers).Select(num => string.Format("Number of players: {0}", num)).ToProperty(this, x => x.NumPlayersString);
        _numLoadedChunksString = this.WhenAnyValue(x => x.NumLoadedChunks).Select(num => string.Format("Number of loaded chunks: {0}", num)).ToProperty(this, x => x.NumLoadedChunksString);
        _numLoadedObjectsString = this.WhenAnyValue(x => x.NumLoadedObjects).Select(num => string.Format("Number of loaded objects: {0}", num)).ToProperty(this, x => x.NumLoadedObjectsString);

    }

    public bool IsRunning => _server.IsRunning;

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
            this.RaisePropertyChanged(nameof(Logs));
        }));
    }
}