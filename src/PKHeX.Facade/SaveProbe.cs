using PKHeX.Core;
using PKHeX.Facade.Repositories;

namespace PKHeX.Facade;

/// <summary>
/// What a scanned file looks like before anyone opens it: enough to list and choose between saves,
/// and nothing that costs a full decode to produce.
/// </summary>
public sealed record SaveSummary(
    string TrainerName,
    uint TrainerId,
    string GameName,
    string Context,
    string PlayTime,
    int Language,
    int BoxCount,
    int PartyCount,
    bool ChecksumsValid);

/// <summary>
/// A read-only look at a candidate file, for scanning a folder full of things that may or may not be
/// saves.
///
/// This exists because <see cref="Game.LoadFrom(byte[], string?)"/> is the wrong tool for the job: it
/// builds every repository and decodes every slot of every box just to reach a trainer name, and it
/// reports "not a save" by throwing. Probing hundreds of files that way would be both slow and
/// exception-driven.
/// </summary>
public static class SaveProbe
{
    /// <summary>
    /// Size is the only cheap filter worth applying, and it is the one PKHeX's own folder scanner uses.
    /// Deliberately not an extension allow-list: Switch saves are simply named "main", so filtering on
    /// extension would drop whole generations.
    /// </summary>
    public static bool IsPlausibleSaveSize(long size) => SaveUtil.IsSizeValid(size);

    /// <summary>
    /// Returns false rather than throwing for anything that is not a save we can read. The byte overload
    /// of <c>TryGetSaveFile</c> has no try/catch of its own - unlike the path overload - so a file that
    /// matches a known size but holds unrelated bytes can still throw out of a save constructor.
    /// </summary>
    public static bool TryProbe(byte[] bytes, string? path, out SaveSummary? summary)
    {
        summary = null;

        try
        {
            if (!SaveUtil.TryGetSaveFile(bytes, out var save, path) || save is null) return false;

            summary = new SaveSummary(
                TrainerName: save.OT,
                TrainerId: save.DisplayTID,
                GameName: NameOf(save),
                Context: save.Context.ToString(),
                PlayTime: save.PlayTimeString,
                Language: save.Language,
                BoxCount: save.BoxCount,
                PartyCount: save.PartyCount,
                ChecksumsValid: save.ChecksumsValid);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string NameOf(SaveFile save)
    {
        try
        {
            return GameVersionRepository.Instance.Get(save.Version).Name;
        }
        catch
        {
            return save.Version.ToString();
        }
    }
}
