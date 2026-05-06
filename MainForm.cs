using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;
using Microsoft.Win32;

public class MainForm : Form
{
    const string AlphaRepoBaseUrl = "https://raw.githubusercontent.com/timex05/ReturnAlphaBoost/main/alpha_files/";
    static readonly string[] AlphaFileNames = new[]
    {
        "Boost_Bubble_SF.upk",
        "SFX_Boost_Bubbles.bnk"
    };

    TextBox pathBox;
    Button browseBtn;
    Button replaceBtn;
    Label statusLabel;

    string? installRoot;

    public MainForm()
    {
        Text = "ReturnAlphaBoost";
        Width = 560;
        Height = 220;
        StartPosition = FormStartPosition.CenterScreen;

        var appIconPath = Path.Combine(AppContext.BaseDirectory, "icon.ico");
        if (File.Exists(appIconPath))
        {
            try { Icon = new Icon(appIconPath); } catch { }
        }

        var pathLabel = new Label() { Text = "Install path:", Left = 10, Top = 18, Width = 80 };
        pathBox = new TextBox() { Left = 95, Top = 14, Width = 350, ReadOnly = true };
        browseBtn = new Button() { Text = "Browse...", Left = 450, Top = 12, Width = 85 };
        browseBtn.Click += (s, e) => SelectInstallPath();

        statusLabel = new Label()
        {
            Left = 10,
            Top = 50,
            Width = 525,
            Height = 40,
            Text = ""
        };

        replaceBtn = new Button()
        {
            Left = 10,
            Top = 105,
            Width = 525,
            Height = 35,
            Text = "Replace Bubbles with Alpha Boost",
            Enabled = false
        };
        replaceBtn.Click += (s, e) => CopyAlphaFilesIntoGameFolder();

        Controls.AddRange(new Control[] { pathLabel, pathBox, browseBtn, statusLabel, replaceBtn });

        Load += (s, e) =>
        {
            MessageBox.Show("Please close Rocket League before running this tool.", "Close Rocket League", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            installRoot = TryFindRocketLeagueInstall();
            if (!string.IsNullOrEmpty(installRoot))
            {
                pathBox.Text = installRoot;
                statusLabel.Text = "Rocket League installation found.";
                replaceBtn.Enabled = true;
            }
            else
            {
                pathBox.Text = "Not found";
                statusLabel.Text = "Rocket League installation not found.";
                MessageBox.Show("Rocket League installation not found. You can select it manually.", "Installation not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
    }

    void SelectInstallPath()
    {
        using var dlg = new FolderBrowserDialog();
        dlg.Description = "Select the Rocket League install folder";
        dlg.SelectedPath = string.IsNullOrEmpty(installRoot) ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) : installRoot;

        if (dlg.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        installRoot = dlg.SelectedPath;
        pathBox.Text = installRoot;
        statusLabel.Text = "Using manually selected installation path.";
        replaceBtn.Enabled = true;
    }

    void CopyAlphaFilesIntoGameFolder()
    {
        if (string.IsNullOrEmpty(installRoot))
        {
            MessageBox.Show("Rocket League installation path is missing.", "Missing Installation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var cookedPath = ResolveCookedPcConsolePath(installRoot);
        if (string.IsNullOrEmpty(cookedPath))
        {
            MessageBox.Show("The subfolder 'TAGame\\CookedPCConsole' was not found.", "Subfolder Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            using var http = new HttpClient();
            var downloadFolder = Path.Combine(Path.GetTempPath(), "ReturnAlphaBoost_alpha_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(downloadFolder);

            foreach (var fileName in AlphaFileNames)
            {
                var downloadUrl = AlphaRepoBaseUrl + fileName;
                var response = http.GetAsync(downloadUrl).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Failed to download from GitHub: {downloadUrl}\nStatus: {response.StatusCode}", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var targetDownloadPath = Path.Combine(downloadFolder, fileName);
                var bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                File.WriteAllBytes(targetDownloadPath, bytes);

                var targetFile = Path.Combine(cookedPath, fileName);
                File.Copy(targetDownloadPath, targetFile, true);
            }

            MessageBox.Show($"Downloaded alpha files from GitHub and copied them into TAGame\\CookedPCConsole.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            statusLabel.Text = "Downloaded alpha files from GitHub and copied them into TAGame\\CookedPCConsole.";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Download or copy failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    string? ResolveCookedPcConsolePath(string basePath)
    {
        var subfolder = Path.Combine(basePath, "TAGame", "CookedPCConsole");
        if (Directory.Exists(subfolder))
        {
            return subfolder;
        }

        if (basePath.IndexOf(Path.Combine("TAGame", "CookedPCConsole"), StringComparison.OrdinalIgnoreCase) >= 0 && Directory.Exists(basePath))
        {
            return basePath;
        }

        return null;
    }

    string TryFindRocketLeagueInstall()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "rocketleague"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Epic Games", "rocketleague"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EpicGames", "rocketleague"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "EpicGames", "rocketleague"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "rocketleague"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Rocket League"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam", "steamapps", "common", "rocketleague"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam", "steamapps", "common", "Rocket League")
        };

        foreach (var candidate in candidates)
        {
            try
            {
                if (!string.IsNullOrEmpty(candidate) && Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch { }
        }

        try
        {
            var steamPath = GetSteamInstallPathFromRegistry();
            if (!string.IsNullOrEmpty(steamPath))
            {
                var candidate = Path.Combine(steamPath, "steamapps", "common", "rocketleague");
                if (Directory.Exists(candidate)) return candidate;

                candidate = Path.Combine(steamPath, "steamapps", "common", "Rocket League");
                if (Directory.Exists(candidate)) return candidate;
            }
        }
        catch { }

        return null;
    }

    string GetSteamInstallPathFromRegistry()
    {
        string[] keys = new[]
        {
            @"SOFTWARE\\WOW6432Node\\Valve\\Steam",
            @"SOFTWARE\\Valve\\Steam",
            @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam"
        };

        foreach (var key in keys)
        {
            try
            {
                using var reg = Registry.LocalMachine.OpenSubKey(key);
                if (reg != null)
                {
                    var value = reg.GetValue("InstallPath") as string;
                    if (!string.IsNullOrEmpty(value) && Directory.Exists(value))
                    {
                        return value;
                    }
                }
            }
            catch { }
        }

        try
        {
            using var reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\\Valve\\Steam");
            if (reg != null)
            {
                var value = reg.GetValue("SteamPath") as string ?? reg.GetValue("InstallPath") as string;
                if (!string.IsNullOrEmpty(value) && Directory.Exists(value))
                {
                    return value;
                }
            }
        }
        catch { }

        return null;
    }
}
