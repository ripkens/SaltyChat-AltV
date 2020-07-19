# Salty Chat for [Alt:V](https://https://altv.mp/)

An example implementation of Salty Chat for [Alt:V](https://https://altv.mp/).

You can report bugs or make sugguestions via issues, or contribute via pull requests - we appreciate any contribution.  
Join our [Discord](https://discord.gg/MBCnqSf) and start with [Salty Chat](https://www.saltmine.de/)!

# Current supported Plugin Version
- ~~SaltyChat 1.3.3 Stable~~
- SaltyChat 2.0.0 Stable
- SaltyChat 2.0.1 Stable

# Installation

- Add SaltyServer and SaltyShared as Projects to your VS Solution
- Add NuGet Packages as needed
- Copy SaltyClient to your resources folder, add it to your server.cfg and change resource.cfg to reflect the location of your output Folder.

SaltyChat Server will start automatically if your resource.cfg is correct and you will see "=====> Salty Chat Server Started =)" in your server console
 
- Update the "OnResourceStart" function in "VoiceManager.cs" to reflect your Teamspeak settings
 
 ```
this.ServerUniqueIdentifier = "";
this.MinimumPluginVersion = "x.x.x";
this.SoundPack = "default";
this.IngameChannel = IngameChannel = "x";
this.IngameChannelPassword = IngameChannelPassword = "1234";
 ```
 
- add ```Alt.Emit("PlayerLoggedIn", client);``` to your player Spawn function to move the player into the Teamspeak-Channel for Ingame-Voice
 
- The Server will Emit "client::updateVoiceRange" to the Player on VoiceRange change, you can react to it in your HUD for example.

# Supported Ingame Devices

- Ingame Voice with range 0, 3, 8, 15, 32 meters (can be changed)
- Phone functions (default)
- MultiChannel Radio (unlimited Channels)

# Credits
This Repo uses Code extracted from the following authors:

[WhishN v1.3.3](https://github.com/WhishN/)

[SaltMineDE](https://github.com/saltminede)

