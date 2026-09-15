/*
    The app's light/dark switch runs through AntDesign's ConfigProvider, which never touches the DOM, so
    stylesheets have no way to know which theme is active. The Pokedex UI mode needs both facts - mode and
    theme - as attributes on the root element, because it restyles surfaces AntDesign does not own and
    carries its own light and dark palettes.
*/
export function setShellAttribute(name: string, value: string | null): void {
    const root = document.documentElement;

    if (value === null || value === "") {
        root.removeAttribute(name);
        return;
    }

    root.setAttribute(name, value);
}

/*
    The Pokedex theme sheet is authored without a mode selector on its rules, so it is switched on and
    off as a whole rather than rewritten to nest under one. Keeping it untouched means the design file
    stays the source of truth and can be replaced wholesale.
*/
export function setStylesheetEnabled(id: string, enabled: boolean): void {
    const link = document.getElementById(id) as HTMLLinkElement | null;
    if (!link) return;

    link.disabled = !enabled;
}
