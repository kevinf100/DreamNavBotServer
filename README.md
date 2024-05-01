# DreamNav BotServer Edit
Changed Channels to be somewhat more thread safe. Reading the sockets from them is not, but writing is.

Changed many things to include C# features.

Updated many naming.

You no Longer need to ping from the client, the server will ping for you.

This now supports Linux, feel free to try it. I tested and confirm it works in WSL.

```lua
--[REQUIRED] Change BotServer URL
BotServer.url = "ws://127.0.0.1:5000/send"

local playerName = name()
local channel = "1"
BotServer.init(playerName, channel)

BotServer.listen("Test", function(name, mens)
  print("Recived Mens From "..name)
  print("Mens:"..mens)
end)

macro(1000, function()
  print("Sending Message...")
  BotServer.send("Test", "Test Message")
end)
```

Below is the unchanged README without the lua.

# DreamNav BotServer
Alternative OTCV8 BotServer

Made by Discord: VivoDibra#1182

Oficial Discord Server: https://discord.gg/RkQ9nyPMBH

Oficial Youtube Channel: https://www.youtube.com/@vivodibra/videos

You Can Download the modified BotServer.lua in the Discord Server (work like the original BotServer) !!!

You can download the BotServer here: https://github.com/GabrielPiM/DreamNavBotServer/releases

[ENG] THIS PROGRAM IS FREE AND CAN NOT BE SOLD !!

[BR] ESTE PROGRAMA É GRATIS E NÃO PODE SER VENDIDO !!

Usage Example (OTCV8 .lua script):

[ENG] Liked it? consider buying a script!

[BR] Gostou? considere comprar um script!
