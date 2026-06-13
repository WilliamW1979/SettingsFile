# SettingsFile

A lightweight, production-ready C# library for reading and writing INI-style configuration files in .NET 10+ applications. Supports typed value retrieval, safe fallback defaults, optional encryption, and graceful handling of malformed input — with zero external dependencies.

---

## Features

- **INI-style format** with `[Section]` / `Key: Value` structure
- **Typed `Get<T>`** — retrieve values as `string`, `int`, `uint`, `bool`, or any supported type with a single call
- **Safe `TryGet<T>`** — pattern-match style retrieval that never throws on missing or invalid keys
- **Default values** — always get a usable value even when a key is absent
- **Malformed line tolerance** — invalid lines are silently skipped rather than crashing
- **Optional encryption** via a pluggable `IEncryptor` interface
- **Flush to disk** with `Flush()` to persist changes on demand
- **No external dependencies** — pure .NET 10, no NuGet packages required

---

## Requirements

- .NET 10 or later

---

## Installation

Clone or download the repository and add the `SettingsFile` project as a reference, or build it as a DLL.

```bash
git clone https://github.com/WilliamW1979/SettingsFile.git
```

---

## File Format

```ini
[Server]
Host: localhost
Port: 8080

[Logging]
Filename: logs.txt
Verbose: true
```

Sections are declared with `[SectionName]`. Key-value pairs use `Key: Value` syntax. Any line that does not conform to this format is silently ignored.

---

## Quick Start

```csharp
var settings = new SettingsFile("config.ini");

// Write values
settings.Set("Server", "Host", "localhost");
settings.Set("Server", "Port", "8080");
settings.Flush();  // Persist to disk

// Read values with typed defaults
string host = settings.Get("Server", "Host", "127.0.0.1");
uint   port = settings.Get("Server", "Port", 80u);
bool verbose = settings.Get("Logging", "Verbose", false);

Console.WriteLine($"Connecting to {host}:{port}");
```

---

## Safe Retrieval with TryGet

Use `TryGet` when you want to handle missing keys explicitly without exceptions:

```csharp
if (settings.TryGet("Server", "Host", out string host))
{
    Console.WriteLine($"Host: {host}");
}
else
{
    Console.WriteLine("Host not configured — using default");
}

if (settings.TryGet("Server", "Port", out uint port))
{
    StartServer(port);
}
```

---

## Optional Encryption

Pass any `IEncryptor` implementation to encrypt the settings file at rest:

```csharp
IEncryptor encryptor = new MyAesEncryptor(key, iv);
var settings = new SettingsFile("config.ini", encryptor);

settings.Set("Database", "Password", "s3cr3t");
settings.Flush();  // Written to disk encrypted
```

---

## Design Notes

- The typed `Get<T>` and `TryGet<T>` pattern eliminates repetitive parsing code in application startup.
- Malformed lines are skipped gracefully, making the library safe to use with hand-edited config files.
- The `IEncryptor` interface keeps the library unopinionated about encryption — any algorithm works.
- `Flush()` gives explicit control over when changes are written, preventing partial writes.

---

## License

Free to use for personal or commercial projects. Please credit **William Ward** if used. No warranty is provided. See [LICENSE.txt](LICENSE.txt) for full terms.

---

## Author

**William Ward** — [github.com/WilliamW1979](https://github.com/WilliamW1979)
