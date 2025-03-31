# Printer Manager native Tool

A lightweight Windows Forms application (C# / .NET) that simulates the classic printer control panel from Windows XP and 7, for quick and efficient management of installed printers on modern systems (Windows 10/11).

## 🎯 Features

- 📜 Displays a list of all installed printers
- 🖱️ Right-click menu with:
  - Open Print Queue
  - Printer Preferences
  - Set as Default Printer
  - Printer Properties
- 🔁 Reload printers list
- 🧹 Spooler Tools (with admin rights prompt only when needed):
  - Restart Print Spooler
  - Clear Spooler Folder

## 💡 Why this tool?

Windows 11's printer settings are often slow and hidden behind multiple menus.  
This tool restores a minimal and fast interface for technicians and system administrators.

## 🔒 UAC Smart Elevation

The app will **only request admin privileges when needed**, using UAC elevation:
- Restarting the spooler
- Clearing the spooler folder

No need to run the whole app as administrator.

## 🛠️ Technologies

- Language: C#
- Framework: .NET 8.0 (WinForms)
- Visual Studio 2022

## 🚀 How to Build

1. Open the solution in Visual Studio 2022+
2. Set configuration to **Release**
3. Build the project
4. Run the `.exe`

## 📦 Portable Mode

The application is fully portable and does **not require installation**.  
All dependencies are embedded in the `.exe`.

## 📷 Preview

![Screenshot](/main_window.png)

## 📄 License

MIT License – feel free to use, modify and redistribute.
