# Font Localization

`FontLocalization` provides the reusable TextMeshPro integration for projects that
need to change fonts when the active language changes.

## Runtime

- Add `LocalizedFontTarget` next to each `TextMeshProUGUI` whose font is managed by
  localization.
- Call `LocalizedFontTarget.ApplyFont(TMP_FontAsset)` from the project's locale-to-font
  coordinator. The project remains responsible for choosing the font asset for each
  locale.

## Editor governance

Open `KahaGameCore > Font Localization > TMP Font Target Scanner` to scan all Scenes
and Prefabs under `Assets`.

- **Add** attaches `LocalizedFontTarget` to the selected text.
- **Ignore** records an intentional exception in
  `ProjectSettings/TmpFontTargetScannerIgnoreRegistry.asset`.
- **Add All** and **Ignore All** apply the same actions to every current finding.

The same scan runs before a Player build. Any `TextMeshProUGUI` that has neither a
target nor an ignore record stops the build.

The scanner skips inherited text inside nested Prefabs. That text is governed by its
source Prefab so the component is added only once at the ownership boundary.
