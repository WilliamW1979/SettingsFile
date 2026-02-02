# Settings File Wrapper

This is a Settings File Wrapper that is free to use. All I ask is that you give me credit.

The way this wrapper works is that settings are saved in a specific format so that they can easily be read.

# Format
```
[Category 1]
Setting name: Setting Value
Setting name2: Setting Value2

[Category 2]
Setting name: Setting Value
Setting name2: Setting Value2
```

The wrapper automatically will read and write to the settings file for you and organize it based on your given categories. So all you need to do is tell it what belongs where. If you change the settings file outside of the 

The DLL that compiles is usable in DEBUG and RELEASE modes.

## Disclaimor
DLLs are VERY DANGEROUS to download. I will always recommend that you download the source code and compile it yourself so that you know for sure you are getting a clean DLL that someone didn't tamper with. A lot of web sites that will allow you to download free games to play that usually cost money, they will sometimes try to add code to the DLLs to hide their true intentions on giving you the game for free. Methods like these are very common and Microsoft Defender / Virus Protections generally won't catch them until they are well known. Injecting bad code into DLLs is actually very easy to do and something you should be aware that people do. That is why I will always recommend that you NEVER use a DLL straight from the internet unless it comes from a source you trust 100%. With that said, I did post the DLL that compiled with the very code you see here in this github. If you follow my instructions and compile your own, you will see that your DLL will be identical to mine. That is how you will know mine is clean. Any added code will add to the size and change the CHECKSUM of the file. Use these methods in the future to tell if people are being honest with you. It is always better to be safe then sorry!

It can pull any type of data, but using get you will need to put in a default so that it knows what type to pull.
In the event that someone manually adds configurations or changes, it will skip over any line that is determined to be a bad line. So missing category markers or wrong types will not crash it. It just will return false with the Try or the default with the get.

# Example Code
```
using SettingsFile;

SettingsFile settings = new("config.ini");
settings.Set("Server", "Host", "localhost");
settings.Set("Server", "Port", "80");
settings.Flush();

// Using Get
string host = settings.Get("Server", "Host", "127.0.0.1");
uint port = settings.Get("Server", "Port", 8080u)

//Using TryGet
if(!settings.TryGet("Server", "Host", out string host)
{
    // Setting not found
    // Code in here to handle the issue, like setting host to "" or returning out of the function
}
if (!setting.TryGet("Server", "Port", out uint port)
{
    // Setting not found
    // Code in here to handle the issue, like setting host to "" or returning out of the function
}
Console.WriteLine("Host is " + host);
Console.WriteLine("Port is " + port);
```
