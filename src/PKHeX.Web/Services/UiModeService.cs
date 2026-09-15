using Blazored.LocalStorage;
using Microsoft.JSInterop;

namespace PKHeX.Web.Services;

/// <summary>
/// Which interface the app wears. This is not a theme: the Pokedex mode carries its own components and
/// changes layout as well as appearance, and each mode has its own light and dark palettes.
///
/// Deliberately shaped like <see cref="AntdThemeService"/> - local storage plus a static changed event -
/// so the two settings behave the same way rather than inventing a second mechanism.
/// </summary>
public class UiModeService(ISyncLocalStorageService localStorage, IJSRuntime js)
{
    public const string ModeAttribute = "data-ui-mode";
    public const string ThemeAttribute = "data-theme";

    public const string ThemeStylesheetId = "pokedex-theme";

    private const string StorageKey = "uiMode";

    public static event Func<UiMode, Task>? ModeChanged;

    public UiMode Mode { get; private set; } = Read(localStorage);

    public bool IsPokedex => Mode == UiMode.Pokedex;

    public async Task Set(UiMode mode)
    {
        Mode = mode;
        localStorage.SetItemAsString(StorageKey, mode.ToString());

        await Apply();

        if (ModeChanged is not null) await ModeChanged.Invoke(mode);
    }

    /// <summary>
    /// Pushes both facts onto the root element. The theme has to travel with the mode because AntDesign's
    /// ConfigProvider keeps it in .NET state only, where CSS cannot reach it.
    /// </summary>
    public async Task Apply(bool? isDarkTheme = null)
    {
        await js.InvokeVoidAsync("setShellAttribute", ModeAttribute, Mode switch
        {
            UiMode.Pokedex => "pokedex",
            _ => "default",
        });

        if (isDarkTheme is not null)
        {
            await js.InvokeVoidAsync("setShellAttribute", ThemeAttribute, isDarkTheme.Value ? "dark" : "light");
        }

        // The authored theme sheet carries no mode selector on its rules, so it is switched on as a whole
        // rather than being rewritten to nest under one.
        await js.InvokeVoidAsync("setStylesheetEnabled", ThemeStylesheetId, IsPokedex);
    }

    private static UiMode Read(ISyncLocalStorageService storage) =>
        storage.GetItemAsString(StorageKey) switch
        {
            nameof(UiMode.Pokedex) => UiMode.Pokedex,
            _ => UiMode.Default,
        };
}

public enum UiMode
{
    /// The modernised default - AntDesign, and what every user gets today.
    Default,

    /// The Pokedex interface, built on its own components.
    Pokedex,
}
