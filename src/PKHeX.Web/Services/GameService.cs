using PKHeX.Facade;
using PKHeX.Facade.Repositories;

namespace PKHeX.Web.Services;

public class GameService(
    AnalyticsService analytics,
    JsService js)
{
    public Game? Game { get; private set; }
    public Game LoadedGame => Game ?? throw new NullReferenceException("Expected game to be loaded, but it was null.");
    public string? FileName { get; private set; }
    
    public bool IsLoaded => Game != null;

    public event EventHandler? OnGameLoaded;

    public void Load(byte[] bytes, string fileName)
    {
        Game = Game.LoadFrom(bytes, fileName);
        
        FileName = string.IsNullOrWhiteSpace(fileName)
            ? FileName
            : fileName;

        OnGameLoaded?.Invoke(this, EventArgs.Empty);

        analytics.TrackGameLoaded(Game);
    }

    public void LoadBlank(GameVersionDefinition version)
    {
        Game = Game.EmptyOf(version);
        FileName = version.Name;
        
        OnGameLoaded?.Invoke(this, EventArgs.Empty);
        
        analytics.TrackGameLoaded(Game);
    }

    public Stream Export()
    {
        ArgumentNullException.ThrowIfNull(Game, nameof(Game));

        var bytes = Game.ToByteArray();
        return new MemoryStream(bytes);
    }

    /// <summary>
    /// Saving hands the user a fresh copy and never writes over anything. The original save - wherever it
    /// came from, a linked folder or a file picker - is left exactly as it was found.
    /// </summary>
    public async Task ExportToDownload()
    {
        ArgumentNullException.ThrowIfNull(Game, nameof(Game));

        await js.DownloadFile(Export(), FileName ?? string.Empty);

        analytics.TrackGameExported(Game);
    }
}