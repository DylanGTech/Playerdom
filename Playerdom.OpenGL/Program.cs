﻿using Microsoft.Xna.Framework;
using Playerdom.Shared;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace Playerdom.OpenGL
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            IPEndPoint serverEndpoint = null;


            try
            {
                string connection = File.ReadAllText("Connection.txt");
                if(IPEndPoint.TryParse(connection, out IPEndPoint result))
                {
                    if (result != null) serverEndpoint = result;
                }
               
            }
            catch(Exception)
            {

            }


            using (var game = new PlayerdomGame(serverEndpoint))
            {
                game.Window.AllowUserResizing = true;

                game.Exiting += (object sender, EventArgs e) =>
                {
                    game.Dispose();
                };

                game.Run();
            }
        }
    }
}
