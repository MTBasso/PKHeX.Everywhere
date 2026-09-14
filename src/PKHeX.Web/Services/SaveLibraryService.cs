using Microsoft.JSInterop;
using PKHeX.Facade;

namespace PKHeX.Web.Services;

/// <summary>
/// A folder on the user's machine, linked once, holding however many saves. This service is the .NET
/// side of it; the browser work lives in <c>_js/lib/files/saveLibrary.ts</c>.
///
/// Read-only by design for now. Writing back to a real save is the destructive half of this feature and
/// waits on decisions the Save Library model has not settled: backups, and what to do when an emulator
/// changes a file while it is open here.
/// </summary>
public class SaveLibraryService(IJSRuntime js)
{
    /// Mirrors the FolderState returned by the JS module. Permission is the raw Permissions API state -
    /// "granted", "prompt", "denied" - or "unsupported".
    public record FolderState(string Name, string Permission);

    public record ScannedFile(string Id, string Name, string Path, long Size, long LastModified);

    public record LibraryEntry(ScannedFile File, SaveSummary Summary);

    /// Status is "ok", "cancelled", "unsupported" or "failed". A folder the browser refuses outright and
    /// a folder the user simply cancelled both arrive as "cancelled" - the picker dialog gives the page
    /// no way to tell them apart.
    public record PickResult(string Status, FolderState? Folder, string Message);

    public ValueTask<bool> IsSupported() =>
        js.InvokeAsync<bool>("isSaveLibrarySupported");

    /// Must be called from a user gesture - the picker requires transient activation.
    public ValueTask<PickResult> PickFolder() =>
        js.InvokeAsync<PickResult>("pickSaveFolder");

    public ValueTask<FolderState?> RestoreFolder() =>
        js.InvokeAsync<FolderState?>("restoreSaveFolder");

    /// Also gesture-bound. A handle restored from IndexedDB normally reports "prompt", so this is the
    /// one click the user has to make on a later visit.
    public ValueTask<string> RequestPermission() =>
        js.InvokeAsync<string>("requestSaveFolderPermission");

    public ValueTask ForgetFolder() =>
        js.InvokeVoidAsync("forgetSaveFolder");

    public ValueTask<byte[]?> ReadFile(string id) =>
        js.InvokeAsync<byte[]?>("readSaveFile", id);

    /// <summary>
    /// Walks the linked folder and returns only what actually parses as a save.
    ///
    /// The order matters for cost: the walk itself reads no file contents, size rules out nearly
    /// everything in a normal folder, and only the survivors are read and probed.
    /// </summary>
    public async Task<IReadOnlyList<LibraryEntry>> Scan(int maxDepth = 4)
    {
        var files = await js.InvokeAsync<ScannedFile[]>("scanSaveFolder", maxDepth);
        var entries = new List<LibraryEntry>();

        foreach (var file in files)
        {
            if (!SaveProbe.IsPlausibleSaveSize(file.Size)) continue;

            var bytes = await ReadFile(file.Id);
            if (bytes is null) continue;

            if (!SaveProbe.TryProbe(bytes, file.Name, out var summary) || summary is null) continue;

            entries.Add(new LibraryEntry(file, summary));
        }

        return entries;
    }
}
