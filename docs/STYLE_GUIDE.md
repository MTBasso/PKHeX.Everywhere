# Pokédex Theme — Style Guide

Companion to `pokedex-theme.css`. Read this first, then wire the classes/tokens
into the existing PKHeX.web / PKVault fork components.

## The concept

The reference hardware (Kanto → Kalos units) is a red-and-black plastic shell
wrapped around a small screen. The reference software (Unova Pokédex list) is
that screen: a green monochrome LCD, a dot-grid pixel texture, hollow vs.
filled circles for seen/owned, a fixed counter readout at the top.

So the theme has two layers, not one:

- **Shell** — the app chrome (top bar, tabs, panel frames, buttons). Red/black
  plastic, screws, lens-lights. This is where "Pokédex device" reads.
- **Screen** — anywhere Pokémon data actually lives (box grids, dex lists,
  stat panels). Green LCD glass, scanlines, dot-matrix. This is where
  "Pokédex software" reads.

Don't apply the screen treatment to the whole app — it's a monochrome-green
surface and your users will be staring at it for a while sorting boxes. Keep
it to the data panels, the way the real device keeps it to the actual screen.

## Color

| Token | Hex | Use |
|---|---|---|
| `--shell-red` | `#D6362B` | primary chrome, top bar, active tab |
| `--shell-red-dark` | `#9E2419` | chrome shadows, pressed states |
| `--shell-black` | `#1C1C1E` | bezels, dividers, D-pad |
| `--shell-charcoal` | `#2B2B2E` | secondary chrome panels |
| `--screen-darkest` | `#0F380F` | LCD text / filled pixels |
| `--screen-dark` | `#306230` | LCD secondary text, borders on screen |
| `--screen-mid` | `#8BAC0F` | LCD active/hover state |
| `--screen-light` | `#9BBC0F` | LCD background |
| `--lens-blue` | `#2F86D6` | status light (connected / save loaded) |
| `--lens-amber` | `#F2B705` | status light (attention / unsaved) |
| `--cream` | `#F5F3E7` | text on red shell, glass reflection |

This is a four-shade LCD ramp (screen-darkest → screen-light) on purpose —
it's the actual constraint of a monochrome dot-matrix display, and it's what
makes the screen areas read as "device" instead of "green theme." Don't add a
fifth green or lighten it toward teal; the limitation is the point.

Keep `--shell-red` for structural chrome only (bars, active state, borders).
If red starts showing up as body text or filling large areas, it'll fight the
green screens for attention — one bold color per panel per screen.

## Type

- **Pixel display** — `Press Start 2P` (or `Silkscreen` as a fallback). Use it
  *only* for short labels: box tab names, the "SEEN / OBTAINED" readout,
  button micro-labels, badge counts. It's illegible at body-text length —
  don't set Pokémon names, stats, or move lists in it.
- **Data/mono** — `JetBrains Mono` or `IBM Plex Mono` for everything else:
  species names, IVs/EVs, nicknames, trainer info, list rows. This carries
  the "terminal readout" feeling without sacrificing legibility across a
  whole box of 30 Pokémon.

Two families, clearly distinct roles (display vs. data) — not decoration
stacked on decoration.

## Layout patterns

```
┌───────────────────────────────────────────┐
│ ● ●   POKÉDEX            [seen 115][own 25]│  ← shell header, lens-lights
├───────────────────────────────────────────┤
│▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓│
│▓ ○ 060 Krokorok                          ▓│  ← screen: dex-list-row
│▓ ● 065 Scrafty                     [i]   ▓│     hollow/filled = seen/owned
│▓ ○ 066 Scraggy                           ▓│
│▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓│
├───────────────────────────────────────────┤
│ [BOX 1] [BOX 2] [BOX 3] …    ← cartridge tabs
└───────────────────────────────────────────┘
```

- Box tabs (`.tab-cartridge`) sit like cartridge slots along the shell, not
  rounded pill tabs — flat top, one accent edge, selected tab reads red.
- Storage grid slots (`.box-slot`) live inside a `.pokedex-screen` panel so
  empty slots read as dim/off pixels rather than empty white space.
- Status lights (`.status-led`) are functional, not decorative: blue = save
  loaded, amber = unsaved changes, off/grey = no save attached. Use the two
  lens-lights already implied by the hardware photos (top-left corner of the
  shell) instead of inventing a new banner/toast for save state.

## Motion — spend it once

One deliberate moment: when a box slot or dex row is focused/hovered, the
scanline texture gets a brief brightness pulse (`.screen-flicker`, ~180ms,
already in the CSS). That's it. Don't add hover transitions to every card or
a page-load stagger — the real hardware doesn't animate, it just lights up
when you interact with it.

Respect `prefers-reduced-motion` — the CSS already guards the flicker and
scanline animation behind it.

## Accessibility notes

- The LCD green-on-green ramp is low contrast by design (that's the whole
  reference). Use `--screen-darkest` text only on `--screen-light` /
  `--screen-mid` backgrounds — never green-on-green at adjacent shades for
  actual readable text (stats, names). Reserve the subtler combinations for
  decorative texture (scanlines, dimmed/empty slots).
- Keep a visible focus ring (`--lens-blue`, 2px) on every interactive
  element inside `.pokedex-screen` — the monochrome surface makes default
  browser focus outlines nearly invisible otherwise.
- `Press Start 2P` renders small — never drop below 10–11px effective size
  for it, and never use it for anything the user has to read quickly.
