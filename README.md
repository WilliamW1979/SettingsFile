# Settings File Wrapper

SettingsFile is a C# DLL for managing INI-style configuration files with optional encryption and type conversion. Works in both Debug and Release modes. Requires .NET 10+.

## Format
```SettingFile
[Server]
Host: localhost
Port: 80

[Logs]
Filename: logs.txt
```

The wrapper can pull any type of data, but when using `Get`, you must provide a default value so it knows what type to pull. If someone manually edits the settings file with invalid lines, it will skip those lines and return the default or false in a TryGet.

## Example Usage
```csharp
SettingsFile settings = new("config.ini"); 
settings.Set("Server", "Host", "localhost");
settings.Set("Server", "Port", "80");
settings.Flush();

string host = settings.Get("Server", "Host", "127.0.0.1");
uint port = settings.Get("Server", "Port", 8080u);

if(!settings.TryGet("Server", "Host", out string host)) { /* handle missing */ }
if(!settings.TryGet("Server", "Port", out uint port)) { /* handle missing */ }

Console.WriteLine("Host is " + host);
Console.WriteLine("Port is " + port);
```

License / Credit: Free to use for personal or commercial projects. Please credit William Ward if used. No warranty is provided.
