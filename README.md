
# AntiTimeOutService
[![Build status](https://ci.appveyor.com/api/projects/status/f1ai7s4yduxhwkcr/branch/dev?svg=true)](https://ci.appveyor.com/project/rashlight/antitimeoutservice/branch/dev)

A Windows service for monitoring network traffic that strives for usability and speed.

The latest download is available [here](https://github.com/rashlight/AntiTimeOutService/releases/latest)

## Usage
For guides on how and why to use this tool, check the (WIP) [wiki](https://github.com/rashlight/AntiTimeOutService/wiki)

## How to build

Open the solution file in Visual Studio (2019 or more is recommended) and build from there.

Or, using CLI:

    msbuild /path/to/AntiTimeOutService.sln

Note that the binary is a Window Service, which need addtional means to add this into your SCM.

## Attribution
This program is licensed under licensed by [Apache License 2.0](https://github.com/rashlight/AntiTimeOutService/blob/main/LICENSE)
