#region Using Statements
using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

using MessagePack;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using Playerdom.Shared.GameObjects;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Security.Permissions;
using System.Security.Cryptography;
using System.Buffers;
using System.Runtime.CompilerServices;
using Playerdom.Shared.GameEntities;
using System.Net;
using FontStashSharp;
using FontStashSharp.Interfaces;
using System.IO;

#endregion

namespace Playerdom.Shared
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class PlayerdomGame : Game
    {
        internal class Texture2DManager : ITexture2DManager
        {
            readonly GraphicsDevice _device;

            public Texture2DManager(GraphicsDevice device)
            {
                if (device == null)
                {
                    throw new ArgumentNullException(nameof(device));
                }

                _device = device;
            }

            public object CreateTexture(int width, int height)
            {
                return new Texture2D(_device, width, height);
            }

            public void SetTextureData(object texture, System.Drawing.Rectangle bounds, byte[] data)
            {
                var mgTexture = (Texture2D)texture;
                mgTexture.SetData(0, 0, new Microsoft.Xna.Framework.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height),
                    data, 0, bounds.Width * bounds.Height * 4);
            }
        }

        internal class Renderer : IFontStashRenderer
        {
            SpriteBatch _batch;

            public Renderer(SpriteBatch batch)
            {
                if (batch == null)
                {
                    throw new ArgumentNullException(nameof(batch));
                }

                _batch = batch;
            }

            public void Draw(object texture, System.Numerics.Vector2 position, System.Drawing.Rectangle? sourceRectangle, System.Drawing.Color color, float rotation, System.Numerics.Vector2 origin, System.Numerics.Vector2 scale, float depth)
            {
                var textureWrapper = (Texture2D)texture;

                _batch.Draw(textureWrapper,
                    position.ToXNA(),
                    sourceRectangle == null ? default(Microsoft.Xna.Framework.Rectangle?) : sourceRectangle.Value.ToXNA(),
                    color.ToXNA(),
                    rotation,
                    origin.ToXNA(),
                    scale.ToXNA(),
                    SpriteEffects.None,
                    depth);
            }
        }






        CurrentClientState currentState = new CurrentClientState(null, new ConcurrentDictionary<Guid, GameObject>(), new ConcurrentDictionary<Guid, GameEntities.GameEntity>());

        public static MessagePackSerializerOptions SerializerSettings
        {
            get
            {
                //MessagePackSerializer.DefaultOptions = MessagePack.Resolvers.ContractlessStandardResolver.Options.WithSecurity(MessagePackSecurity.UntrustedData);
                return MessagePackSerializer.DefaultOptions.WithSecurity(MessagePackSecurity.UntrustedData);
            }
        }
        
        Texture2D playerTexture = null;
        Texture2D merchantTexture = null;
        Texture2D dylangtechTexture = null;

        DynamicSpriteFont debugFont = null;
        Texture2D defaultRectangle = null;


        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        RenderTarget2D scene;

        TcpClient _tcpClient;
        NetworkStream _netStream;

        Guid? token = null;
        Guid? focusedObjectId = null;


        ushort dimensionId = 0;
        (float hue, float sat, float val) hsv = (0f, 0f, 0f);


        ConcurrentQueue<ChatMessage> messages = new ConcurrentQueue<ChatMessage>();
        bool isTyping = false;
        bool messageReadyToSend = false;
        string typedMessage = "";
        string username = "";
        string password = "";
        bool passwordSelected = false;


        readonly IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Loopback, 25565);

        public PlayerdomGame(IPEndPoint endPoint = null)
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.IsFullScreen = false;

            if (endPoint != null) serverEndpoint = endPoint;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            base.Initialize();

            scene = new RenderTarget2D(
                graphics.GraphicsDevice,
                1920, 1080, false,
                SurfaceFormat.Color, DepthFormat.None);

            TouchPanel.DisplayWidth = 1920;
            TouchPanel.DisplayHeight = 1080;
            TouchPanel.EnableMouseTouchPoint = true;

            _tcpClient = new TcpClient();
            _tcpClient.Connect(serverEndpoint);
            _netStream = _tcpClient.GetStream();

            Thread newThread1 = new Thread(() => ReceiveOutputAsync(this)) { Name = "OutputThread" };
            Thread newThread2 = new Thread(() => SendInputAsync(this)) { Name = "InputThread" };
            newThread1.Start();
            newThread2.Start();

            Window.TextInput += (sender, e) =>
            {
                if(focusedObjectId.HasValue)
                {
                    if (!isTyping) return;
                    KeyboardState ks = Keyboard.GetState();
                    if (ks.IsKeyDown(Keys.Escape))
                    {
                        typedMessage = "";
                        isTyping = false;
                        return;
                    }

                    if (ks.IsKeyDown(Keys.Enter))
                    {
                        isTyping = false;
                        messageReadyToSend = true;
                        return;
                    }

                    if (ks.IsKeyDown(Keys.Back) && typedMessage.Length > 0)
                    {
                        typedMessage = typedMessage.Substring(0, typedMessage.Length - 1);
                        return;
                    }


                    if (isTyping)
                    {
                        /*
                        if (debugFont.Characters.Contains(e.Character))
                        {
                            char key = e.Character;
                            if (ks.IsKeyUp(Keys.LeftShift) && ks.IsKeyUp(Keys.RightShift))
                            {
                                if (char.IsLetter(key))
                                    key = char.ToLower(key);
                            }
                            else if (char.IsDigit(key))
                                switch (key)
                                {
                                    case '0':
                                        key = ')';
                                        break;

                                    case '1':
                                        key = '!';
                                        break;

                                    case '2':
                                        key = '@';
                                        break;

                                    case '3':
                                        key = '#';
                                        break;

                                    case '4':
                                        key = '$';
                                        break;

                                    case '5':
                                        key = '%';
                                        break;

                                    case '6':
                                        key = '^';
                                        break;

                                    case '7':
                                        key = '&';
                                        break;

                                    case '8':
                                        key = '*';
                                        break;

                                    case '9':
                                        key = '(';
                                        break;
                                }
                            else
                            {
                                switch (e.Character)
                                {
                                    case '\\':
                                        key = '|';
                                        break;

                                    case ']':
                                        key = '}';
                                        break;

                                    case ',':
                                        key = '<';
                                        break;

                                    case '-':
                                        key = '_';
                                        break;

                                    case '.':
                                        key = '>';
                                        break;

                                    case ';':
                                        key = ':';
                                        break;
                                }
                            }



                            typedMessage += key;
                        }
                        */
                        typedMessage += e.Character;
                    }
                }
                else
                {
                    KeyboardState ks = Keyboard.GetState();

                    if (ks.IsKeyDown(Keys.Enter))
                    {
                        if (passwordSelected) messageReadyToSend = true;
                        passwordSelected = !passwordSelected;
                        return;
                    }
                    if(ks.IsKeyDown(Keys.Tab))
                    {
                        passwordSelected = !passwordSelected;
                        return;
                    }

                    if (ks.IsKeyDown(Keys.Back))
                    {
                        if(passwordSelected && password.Length > 0) password = password.Substring(0, password.Length - 1);
                        else if(!passwordSelected && username.Length > 0) username = username.Substring(0, username.Length - 1);
                        return;
                    }
                    /*
                        if (debugFont.Characters.Contains(e.Character))
                        {
                            char key = e.Character;
                            if (ks.IsKeyUp(Keys.LeftShift) && ks.IsKeyUp(Keys.RightShift))
                            {
                                if (char.IsLetter(key))
                                    key = char.ToLower(key);
                            }
                            else if (char.IsDigit(key))
                                switch (key)
                                {
                                    case '0':
                                        key = ')';
                                        break;

                                    case '1':
                                        key = '!';
                                        break;

                                    case '2':
                                        key = '@';
                                        break;

                                    case '3':
                                        key = '#';
                                        break;

                                    case '4':
                                        key = '$';
                                        break;

                                    case '5':
                                        key = '%';
                                        break;

                                    case '6':
                                        key = '^';
                                        break;

                                    case '7':
                                        key = '&';
                                        break;

                                    case '8':
                                        key = '*';
                                        break;

                                    case '9':
                                        key = '(';
                                        break;
                                }
                            else
                            {
                                switch (e.Character)
                                {
                                    case '\\':
                                        key = '|';
                                        break;

                                    case ']':
                                        key = '}';
                                        break;

                                    case ',':
                                        key = '<';
                                        break;

                                    case '-':
                                        key = '_';
                                        break;

                                    case '.':
                                        key = '>';
                                        break;

                                    case ';':
                                        key = ':';
                                        break;
                                }
                            }
                    
                        if (passwordSelected) password += key;
                        else username += key;
                    }
                    */

                    if (passwordSelected) password += e.Character;
                    else username += e.Character;
                }
            };
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            dylangtechTexture = this.Content.Load<Texture2D>("dylangtech");
            playerTexture = this.Content.Load<Texture2D>("player");
            merchantTexture = this.Content.Load<Texture2D>("merchant");

            Tile.LoadTextures(this.Content, this.GraphicsDevice);

            //debugFont = this.Content.Load<SpriteFont>("font_debug");
            FontSystem fontSystem = new FontSystem(StbTrueTypeSharpFontLoader.Instance, new Texture2DManager(this.GraphicsDevice), 1024, 1024, 0, 0, true);



            string fontPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Content/seguiemj.ttf");
            fontSystem.AddFont(File.ReadAllBytes(fontPath));
            debugFont = fontSystem.GetFont(16);


            defaultRectangle = new Texture2D(GraphicsDevice, 1, 1);

            Color[] data = new Color[1];
            for (int i = 0; i < data.Length; ++i) data[i] = Color.White;
            defaultRectangle.SetData(data);

        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // For Mobile devices, this logic will close the Game when the Back button is pressed
            // Exit() is obsolete on iOS
#if !__IOS__ && !__TVOS__
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                //Exit();
            }
#endif
            if (focusedObjectId.HasValue && !isTyping && Keyboard.GetState().IsKeyDown(Keys.T)) isTyping = true;


            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            if(!focusedObjectId.HasValue || !currentState.Objects.ContainsKey(focusedObjectId.Value))
            {
                GraphicsDevice.SetRenderTarget(scene);
                spriteBatch.Begin();

                string usernameString = $"Username: {username}";
                string passwordString = $"Password: {"".PadLeft(password.Length, '*')}";

                Vector2 usernameSize = debugFont.MeasureString(usernameString).ToXNA();
                Vector2 passwordSize = debugFont.MeasureString(passwordString).ToXNA();

                Vector2 usernameLocation = new Vector2(scene.Width / 2 - usernameSize.X / 2, scene.Height / 2 - usernameSize.Y / 2 - 32);
                Vector2 passwordLocation = new Vector2(scene.Width / 2 - passwordSize.X / 2, scene.Height / 2 - passwordSize.Y / 2 + 32);

                if (!passwordSelected) spriteBatch.Draw(defaultRectangle, new Rectangle(usernameLocation.ToPoint(), usernameSize.ToPoint()), Color.Green);
                else spriteBatch.Draw(defaultRectangle, new Rectangle(passwordLocation.ToPoint(), passwordSize.ToPoint()), Color.Green);

                //spriteBatch.DrawString(debugFont, usernameString, usernameLocation, Color.White);
                //spriteBatch.DrawString(debugFont, passwordString, passwordLocation, Color.White);
                debugFont.DrawText(new Renderer(spriteBatch), usernameString, usernameLocation.ToGeneric(), Color.White.ToGeneric());
                debugFont.DrawText(new Renderer(spriteBatch), passwordString, passwordLocation.ToGeneric(), Color.White.ToGeneric());

                spriteBatch.End();
                GraphicsDevice.SetRenderTarget(null);
            }
            else
            {
                Tile.ChangeTextureHSV(hsv);
                //ConcurrentDictionary<Guid, GameObject> copyO = new ConcurrentDictionary<Guid, GameObject>();
                (double, double) copyP;
                //Chunk[,] copyC;
                lock (currentState.Chunks) lock (currentState.Objects) lock (currentState)
                        {
                            copyP = currentState.Objects[focusedObjectId.Value].Coordinates;


                            GraphicsDevice.SetRenderTarget(scene);
                            spriteBatch.Begin();
                            {
                                //Vector2 localOffset = focusedObjectId.HasValue && objects.ContainsKey(focusedObjectId.Value) ? new Vector2((float)(objects[focusedObjectId.Value].Coordinates.Item1 % (Chunk.SIZE)) * Tile.SIZE, (float)(objects[focusedObjectId.Value].Coordinates.Item2 % (Chunk.SIZE)) * Tile.SIZE) : new Vector2(0f, 0f);
                                Vector2 localOffset = new Vector2(
                                    (float)(copyP.Item1 % (Chunk.SIZE)) * Tile.SIZE,
                                    (float)(copyP.Item2 % (Chunk.SIZE)) * Tile.SIZE);

                                (double, double) focusedObjectCoordinates = copyP;

                                if (currentState.Chunks.Length > 0)
                                {
                                    int chunkRangeX = currentState.Chunks.GetLength(0) / 2 + currentState.Chunks.GetLength(0) % 1;
                                    int chunkRangeY = currentState.Chunks.GetLength(1) / 2 + currentState.Chunks.GetLength(1) % 1;

                                    //Because position can be negative, division will offset negagtive values improperly. This corrects it.
                                    int xSupplement = focusedObjectCoordinates.Item1 <= 0.0 ? -(int)(Chunk.SIZE * Tile.SIZE) : 0;
                                    int ySupplement = focusedObjectCoordinates.Item2 <= 0.0 ? -(int)(Chunk.SIZE * Tile.SIZE) : 0;


                                    for (int yChunk = -chunkRangeX; yChunk <= chunkRangeX; yChunk++)
                                    {
                                        for (int xChunk = -chunkRangeY; xChunk <= chunkRangeY; xChunk++)
                                        {
                                            for (int y = 0; y < Chunk.SIZE; y++)
                                            {
                                                for (int x = 0; x < Chunk.SIZE; x++)
                                                {
                                                    Texture2D textureToUse = currentState.Chunks[xChunk + chunkRangeX, yChunk + chunkRangeY].Tiles[x, y].Texture;

                                                    if(textureToUse != null)
                                                    {
                                                        Rectangle r1 = new Rectangle(new Point((int)(xChunk * Tile.SIZE * (int)Chunk.SIZE + x * Tile.SIZE - localOffset.X) + scene.Width / 2 + xSupplement,
                                                            (int)(yChunk * Tile.SIZE * (int)Chunk.SIZE + y * Tile.SIZE - localOffset.Y) + scene.Height / 2 + ySupplement), new Point((int)Tile.SIZE, (int)Tile.SIZE));


                                                        spriteBatch.Draw(textureToUse, r1, Color.White);
                                                        //spriteBatch.Draw(textureToUse, r1, hsv.ToColor());
                                                    }

                                                }
                                            }
                                        }
                                    }
                                    foreach (GameObject obj in currentState.Objects.Values)
                                    {
                                        Rectangle r1 = new Rectangle(new Point((int)((obj.Coordinates.Item1 - focusedObjectCoordinates.Item1) * (double)Tile.SIZE - (double)(obj.Size.Item1 * Tile.SIZE / 2)) + scene.Width / 2, (int)((obj.Coordinates.Item2 - focusedObjectCoordinates.Item2) * (double)Tile.SIZE - (double)(obj.Size.Item2 * Tile.SIZE / 2)) + scene.Height / 2), new Point((int)(obj.Size.Item1 * Tile.SIZE), (int)(obj.Size.Item2 * Tile.SIZE)));

                                        switch (obj)
                                        {
                                            case Player player:
                                                spriteBatch.Draw(playerTexture, r1, Color.White);
                                                break;
                                            case Merchant merchant:
                                                spriteBatch.Draw(merchantTexture, r1, Color.White);
                                                break;
                                        }
                                    }

                                    foreach (GameEntity ent in currentState.Entities.Values)
                                    {
                                        Rectangle r1 = new Rectangle(new Point((int)((ent.Coordinates.Item1 - focusedObjectCoordinates.Item1) * (double)Tile.SIZE - (double)(ent.Size.Item1 * Tile.SIZE / 2)) + scene.Width / 2, (int)((ent.Coordinates.Item2 - focusedObjectCoordinates.Item2) * (double)Tile.SIZE - (double)(ent.Size.Item2 * Tile.SIZE / 2)) + scene.Height / 2), new Point((int)(ent.Size.Item1 * Tile.SIZE), (int)(ent.Size.Item2 * Tile.SIZE)));

                                        switch (ent)
                                        {
                                            case SlashAttack player:
                                                spriteBatch.Draw(defaultRectangle, r1, Color.Black);
                                                break;
                                        }
                                    }


                                    foreach (GameObject obj in currentState.Objects.Values)
                                    {
                                        string label = $"{obj.Name} [{obj.Level}]";
                                        Vector2 size = debugFont.MeasureString(label).ToXNA();

                                        Rectangle healthBarRect = new Rectangle((int)((obj.Coordinates.Item1 - focusedObjectCoordinates.Item1 - obj.Size.Item1 * 1.25 / 2) * (double)Tile.SIZE + scene.Width / 2), (int)((obj.Coordinates.Item2 - focusedObjectCoordinates.Item2) * (double)Tile.SIZE - (double)(obj.Size.Item2 * Tile.SIZE / 2) - 12 + scene.Height / 2), (int)(obj.Size.Item2 * 1.25 * (double)Tile.SIZE), (int)(12));

                                        spriteBatch.Draw(defaultRectangle, healthBarRect, Color.Gray);

                                        Point shrinkage = new Point(4, 4);
                                        healthBarRect.Location += shrinkage / new Point(2);
                                        healthBarRect.Size -= shrinkage;

                                        spriteBatch.Draw(defaultRectangle, healthBarRect, Color.Red);

                                        healthBarRect.Size = new Point((int)(healthBarRect.Size.X * ((double)obj.Health / (double)GameObject.GetMaxHealth(obj.Level))), healthBarRect.Size.Y);

                                        spriteBatch.Draw(defaultRectangle, healthBarRect, Color.Green);


                                        Rectangle r1 = new Rectangle(new Point((int)((obj.Coordinates.Item1 - focusedObjectCoordinates.Item1) * (double)Tile.SIZE) + scene.Width / 2 - (int)(size.X / 2), (int)((obj.Coordinates.Item2 - focusedObjectCoordinates.Item2) * (double)Tile.SIZE - (double)(obj.Size.Item2 * Tile.SIZE / 2)) + scene.Height / 2 - (int)(size.Y / 2) - (int)(Tile.SIZE / 4)), new Point((int)size.X, (int)size.Y));

                                        //spriteBatch.DrawString(debugFont, label, new Vector2(r1.X, r1.Y - 12), Color.White);
                                        debugFont.DrawText(new Renderer(spriteBatch), label, new Vector2(r1.X, r1.Y - 12).ToGeneric(), Color.White.ToGeneric());

                                    }

                                    string watermark = "Playerdom Test 💻 - Copyright © 2021 Dylan Green";
                                    Vector2 watermarkSize = debugFont.MeasureString(watermark).ToXNA();

                                    //spriteBatch.DrawString(debugFont, watermark, new Vector2(scene.Width - watermarkSize.X, scene.Height - watermarkSize.Y), Color.White);
                                    debugFont.DrawText(new Renderer(spriteBatch), watermark, new Vector2(scene.Width - watermarkSize.X, scene.Height - watermarkSize.Y).ToGeneric(), Color.White.ToGeneric());


                                }
                            }
                        }

                string posString = $"Dimension: {dimensionId}, X: {copyP.Item1:0.000000}, Y: {copyP.Item2:0.000000}";

                //spriteBatch.DrawString(debugFont, posString, new Vector2(0.0f, 0.0f), Color.DarkRed);
                debugFont.DrawText(new Renderer(spriteBatch), posString, new Vector2(0.0f, 0.0f).ToGeneric(), Color.DarkRed.ToGeneric());


                Vector2 position = new Vector2(16, 48);
                foreach (ChatMessage message in messages.ToArray())
                {
                    Color color;
                    string content = $"{message.TimeSent.ToLocalTime().ToShortTimeString()} - {message.Sender}: {message.Content}";
                    Vector2 size = debugFont.MeasureString(content).ToXNA();

                    switch (message.MessageType)
                    {
                        default:
                            color = Color.LightGray;
                            break;
                        case ChatMessageTypes.Server:
                            color = Color.Purple;
                            break;
                        case ChatMessageTypes.Owner:
                            color = Color.Gold;
                            break;
                        case ChatMessageTypes.Admin:
                            color = Color.Red;
                            break;
                        case ChatMessageTypes.Mod:
                            color = Color.Blue;
                            break;
                    }

                    Rectangle location = new Rectangle(position.ToPoint(), size.ToPoint());
                    spriteBatch.Draw(defaultRectangle, location, Color.Black);
                    //spriteBatch.DrawString(debugFont, content, position, color);
                    debugFont.DrawText(new Renderer(spriteBatch), content, position.ToGeneric(), color.ToGeneric());
                    position.Y += location.Height;
                }

                if (isTyping)
                {
                    string content = $">: " + typedMessage;
                    Vector2 size = debugFont.MeasureString(content).ToXNA();

                    spriteBatch.Draw(defaultRectangle, new Rectangle(position.ToPoint(), size.ToPoint()), Color.Black);
                    //spriteBatch.DrawString(debugFont, content, position, Color.White);
                    debugFont.DrawText(new Renderer(spriteBatch), content, position.ToGeneric(), Color.White.ToGeneric());
                }


                spriteBatch.End();
                GraphicsDevice.SetRenderTarget(null);
            }


            float outputAspect = Window.ClientBounds.Width / (float)Window.ClientBounds.Height;
            float preferredAspect = 1920 / (float)1080;

            Rectangle dst;
            if (outputAspect <= preferredAspect)
            {
                // output is taller than it is wider, bars on top/bottom
                int presentHeight = (int)((Window.ClientBounds.Width / preferredAspect) + 0.5f);
                int barHeight = (Window.ClientBounds.Height - presentHeight) / 2;
                dst = new Rectangle(0, barHeight, Window.ClientBounds.Width, presentHeight);
            }
            else
            {
                // output is wider than it is tall, bars left/right
                int presentWidth = (int)((Window.ClientBounds.Height * preferredAspect) + 0.5f);
                int barWidth = (Window.ClientBounds.Width - presentWidth) / 2;
                dst = new Rectangle(barWidth, 0, presentWidth, Window.ClientBounds.Height);
            }

            graphics.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 1.0f, 0);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            spriteBatch.Draw(scene, dst, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        static async void SendInputAsync(PlayerdomGame game)
        {

            try
            {
                while (game._tcpClient.Connected)
                {
                    
                    if(game.messageReadyToSend)
                    {
                        game.messageReadyToSend = false;
                        
                        if(game.focusedObjectId.HasValue)
                        {
                            MessagePackSerializer.Serialize(game._netStream, new ServerPack() { CurrentMessage = new ServerMessage() { MessageType = "chat_area", MessageContent = new string[1] { game.typedMessage } } }, PlayerdomGame.SerializerSettings);
                            game.typedMessage = "";
                        }
                        else
                        {
                            ServerPack pack = new ServerPack() { CurrentMessage = new ServerMessage() { MessageType = "login", MessageContent = new string[2] { game.username, game.password } } };
                            MessagePackSerializer.Serialize(game._netStream, pack, PlayerdomGame.SerializerSettings);
                            game.username = "";
                            game.password = "";
                        }
                    }
                    else if(game.isTyping)
                    {
                        MessagePackSerializer.Serialize(game._netStream, new ServerPack() { KeysPressed = Array.Empty<Keys>() }, PlayerdomGame.SerializerSettings);
                    }
                    else
                    {
                        MessagePackSerializer.Serialize(game._netStream, new ServerPack() { KeysPressed = Keyboard.GetState().GetPressedKeys() }, PlayerdomGame.SerializerSettings);

                    }
                    Task.Delay(30).Wait();
                }
            }
            catch(Exception)
            {
                game.Exit();
                //Connection was lost
            }
        }


        static async void ReceiveOutputAsync(PlayerdomGame game)
        {


            if (!game.token.HasValue) game.token = Guid.NewGuid();

            try
            {
                using (CancellationTokenSource tokenSource = new CancellationTokenSource())
                {
                    while (game._tcpClient.Connected)
                    {
                        ClientPack obj = null;
                        if(game._netStream.DataAvailable)
                        using (var streamReader = new MessagePackStreamReader(game._netStream, true))
                        {
                            ReadOnlySequence<byte>? sequence = await streamReader.ReadAsync(tokenSource.Token);
                                if (sequence.HasValue && streamReader.RemainingBytes.Length == 0)
                                    obj = MessagePackSerializer.Deserialize<ClientPack>(sequence.Value, PlayerdomGame.SerializerSettings);
                        }

                        if (obj != null)
                        {
                            if (obj.CurrentMessage != null)
                            {
                                switch(obj.CurrentMessage.MessageType)
                                {
                                    case "focusedObjectId":
                                        if (Guid.TryParse(obj.CurrentMessage.MessageContent[0], out Guid focusedObjectId))
                                            game.focusedObjectId = focusedObjectId;
                                        break;
                                    case "changeDimension":
                                        if( ushort.TryParse(obj.CurrentMessage.MessageContent[0], out ushort dimensionId) &&
                                            float.TryParse(obj.CurrentMessage.MessageContent[1], out float hue) &&
                                            float.TryParse(obj.CurrentMessage.MessageContent[2], out float sat) &&
                                            float.TryParse(obj.CurrentMessage.MessageContent[3], out float val))
                                        {
                                            game.hsv = (hue, sat, val);
                                            game.dimensionId = dimensionId;
                                        }
                                        break;
                                }
                            }
                            if (obj.Chunks != null && obj.GameObjects != null)
                            {
                                ConcurrentDictionary<Guid, GameObject> gameObjects = obj.GameObjects;
                                ConcurrentDictionary<Guid, GameEntity> gameEntities = obj.GameEntities;


                                lock (game.currentState.Chunks) lock (game.currentState) lock (game.currentState.Objects) lock (game.currentState.Entities)
                                            {
                                                foreach (KeyValuePair<Guid, GameObject> kvp in gameObjects)
                                                {
                                                    if (game.currentState.Objects.ContainsKey(kvp.Key))
                                                    {
                                                        game.currentState.Objects[kvp.Key].UpdateStats(kvp.Value);
                                                    }
                                                    else
                                                    {
                                                        game.currentState.Objects.TryAdd(kvp.Key, kvp.Value);
                                                    }
                                                }
                                                foreach (KeyValuePair<Guid, GameObject> kvp in game.currentState.Objects)
                                                {
                                                    if (!gameObjects.ContainsKey(kvp.Key))
                                                    {
                                                        game.currentState.Objects.TryRemove(kvp.Key, out GameObject value);
                                                    }
                                                }


                                                foreach (KeyValuePair<Guid, GameEntity> kvp in gameEntities)
                                                {
                                                    if (game.currentState.Entities.ContainsKey(kvp.Key))
                                                    {
                                                        game.currentState.Entities[kvp.Key].UpdateStats(kvp.Value);
                                                    }
                                                    else
                                                    {
                                                        game.currentState.Entities.TryAdd(kvp.Key, kvp.Value);
                                                    }
                                                }
                                                foreach (KeyValuePair<Guid, GameEntity> kvp in game.currentState.Entities)
                                                {
                                                    if (!gameEntities.ContainsKey(kvp.Key))
                                                    {
                                                        game.currentState.Entities.TryRemove(kvp.Key, out GameEntity value);
                                                    }
                                                }

                                                game.currentState = new CurrentClientState(obj.Chunks, game.currentState.Objects, game.currentState.Entities);
                                        }
                            }

                            if(obj.NewChats != null)
                            {
                                while(obj.NewChats.Count > 0)
                                {
                                    while (game.messages.Count >= 24) game.messages.TryDequeue(out ChatMessage message);
                                    game.messages.Enqueue(obj.NewChats.Dequeue());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                game.Exit();
                //Connection was lost
            }
        }

        protected override void Dispose(bool disposing)
        {
            _tcpClient.Dispose();
            base.Dispose(disposing);
        }
    }
}
