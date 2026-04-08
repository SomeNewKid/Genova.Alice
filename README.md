# Genova.Alice

A .NET 8 implementation of the classic ALICE AIML chatbot, with console and automated chat runners.

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

## License

GNU General Public License v3.0
