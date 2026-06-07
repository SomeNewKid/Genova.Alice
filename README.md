# Genova.Alice

A .NET 8 implementation of the classic ALICE AIML chatbot, with console and automated chat runners.

> [!WARNING]
> This is an experimental project and should not be considered production-ready. It exists to explore a small AI, ML, agent, or demo idea within the broader Genova ecosystem.

> [!IMPORTANT]
> A fresh public clone of this repository should not be expected to restore or build without additional Genova infrastructure. Many Genova dependencies are distributed through a private authenticated NuGet feed, and the public source does not include feed credentials or a complete public package graph.

## Installation

```bash
dotnet restore
dotnet build
```

## Usage

Run the console chatbot:

```bash
dotnet run --project Alice.Console
```

Run the automated chat app:

```bash
dotnet run --project Alice.Chatting
```

Use the core library from code:

```csharp
var alice = new Genova.Alice.Alice();
var reply = alice.GetResponse("Hello");
```

## Features

* Loads embedded AIML and bot property resources
* Generates single-turn chatbot responses through a simple `Alice` API
* Includes a console interface for interactive chatting
* Includes an automated chat runner that writes a transcript to a file

## Notes

* `Alice.Chatting` requires the `OPENAI_API_KEY` environment variable.
* `Alice.Chatting` also expects `appsettings.json` to define `OpenAI:TextModel` and `OutputDirectory`.

## Thanks

* ALICE / AIML
* OpenAI API

## Third-Party Notices

This project has direct runtime dependencies on third-party NuGet packages, including `Microsoft.Extensions.*` packages (MIT). See each package's NuGet license metadata for full license and notice terms.

## License

GNU General Public License v3.0. See the `LICENSE` file for details.
