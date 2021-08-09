# Petcord Bot
This is a C# Discord bot made with the [Discord.Net](https://github.com/discord-net/Discord.Net/) API wrapper.  
This bot was custom made for the [Petcord Discord server](https://discord.gg/petcord).  
The bot's main functionality is interfacing with [a Google sheet](https://docs.google.com/spreadsheets/d/1vnkD2YENDRVTLzv-CCzrfYmSUp47Xsgkg9E8CHhlCrc/edit?usp=sharing) <sub><sup>(note: this is not their actual sheet, just a copy of it that was made at the time of this repo creation)</sup></sub> and adding the ability to directly and easily use/update the sheet via commands in Discords.

Example of adding a player and their pets the Google sheet, and then displaying them:  
![demonstration](https://i.imgur.com/HoHaJm4.gif)

## Building/Running
Building/running this should only be done for educational purposes, since you wont be able to actually use this yourself. You'd need to have access to the Google sheet.
#### Via the command line
* Install [.NET & .NET Core](https://dotnet.microsoft.com/download)
* Clone this repository with  
`git clone https://github.com/0x464e/petcord-bot`
* Setup the [`config.json`](https://github.com/0x464e/petcord-bot/blob/master/Petcord/config.json)
* Navigate yourself to the folder with the `.csproj` file  
`cd Petcord/Petcord`
* Run `dotnet run` to run the program  
or `dotnet build` to build the program
#### With Visual Studio
* Clone this repository with  
`git clone https://github.com/0x464e/petcord-bot`
* Open the solution (`.sln`) with Visual Studio
* Hit run/debug or build from the Build menu

## Deploying
See [the documentation](https://docs.microsoft.com/en-us/dotnet/core/deploying/) for deploying .NET Core apps.  
Quick example for deploying a self contained installation:
* Install [.NET & .NET Core](https://dotnet.microsoft.com/download)
* Clone this repository with  
`git clone https://github.com/0x464e/petcord-bot`
* Run `dotnet publish -c Release -r RID` where RID is your desired [runtime identifier](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)  
e.g.  
`dotnet publish -c Release -r linux-x64`  
`dotnet publish -c Release -r win-x64`  
* `chmod +x` the binary if needed and ensure the [`config.json`](https://github.com/0x464e/petcord-bot/blob/master/Petcord/config.json) file is found in the same directory as the binary