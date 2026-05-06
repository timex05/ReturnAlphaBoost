# ReturnAlphaBoost (kleines Windows-UI Tool)

Kleines Windows‑GUI‑Tool (WinForms) mit zwei Funktionen:

- `Greet` — Begrüßt den eingegebenen Namen.
- `Sum` — Summiert Zahlen, getrennt durch Leerzeichen oder Komma.

Build & Publish (Windows exe):

```powershell
dotnet build
# Für eine veröffentlichte Windows-x64 EXE (Single File):
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishTrimmed=false --self-contained true
# Ergebnis in: bin\Release\net8.0\win-x64\publish\ReturnAlphaBoost.exe
```

Starten lokal während Entwicklung:

```powershell
dotnet run
```
# ReturnAlphaBoost