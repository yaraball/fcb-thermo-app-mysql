# fcb-thermo-app

## Description

**fcb-thermo-app** is an Oven Thermal Analytics Dashboard for visualizing, analyzing, and exporting temperature data from industrial ovens. It supports DXF model import, thermoelement placement, measurement file import, and performance mapping. The app is designed for research, engineering, and process optimization in thermal environments.

## Warning

Note that this app is very specific in its configuration and has hard-coded limitations.
Furthermore, this app requires adding two dxf files "MainbodyPyrometers.dxf" and "ReinforcementPyrometers.dxf", as well as an app icon "app_icon.ico" to the App's Resources folder. They are not part of this repository and require manual adding. Make sure to name them identically as described here.  
They DXF files serve as background images for the ovens, while the app icon serves as icon for the app. 

## Features

- Import and visualize DXF oven models
- Place and edit thermoelements on oven canvases
- Import measurement data (CSV/GBD)
- Map and analyze temperature and performance data
- Export views as JPEG images
- User-friendly WPF interface

## License

This project is licensed under the [GNU General Public License v2.0 (GPL-2.0)](https://www.gnu.org/licenses/old-licenses/gpl-2.0.html).

## Installation

If you just want to use the app:

1. **Download the release** from the repository's Releases section.
2. **Extract** the downloaded archive to your desired location on your device.
3. **Navigate to the `Resources` folder** inside the extracted app directory.
4. **Add the two required DXF files**:  
   - `MainbodyPyrometers.dxf`  
   - `ReinforcementPyrometers.dxf`  
   These files are not included in the repository and must be added manually.
5. **Add the required app icon**:
   - `app_icon.ico`
6. **Configure the connection string to you mysql server**
   - Navigate to `appsettings.json` and replace the connection string with the correct parameters for your server.
7. **Run the app** by double-clicking the `.exe` file in the main directory.

If you want to build from source:

1. **Clone the repository:**
   ```sh
   git clone <https://github.com/yaraball/fcb-thermo-app-mysql.git>
   cd fcb-thermo-app
   ```

2. **Open in Visual Studio 2022+ or VS Code (with C# extension).**

3. **Build the project:**
   - Select `Debug` or `Release` configuration.
   - Build and run using `dotnet build` and `dotnet run`.

## Requirements

- Windows 10/11
- .NET 9.0 SDK
- Visual Studio 2022+ or VS Code

## Usage

- Start the application.
- Import oven models and measurement files.
- Place thermoelements and analyze temperature data.
- Export visualizations as needed.

## Authors

Yara Ballnus, Jaskaran Dhillon, Steffen Grachtrup & Matvej Halkou
Students at the University of Bremen

## Support

For issues and feature requests, use the GitHub Issues.

## Project Status

Not actively maintained. This was a student project that is now deemed completed and unlikely to receive future updates.
