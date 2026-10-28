---
name: VELORA
description: A calm native workspace for Windows maintenance.
colors:
  primary: "#54DEC1"
  primary-light-theme: "#006B59"
  on-primary: "#07251F"
  on-primary-light-theme: "#FFFFFF"
  accent-muted: "#183D36"
  accent-muted-light-theme: "#D7F3E9"
  shell: "#0B1216"
  shell-light-theme: "#EDF4F2"
  canvas: "#10191E"
  canvas-light-theme: "#F6FAF9"
  surface: "#172329"
  surface-light-theme: "#FFFFFF"
  surface-secondary: "#1D2D34"
  surface-secondary-light-theme: "#EAF2EF"
  stroke: "#2C4149"
  stroke-light-theme: "#C9DCD5"
  text-primary: "#ECF5F2"
  text-primary-light-theme: "#142D29"
  text-secondary: "#AEC3BE"
  text-secondary-light-theme: "#49665F"
typography:
  headline:
    fontFamily: "Segoe UI Variable, Segoe UI"
    fontSize: "30px"
    fontWeight: 600
  identity:
    fontFamily: "Segoe UI Variable, Segoe UI"
    fontSize: "24px"
    fontWeight: 600
rounded:
  small: "6px"
  medium: "10px"
  large: "16px"
  tab: "8px"
spacing:
  control-gap: "8px"
  toolbar-gap: "12px"
  section-gap: "24px"
  page: "28px 32px 20px 32px"
  card: "18px 20px"
  row: "14px 18px"
  toolbar: "10px 12px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.on-primary}"
    padding: "8px 14px"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.medium}"
    padding: "18px 20px"
  toolbar:
    backgroundColor: "{colors.surface-secondary}"
    rounded: "{rounded.small}"
    padding: "10px 12px"
  list-row:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.medium}"
    padding: "14px 18px"
  tab-selected:
    backgroundColor: "{colors.accent-muted}"
    textColor: "{colors.primary}"
    rounded: "{rounded.tab}"
    padding: "12px 18px"
  chip:
    backgroundColor: "{colors.surface-secondary}"
    rounded: "{rounded.small}"
    padding: "2px 6px"
---

# Design System: VELORA

## Overview

**Creative North Star: "The Quiet Control Room"**

VELORA makes local Windows maintenance feel orderly and deliberate. Graphite surfaces, mint emphasis and familiar native controls give real machine information priority. Actions remain close to their scope, with risk and recovery information visible before changes are applied.

The composition is a workspace: horizontal navigation, broad reading rows, compact supporting metadata and a restrained identity. The light theme uses the same hierarchy with pale green neutrals and deep teal emphasis. The owner delegated the design and approved the VELORA identity and teal direction.

**Key Characteristics:**
- Native WPF interaction and Fluent symbols.
- Restrained teal emphasis over layered neutral surfaces.
- Clear task hierarchy and readable localized content.
- Explicit selection, review and action areas.

## Colors

### Primary

Mint is the dark-theme primary accent for selected states and primary actions. Deep teal takes the same role in the light theme. Each theme has its own on-accent text and muted accent surface; the frontmatter records the paired values.

### Neutral

Shell, canvas, surface and secondary surface establish depth. Stroke separates adjacent controls and containers. Primary text carries content; secondary text carries descriptions and metadata. Theme pairs are semantic equivalents, not interchangeable swatches.

**The Semantic Accent Rule.** Use teal for selection and intentional actions; keep warning, danger and success states tied to their existing semantic resources and text labels.

## Typography

Use Segoe UI Variable with Segoe UI fallback, inherited from the main window. Page headings use the headline token; the settings identity uses the identity token. Body, BodyStrong and Caption retain WPF-UI typography resources rather than introducing a second custom type scale. Wrap descriptions and localized headings.

## Layout

The default main window is 1360 by 860 WPF device-independent units, with a 900 by 620 minimum. The native title bar is 44 units high. A horizontal workspace navigation band follows it; navigation scrolls when its content exceeds the available width. Settings is part of the same navigation system.

Pages use a left/top/right/bottom inset of 32/28/32/20 units. Frontmatter spacing and component padding translate WPF source values into CSS top/right/bottom/left order for portability. WPF units correspond to CSS reference pixels for documentation, not physical monitor pixels.

The overview uses an asymmetric 3:2 split: storage and security information sit on the left, hardware on the right. Tuning uses full-width rows with an action area on the right. The app library places filters above its list and batch actions below it; package metadata expands inside each row. Cleanup puts selection review and actions in a bottom dock. Startup separates Apps and Tasks into tabs. Settings pairs a 240-unit identity column with a flexible preferences column.

Lists and page content scroll vertically. Toolbars wrap and text wraps where implemented. Keep minimum-width behavior faithful to the native layout; no web or mobile breakpoint system exists.

**The Scope Rule.** Keep batch actions outside individual rows, next to the selection summary for the list they affect.

## Elevation & Depth

Depth comes primarily from tonal surfaces, one-unit strokes and the native window/dialog layers. Shared card and toolbar styles add no custom drop shadows. Preserve the existing WPF-UI dialog, focus, disabled and loading treatments. Smooth scrolling uses the shared behavior with smoothing 0.20 and multiplier 1.3; it does not justify decorative motion.

## Shapes

Use the small radius for toolbars and metadata pills, medium for cards and list rows, and large for introductory surfaces. Workspace tabs use their own tab radius. The existing 40-unit circular symbol surface is a supporting icon treatment. Do not force every native control into the card radius.

## Components

- **Buttons:** primary buttons use the theme accent and its paired foreground. Shared buttons have a minimum height of 36 units and horizontal/vertical padding of 14/8. Secondary and transparent actions retain native WPF-UI states and keyboard behavior.
- **Cards and rows:** use semantic surface, stroke and text resources. Shared cards have horizontal/vertical padding of 20/18; rows use 18/14, with an 8-unit bottom gap.
- **Inputs:** search fields have a 200-unit minimum width. Keep native text entry, focus, disabled and selection behavior; use localized accessible labels.
- **Navigation:** the main navigation is horizontal. Startup workspace tabs use muted teal for selection, secondary surface on hover and accent stroke for keyboard focus.
- **Chips and badges:** supporting pills use small corners and compact padding; risk and state badges pair meaning with text rather than color alone.
- **Review docks:** app removal and cleanup expose selection totals and scoped actions after the list. Confirm destructive operations through the existing dialogs.
- **Identity:** Resources/Images/VeloraMark.svg is the vector source; VeloraMark.png and Logo.ico use the same mint V with a pale cut on a deep teal square. Use the same geometry in the title bar and settings.

The machine-readable companion lives inside the app at fasterLegit/.impeccable/design.json to respect the repository rule against new root directories. Its self-contained HTML/CSS examples are portable visual previews of native components, not a web implementation or replacements for WPF templates. Native UI source remains authoritative for interaction details inherited from WPF-UI.

## Do's and Don'ts

### Do:
- Do bind surfaces and accent foregrounds through paired theme resources.
- Do preserve localized wrapping, keyboard access and native control states.
- Do show real system data, scoped actions, risk and supported recovery.
- Do reuse the approved VELORA mark and native Fluent symbols.

### Don't:
- Don't add promotional references to the previous identity in user-facing surfaces.
- Don't remove mandatory copyright, license or legal attribution notices.
- Don't invent health scores, performance measurements or speedup claims.
- Don't replace the horizontal workspace with the old navigation rail.
