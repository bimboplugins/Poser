# Poser

Poser is a lightweight UI and command utility that allows you to instantly switch between your available idle, ground sit, chair sit, and doze poses. Bypass the standard emote menu and snap directly to a specific pose index using a sleek visual interface or targeted slash commands.

## Installation
To install Poser, you will need to add this custom repository to your Dalamud settings.

1. Open the game and type `/xlsettings` to open the Dalamud Settings menu.
2. Navigate to the **Experimental** tab.
3. Scroll down to the **Custom Plugin Repositories** section.
4. Paste the following URL into a blank input field:
   `https://raw.githubusercontent.com/bimboplugins/Poser/master/pluginmaster.json`
5. Click the **+** button to add it, then click **Save and Close**.
6. Type `/xlplugins` to open the Plugin Installer, search for **Poser**, and click Install.

## Commands
Poser supports both a graphical interface and direct chat commands for seamless integration into your gameplay or macros.

| Command | Description |
| :--- | :--- |
| `/poser` | Opens or closes the main Poser UI. |
| `/poser [stance] [index]` | Instantly sets a pose via subcommand (e.g., `/poser idle 2`, `/poser sit 1`). |
| `/poserconfig` | Opens the configuration menu to adjust UI layout and opacity. |
| `/pidle [index]` | Sets your idle pose to the specified index. |
| `/psit [index]` | Sets your chair sit pose to the specified index. |
| `/pgsit [index]` | Sets your ground sit pose to the specified index. |
| `/pdoze [index]` | Sets your doze pose to the specified index. |