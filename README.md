# Forked SysBot for Pokemon Games on the Nintendo Switch  -

This is a customized version of Berichans forked Sysbot. This Sysbot is intended for use within the Xieon's Gaming Corner ecosystem.  -
If you're simply looking to obtain Pokemon feel free to join the Discord community, or check out my streams on Youtube and Twitch.

## Project is intended to be compatible with following Pokemon Games/DLC

- BDSP
- Sword/Shield
- Legends Arceus
- Scarlet/Violet including The Teal Mask dlc.

## If using this repository please note

- It is personalized for "Xieon Gaming".
- The code currently utilizes System.IO for local file manipulation. File path's in the code will need to be changed to reflect the correct file locations on the local system. For example the pathing of embed images points to a local system file location.
- This is my personal development build repository and is a work in progress.

## XIEON GAMING

## Xieon's Gaming Corner Discord -

[Join the Xieon's Gaming Corner Discord Community](http://xieon.co)

## Twitch Streams -

I am often streaming my implementation of this project live on Twitch.
[Xieon Gaming on Twitch](http://twitch.xieon.co)

## Youtube Streams -

Also live stream project implementation on Youbtube.
[Xieon Gaming on Youtube](http://yt.xieon.co)

![License](https://img.shields.io/badge/License-AGPLv3-blue.svg)

If you have never used a sysbot before please see the guide first. [Read the official startup instructions](https://github.com/kwsch/SysBot.NET/wiki/Bot-Startup-Details)

## PKHex

This project utilizes PKHex, however, the developers of the PKHex Program do not support, nor are they involved with this project or the one it was forked from - Please do not bother those developers, and take up their time which is much more valuable than mine. Please feel free to pester me in my Discord first if there are issues. Berichan is not involved with or supporting my project, but offers support as well listed below.

## Prerequisities to Sysbot use

- This project requires you to have the appropiate hardware and modifications made to utillze it.
- You will need to have a Nintendo Switch that has CFW and capable of running the sysbotbase.

## Berichan Code's Support Discord

If you use code or portions of Berichan's modified coding you can seek support from them in their server
[Berichan Discord](https://discord.gg/berichan)

## SysBot.Base

- Base logic library to be built upon in game-specific projects.
- Contains a synchronous and asynchronous Bot connection class to interact with sys-botbase.
[sys-botbase](https://github.com/olliz0r/sys-botbase) client for remote control automation of Nintendo Switch consoles.

## SysBot.Tests

- Unit Tests for ensuring logic behaves as intended :)

## Example Implementations

- The driving force to develop this project is automated bots for Nintendo Switch Pokémon games. An example implementation is provided in this repo to demonstrate interesting tasks this framework is capable of performing.
- Refer to the [Wiki](https://github.com/kwsch/SysBot.NET/wiki) for more details on the supported Pokémon features.

## SysBot.Pokemon

- Class library using SysBot.Base to contain logic related to creating & running Pokémon bots.

## SysBot.Pokemon.WinForms

- Simple GUI Launcher for adding, starting, and stopping Pokémon bots (as described above).
- Configuration of program settings is performed in-app and is saved as a local json file.

## SysBot.Pokemon.Discord

- Discord interface for remotely interacting with the WinForms GUI.
- Provide a discord login token and the Roles that are allowed to interact with your bots.
- Commands are provided to manage & join the distribution queue.

## SysBot.Pokemon.Twitch

- Twitch.tv interface for remotely announcing when the distribution starts.
- Provide a Twitch login token, username, and channel for login.

## SysBot.Pokemon.YouTube

- YouTube.com interface for remotely announcing when the distribution starts.
- Provide a YouTube login ClientID, ClientSecret, and ChannelID for login.

## Uses

- [Discord.Net](https://github.com/discord-net/Discord.Net)
- [TwitchLib](https://github.com/TwitchLib/TwitchLib)
- [StreamingClientLibary](https://github.com/SaviorXTanren/StreamingClientLibrary) as a dependency via Nuget.

## Other Dependencies

- Pokémon API logic is   provided by [PKHeX](https://github.com/kwsch/PKHeX/)
- Template generation is provided by [AutoMod](https://github.com/architdate/PKHeX-Plugins/).

## License

- Refer to the `License.md` for details regarding licensing.
- Note to Xieon - Cleaned up Markdown Langugage for test push