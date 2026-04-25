# UI Controller

UI Controller is a Unity Package Manager package scaffold for reusable UI controller code.

## Install

In Unity, open **Window > Package Manager**, click **+**, choose **Install package from git URL...**, and enter:

```text
https://github.com/windsmoon/UIController.git
```

Because `package.json` is located at the repository root, no `?path=` suffix is required.

## Development

This repository contains a Unity development project in `UnityProject~`. The trailing `~` keeps Unity Package Manager from importing the development project as package content. The project references this package locally with:

```json
"com.windsmoon.uicontroller": "file:../../"
```

Open `UnityProject~` in Unity, then use **Assets > Open C# Project** or **Tools > UI Controller > Sync Rider Project Files**. Rider will generate projects for the local package and show the package assemblies from the repository root.

Package source lives in:

- `Runtime/` for runtime code.
- `Editor/` for editor-only code.
- `Tests~/` for local test placeholders hidden from package import.
- `Samples~/` for Package Manager importable samples.
- `Documentation~/` for package documentation.

## License

MIT. See `LICENSE`.
