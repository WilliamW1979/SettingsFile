using System.Collections.Concurrent;
using System.Globalization;

public interface IEncryptor
{
    string Encrypt(string Value);
    string Decrypt(string Value);
}

public sealed class SettingsFile : IDisposable
{
    private readonly ConcurrentQueue<(string Category, string Setting, string Value, bool IsRemove, bool IsCategoryRemove)> WriteQueue = new();
    private readonly SemaphoreSlim FileSemaphore = new(1, 1);
    private readonly CancellationTokenSource Cts = new();
    private readonly Dictionary<string, Dictionary<string, string>> Categories = new();
    private readonly string FilePath;
    private readonly IEncryptor? Encryptor;
    private readonly Task WorkerTask;

    public SettingsFile(string Path, IEncryptor? EncryptionProvider = null)
    {
        FilePath = Path;
        Encryptor = EncryptionProvider;
        LoadAsync().GetAwaiter().GetResult();
        WorkerTask = Task.Run(ProcessQueueAsync);
    }

    public string Get(string Category, string Setting, string DefaultValue = "")
    {
        FileSemaphore.Wait();
        try
        {
            if (Categories.TryGetValue(Category, out Dictionary<string, string>? dict) && dict.TryGetValue(Setting, out string? value))
                return value;
            return DefaultValue;
        }
        finally { FileSemaphore.Release(); }
    }

    public T Get<T>(string Category, string Setting, T DefaultValue)
    {
        string value = Get(Category, Setting, "");
        if (string.IsNullOrEmpty(value)) return DefaultValue;
        try
        {
            Type type = typeof(T);
            if (type == typeof(TimeSpan))
                return (T)(object)TimeSpan.Parse(value, CultureInfo.InvariantCulture);
            return (T)Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }
        catch { return DefaultValue; }
    }

    public bool TryGet<T>(string Category, string Setting, out T Value)
    {
        Value = default!;
        string raw;
        FileSemaphore.Wait();
        try
        {
            if (!Categories.TryGetValue(Category, out Dictionary<string, string>? dict) || !dict.TryGetValue(Setting, out raw!))
                return false;
        }
        finally { FileSemaphore.Release(); }
        try
        {
            Type type = typeof(T);
            if (type == typeof(TimeSpan))
            {
                Value = (T)(object)TimeSpan.Parse(raw, CultureInfo.InvariantCulture);
                return true;
            }
            Value = (T)Convert.ChangeType(raw, type, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            Value = default!;
            return false;
        }
    }


    public void Set(string Category, string Setting, string Value) => WriteQueue.Enqueue((Category, Setting, Value, false, false));
    public void Set<T>(string Category, string Setting, T Value) => Set(Category, Setting, Convert.ToString(Value, CultureInfo.InvariantCulture) ?? "");

    public bool RemoveSetting(string Category, string Setting)
    {
        WriteQueue.Enqueue((Category, Setting, "", true, false));
        return true;
    }

    public bool RemoveCategory(string Category)
    {
        WriteQueue.Enqueue((Category, "", "", false, true));
        return true;
    }

    public IEnumerable<string> GetCategories()
    {
        FileSemaphore.Wait();
        try { return new List<string>(Categories.Keys); }
        finally { FileSemaphore.Release(); }
    }

    public IEnumerable<string> GetSettings(string Category)
    {
        FileSemaphore.Wait();
        try
        {
            if (Categories.TryGetValue(Category, out Dictionary<string, string>? dict))
                return new List<string>(dict.Keys);
            return Array.Empty<string>();
        }
        finally { FileSemaphore.Release(); }
    }

    public Task SetAsync(string Category, string Setting, string Value) { Set(Category, Setting, Value); return Task.CompletedTask; }
    public Task SetAsync<T>(string Category, string Setting, T Value) { Set(Category, Setting, Value); return Task.CompletedTask; }
    public Task RemoveSettingAsync(string Category, string Setting) { RemoveSetting(Category, Setting); return Task.CompletedTask; }
    public Task RemoveCategoryAsync(string Category) { RemoveCategory(Category); return Task.CompletedTask; }

    public async Task FlushAsync()
    {
        while (!WriteQueue.IsEmpty)
            await Task.Delay(5);
        await FileSemaphore.WaitAsync();
        FileSemaphore.Release();
    }

    public void Flush()
    {
        while (!WriteQueue.IsEmpty)
            Thread.Sleep(5);
        FileSemaphore.Wait();
        FileSemaphore.Release();
    }

    private async Task LoadAsync()
    {
        await FileSemaphore.WaitAsync();
        try
        {
            Categories.Clear();
            if (!File.Exists(FilePath)) return;
            string content;
            try
            {
                content = await File.ReadAllTextAsync(FilePath);
                if (Encryptor != null)
                    content = Encryptor.Decrypt(content);
            }
            catch { content = ""; }
            Parse(content);
        }
        finally { FileSemaphore.Release(); }
    }

    private void Parse(string Data)
    {
        string category = "";
        string[] lines = Data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                category = line[1..^1].Trim();
                if (!Categories.ContainsKey(category))
                    Categories[category] = new Dictionary<string, string>();
                continue;
            }
            int idx = line.IndexOf(':');
            if (idx < 1 || idx == line.Length - 1) continue;
            string key = line[..idx].Trim();
            string value = line[(idx + 1)..].Trim();
            if (!Categories.ContainsKey(category))
                Categories[category] = new Dictionary<string, string>();
            Categories[category][key] = value;
        }
    }

    private string Serialize()
    {
        List<string> lines = new();
        foreach (KeyValuePair<string, Dictionary<string, string>> cat in Categories)
        {
            lines.Add($"[{cat.Key}]");
            foreach (KeyValuePair<string, string> kv in cat.Value)
                lines.Add($"{kv.Key}: {kv.Value}");
            lines.Add("");
        }
        return string.Join(Environment.NewLine, lines);
    }

    private async Task ProcessQueueAsync()
    {
        while (!Cts.IsCancellationRequested)
        {
            List<(string Category, string Setting, string Value, bool IsRemove, bool IsCategoryRemove)> batch = new();
            while (WriteQueue.TryDequeue(out var item))
                batch.Add(item);
            if (batch.Count == 0)
            {
                await Task.Delay(10);
                continue;
            }
            await FileSemaphore.WaitAsync();
            try
            {
                foreach (var item in batch)
                    if (item.IsCategoryRemove)
                        Categories.Remove(item.Category);
                    else if (item.IsRemove)
                    {
                        if (Categories.TryGetValue(item.Category, out Dictionary<string, string>? dict))
                            dict.Remove(item.Setting);
                    }
                    else
                    {
                        if (!Categories.TryGetValue(item.Category, out Dictionary<string, string>? dict))
                        {
                            dict = new Dictionary<string, string>();
                            Categories[item.Category] = dict;
                        }
                        dict[item.Setting] = item.Value;
                    }
                string content = Serialize();
                if (Encryptor != null)
                    content = Encryptor.Encrypt(content);
                await File.WriteAllTextAsync(FilePath, content);
            }
            catch { }
            finally { FileSemaphore.Release(); }
        }
    }

    public void Dispose()
    {
        Cts.Cancel();
        Flush();
        WorkerTask.Wait();
        FileSemaphore.Dispose();
        Cts.Dispose();
    }
}