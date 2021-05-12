Playerdom
============
[![GitHub Stars](https://img.shields.io/github/stars/DylanGTech/Playerdom2.svg)](https://github.com/DylanGTech/Playerdom2/stargazers) [![GitHub Issues](https://img.shields.io/github/issues/DylanGTech/Playerdom2.svg)](https://github.com/DylanGTech/Playerdom2/issues) [![Current Version](https://img.shields.io/badge/version-N/A-green.svg)](https://github.com/DylanGTech/Playerdom2)

Playerdom is an open-world infinite procedurally-generating multidimensional environment, which is evolving into a fully-fledged multiplayer video game not unlike a top-down MMORPG. The game is based on the MonoGame engine and is written in .NET 6.

![Game Preview](https://i.imgur.com/w9Hbmmy.png)

---
## Buy me a coffee

If you like this project, want to support creators, or just enjoy helping a fella out, feel free to buy me a pizza! This will free up more of my time to work on open-source projects such as this :)

<a href="https://www.buymeacoffee.com/dylangtech" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: auto !important;width: auto !important;" ></a>

---

## Features
- Multiplayer support
- 65536 unique "infinite" tile-based dimensions per world. All Generated with a single seed
- Location-aware in-game chat
- Login system
- Command framework
- Attacks and health
- Efficient collision-handling

---

## Setup and Usage
Clone this repo to your desktop, go to its root directory and run `dotnet restore` to install its dependencies.

Once the dependencies are installed, you can run  `dotnet run --project Playerdom.Server\Playerdom.Server.csproj` to start the server. Then, in another terminal, open the root directory and run `dotnet run --project Playerdom.OpenGL\Playerdom.OpenGL.csproj`. This will run the client and connect to the server IP defined in the `Connection.txt`, and if successful, present a login screen. Type and username that has not been taken, and a password you intend to reuse.

Movement is WASD as with most video games. Z will use a short-ranged meelee attack. T will open the chat.

---

## License
>You can check out the full license [here](https://github.com/DylanGTech/Playerdom2/blob/master/LICENSE)

This project is licensed under the terms of the **Apache** license.
