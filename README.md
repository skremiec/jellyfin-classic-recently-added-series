# Classic Recently Added Series for Jellyfin

A Jellyfin plugin that restores the classic "Recently Added" behavior by displaying the parent **Series** instead of individual **Episodes** or **Seasons** in your home screen and library views.

---

## Features

- **Classic Experience**: Shows TV Series entries in "Recently Added" rows instead of cluttered single episodes.
- **Zero Configuration**: Works automatically out of the box after installation.

---

## Installation

### Method 1: Plugin Repository (Recommended)

1. Open your Jellyfin Web Client and navigate to **Dashboard** > **Plugins** > **Repositories**.
2. Click **+ Add Repository**.
3. Fill in the details:
   - **Repository Name**: `Classic Recently Added Series`
   - **Repository URL**:
     ```
     https://github.com/skremiec/jellyfin-classic-recently-added-series/releases/latest/download/manifest.json
     ```
4. Click **Save**.
5. Go to the **Catalog** tab, find **Classic Recently Added Series** under *Movies and Shows*, and click **Install**.
6. **Restart** your Jellyfin server.

---

### Method 2: Manual Installation

1. Go to the [Releases](https://github.com/skremiec/jellyfin-classic-recently-added-series/releases) page.
2. Download the latest `ClassicRecentlyAddedSeries_{version}.zip` asset.
3. Extract the ZIP contents into your Jellyfin `plugins` directory inside a folder named `ClassicRecentlyAddedSeries`:

   | Platform | Typical Plugins Path |
   | :--- | :--- |
   | **Linux (bare-metal)** | `/var/lib/jellyfin/plugins/ClassicRecentlyAddedSeries/` |
   | **Docker** | `<config_dir>/plugins/ClassicRecentlyAddedSeries/` |
   | **Windows** | `%ProgramData%\Jellyfin\Server\plugins\ClassicRecentlyAddedSeries\` |
   | **macOS** | `~/.local/share/jellyfin/plugins/ClassicRecentlyAddedSeries/` |

   The target folder should contain:
   ```text
   ClassicRecentlyAddedSeries/
   ├── ClassicRecentlyAddedSeries.dll
   ├── ClassicRecentlyAddedSeries.pdb
   └── meta.json
   ```

4. **Restart** your Jellyfin server.

---

## Building from Source

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)

### Build Steps

Clone the repository and build using Make or the .NET CLI:

```bash
# Clone the repository
git clone https://github.com/skremiec/jellyfin-classic-recently-added-series.git
cd jellyfin-classic-recently-added-series

# Build Debug
make build
# or: dotnet build

# Publish Release
make publish
# or: dotnet publish ClassicRecentlyAddedSeries/ClassicRecentlyAddedSeries.csproj -c Release -o ./publish
```

The compiled plugin and its generated `meta.json` will be located in the `./publish` directory.
