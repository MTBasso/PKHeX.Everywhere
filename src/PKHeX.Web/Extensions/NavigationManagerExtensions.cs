using System.Web;
using Microsoft.AspNetCore.Components;
using PKHeX.Facade.Pokemons;
using PKHeX.Web.Plugins;
using PKHeX.Web.Services.Plugins;

namespace PKHeX.Web.Extensions;

public static class NavigationManagerExtensions
{
    public static string CurrentRoute(this NavigationManager navigation) =>
        navigation.Uri.Replace(navigation.BaseUri, string.Empty);
    
    /// <summary>
    /// Every route below is written **relative**, without a leading slash, on purpose.
    ///
    /// A leading slash is resolved against the domain root and ignores &lt;base href&gt;, so on a project
    /// page such as /PKHeX.Everywhere/ it navigates straight out of the app - "/settings" lands on
    /// mtbasso.github.io/settings, which does not exist. Relative paths resolve against the base and work
    /// the same whether the app is served from a sub-path or from the root.
    /// </summary>
    public static void NavigateToHomePage(this NavigationManager navigation) =>
        navigation.NavigateTo(navigation.BaseUri);
    
    /// Where the user lands once a save is open. Distinct from the home page, which is now the save
    /// library - sending someone back there straight after they picked a save would be a loop.
    public static void NavigateToTrainer(this NavigationManager navigation) =>
        navigation.NavigateTo("trainer");

    public static void NavigateToPokemon(this NavigationManager navigation, PokemonSource source, UniqueId uniqueId,
        bool replace = false) =>
        navigation.NavigateTo($"pokemon/{source.RouteString()}/{uniqueId}", replace: replace);
    
    public static void NavigateToNewCloneOf(this NavigationManager navigation, UniqueId uniqueId) => 
        navigation.NavigateTo($"pokemon/{uniqueId}/clone");
    
    public static void NavigateToPokemonBox(this NavigationManager navigation, bool replace = false) => 
        navigation.NavigateTo($"pokemon-box", replace: replace);
    
    public static void NavigateToSearchEncounter(this NavigationManager navigation, bool replace = false) =>
        navigation.NavigateTo($"pokemon/search-encounter", replace);
    
    public static void NavigateToSelectedEncounter(this NavigationManager navigation) =>
        navigation.NavigateTo($"pokemon/selected-encounter");
    
    public static void NavigateToLoadedPokemon(this NavigationManager navigation) =>
        navigation.NavigateTo($"pokemon/loaded-file");
    
    public static void NavigateToPlugInErrors(this NavigationManager navigation) =>
        navigation.NavigateTo($"plugins/errors");
    
    public static void NavigateToPlugIns(this NavigationManager navigation) =>
        navigation.NavigateTo($"plugins");
    
    public static void NavigateToPlugIn(this NavigationManager navigation, LoadedPlugIn plugIn) =>
        navigation.NavigateTo($"plugins/{plugIn.Id}");
    
    public static void NavigateToPlugInPage(this NavigationManager navigation, string plugInId, string path,
        Outcome.PlugInPage.PageLayout layout) =>
        navigation.NavigateTo($"plugins/{plugInId}/{path}/{layout.ToString().ToLowerInvariant()}");
    
    public static void NavigateToAnalyticsResults(this NavigationManager navigation) =>
        navigation.NavigateTo($"analytics");
    
    public static void NavigateToSave(this NavigationManager navigation) =>
        navigation.NavigateTo($"save");
    
    public static void NavigateToSettings(this NavigationManager navigation) =>
        navigation.NavigateTo($"settings");

    public static void NavigateToSharedPokemon(this NavigationManager navigation, Guid id) =>
        navigation.NavigateTo($"s/{id}");
    
    public static void NavigateToCloudPokemonList(this NavigationManager navigation) =>
        navigation.NavigateTo($"cloud/pokemon");
    
    public static void NavigateToCloudPokemon(this NavigationManager navigation, Guid id) =>
        navigation.NavigateTo($"cloud/pokemon/{id}");
    
    public static void NavigateToReleaseNotes(this NavigationManager navigation, DateOnly? since = null) =>
        navigation.NavigateTo($"release-notes?since={since?.ToString("yyyy-MM-dd")}");
    
    public static void StoreOnQuery(this NavigationManager navigation, Dictionary<string, object?> parameters)
    {
        navigation.NavigateTo(navigation.GetUriWithQueryParameters(parameters));
    }
}