using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Feuerzeug.Models;

public class Config
{
    static readonly Size defaultWindowSize = new(1320, 860);

    static readonly string PATH = Path.Combine("data", "config.json");

    public string downloadUrl = "https://nockal.com/download/feuerzeug";

    public Config_RemoteMachine remoteMachine = new();

    Config() { }

    static Config instance;
    public static Config Instance => instance ??= Load();

    static Config Load()
    {
        if (!File.Exists(PATH))
            return new();

        var json = File.ReadAllText(PATH);
        var config = json.FromJson<Config>();

        config.downloadUrl = config.downloadUrl
            .Replace("chvaco.at", "nockal.com")
            .Replace("steinalt.online", "nockal.com");

        return config;
    }

    public void Save()
    {
        var json = this.ToJson();
        Directory.CreateDirectory("data");
        File.WriteAllText(PATH, json);
    }

    string GetDownloadFileUrl(string file)
    {
        return $"{downloadUrl.TrimEnd('/')}/{file}";
    }

    public async Task<string> DownloadFileContent(string file)
    {
        var url = GetDownloadFileUrl(file);
        using var httpClient = new HttpClient();
        return await httpClient.GetStringAsync(url);
    }

    public async Task<Stream> DownloadFileStream(string file)
    {
        var url = GetDownloadFileUrl(file);
        using var httpClient = new HttpClient();
        return await httpClient.GetStreamAsync(url);
    }

    public async Task<bool> DownloadFile(string file)
    {
        var url = GetDownloadFileUrl(file);
        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return false;

        Directory.CreateDirectory("files");
        var path = Path.Combine("files", file);

        using var fileStream = new FileStream(path, FileMode.Create);

        await response.Content.CopyToAsync(fileStream);

        return true;
    }

    public Rectangle bounds;
    public bool maximized;

    public void ResetBounds()
    {
        bounds = default;
        maximized = false;

        SetupBounds();
    }

    public void SetupBounds()
    {
        if (bounds.Size.IsEmpty)
            bounds.Size = defaultWindowSize;
    }
}
