# Usage

Go to the [releases](https://github.com/edamamet/INFR3830-Practical-Midterm/releases) page and download both the server and client zip files, then unpack both.

Navigate to the server and launch `Hub.Client.exe`, followed by the game build `Game.exe`

## Controls
After inputting your IP (or selecting loopback), you can use WASD to move around, and Tab to toggle chat mode. You can send messages by using the input field in the bottom left

# Installation
> [!IMPORTANT]  
> This is not required to run the server/client. Only if you want to build and run from source

You will need .NET 9.0 and .NET 4.8, as well as Unity 6000.0.23f1.

Clone the repo:
```bash
git clone https://github.com/edamamet/INFR3830-Practical-Midterm midterm
cd midterm
```

Build the server:
```bash
cd Hub
dotnet build
```

Copy the Hub.Hooks.dll that was built in the previous step to the Unity Assets folder:
```bash
cd .. # you should be in the midterm folder
cp Hub/Hub.Hooks/bin/Debug/Hub.Hooks.dll Game/Assets/Hub.Hooks.dll
```

You can now open the Unity project
