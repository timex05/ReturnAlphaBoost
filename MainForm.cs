using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;
using Microsoft.Win32;

public class MainForm : Form
{
    TextBox pathBox;
    Button browseBtn;
    Button replaceBtn;
    Label statusLabel;
    ComboBox profileBox;

    string? installRoot;
    ReturnAlphaBoostConfig config = ReturnAlphaBoostConfig.CreateDefault();
    static readonly HttpClient Http = new();

    public MainForm()
    {
        Text = "ReturnAlphaBoost";
        Width = 600;
        Height = 300;
        StartPosition = FormStartPosition.CenterScreen;

        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                var extracted = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                if (extracted != null) Icon = extracted;
            }
        }
        catch { }

        // Main content panel (fills except footer)
        var mainPanel = new Panel()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var profileLabel = new Label() { Text = "Profile:", AutoSize = true, Location = new Point(10, 12) };
        profileBox = new ComboBox()
        {
            Width = 350,
            Location = new Point(110, 8),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        var pathLabel = new Label() { Text = "Install path:", AutoSize = true, Location = new Point(10, 45) };
        pathBox = new TextBox() { Width = 350, Location = new Point(110, 43), ReadOnly = true };
        browseBtn = new Button() { Text = "Browse...", Width = 85, Location = new Point(470, 43) };
        browseBtn.Click += (s, e) => SelectInstallPath();

        statusLabel = new Label()
        {
            AutoSize = true,
            Location = new Point(10, 80),
            MaximumSize = new Size(560, 40),
            Text = ""
        };

        replaceBtn = new Button()
        {
            Text = "Replace Bubbles with Alpha Boost",
            Dock = DockStyle.Bottom,
            Height = 40
        };
        replaceBtn.Click += (s, e) => CopyAlphaFilesIntoGameFolder();

        mainPanel.Controls.AddRange(new Control[] { profileLabel, profileBox, pathLabel, pathBox, browseBtn, statusLabel, replaceBtn });

        // Footer panel
        var footerPanel = new Panel()
        {
            Dock = DockStyle.Bottom,
            Height = 35,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = SystemColors.ControlLight
        };

        var docsLink = new LinkLabel()
        {
            Text = "Docs",
            AutoSize = true,
            Location = new Point(540, 8),
            LinkColor = Color.FromArgb(0, 102, 204),
            VisitedLinkColor = Color.FromArgb(128, 0, 255),
            Tag = "https://github.com/timex05/ReturnAlphaBoost/blob/main/README.md"
        };
        docsLink.LinkClicked += (s, e) =>
        {
            var psi = new ProcessStartInfo() { FileName = docsLink.Tag.ToString(), UseShellExecute = true };
            Process.Start(psi);
        };

        var separator = new Label()
        {
            Text = "|",
            AutoSize = true,
            Location = new Point(515, 8),
            ForeColor = SystemColors.ControlDark
        };

        var githubLink = new LinkLabel()
        {
            Text = "GitHub",
            AutoSize = true,
            Location = new Point(455, 8),
            LinkColor = Color.FromArgb(0, 102, 204),
            VisitedLinkColor = Color.FromArgb(128, 0, 255),
            Tag = "https://github.com/timex05/ReturnAlphaBoost"
        };
        githubLink.LinkClicked += (s, e) =>
        {
            var psi = new ProcessStartInfo() { FileName = githubLink.Tag.ToString(), UseShellExecute = true };
            Process.Start(psi);
        };

        footerPanel.Controls.AddRange(new Control[] { docsLink, separator, githubLink });

        Controls.AddRange(new Control[] { mainPanel, footerPanel });

        Resize += (s, e) =>
        {
            int panelWidth = footerPanel.Width;
            githubLink.Location = new Point(panelWidth - 140, 8);
            separator.Location = new Point(panelWidth - 95, 8);
            docsLink.Location = new Point(panelWidth - 60, 8);
        };

        Load += (s, e) =>
        {
            try
            {
                config = ReturnAlphaBoostConfig.Load(AppContext.BaseDirectory);
                LoadProfileList();
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
                    MessageBox.Show(this, "Rocket League installation not found. You can select it manually.", "Installation not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Configuration could not be loaded from GitHub:\n" + ex.Message, "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        };

        profileBox.SelectedIndexChanged += (s, e) => UpdateProfileStatus();
    }

    void LoadProfileList()
    {
        profileBox.Items.Clear();

        foreach (var profileName in config.Profiles.Keys.OrderBy(profileName => profileName))
        {
            profileBox.Items.Add(profileName);
        }

        if (profileBox.Items.Count > 0)
        {
            profileBox.SelectedIndex = 0;
            replaceBtn.Enabled = true;
        }
        else
        {
            statusLabel.Text = "No profiles were found in the config file.";
            replaceBtn.Enabled = false;
        }
    }

    void UpdateProfileStatus()
    {
        var selectedProfileName = GetSelectedProfileName();
        if (string.IsNullOrEmpty(selectedProfileName))
        {
            return;
        }

        statusLabel.Text = $"Selected profile: {selectedProfileName}";
    }

    string? GetSelectedProfileName()
    {
        return profileBox.SelectedItem as string;
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
            MessageBox.Show(this, "The subfolder 'TAGame\\CookedPCConsole' was not found.", "Subfolder Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var selectedProfileName = GetSelectedProfileName();
        if (string.IsNullOrEmpty(selectedProfileName))
        {
            MessageBox.Show(this, "Please select a profile first.", "Profile Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var profile = config.GetProfile(selectedProfileName);
        if (profile == null)
        {
            MessageBox.Show(this, $"The profile '{selectedProfileName}' was not found in returnalphaboost.config.json.", "Config Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var mappings = profile.GetValidMappings();
        if (mappings.Count == 0)
        {
            MessageBox.Show(this, $"The profile '{selectedProfileName}' does not contain any valid mappings.", "Config Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (string.Equals(profile.Type, "local", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryCopyFromLocalSource(installRoot, profile, mappings, cookedPath))
            {
                return;
            }

            MessageBox.Show(this, $"Copied the configured local files into TAGame\\CookedPCConsole.\n\nRocket League restart is required for the changes to take effect.", "Success - Restart Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            statusLabel.Text = "Copied the configured local files into TAGame\\CookedPCConsole. Rocket League restart required.";
            return;
        }

        if (string.Equals(profile.Type, "online", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryCopyFromRemoteSource(profile, mappings, cookedPath))
            {
                return;
            }

            MessageBox.Show(this, $"Downloaded the configured online files from GitHubusercontent and copied them into TAGame\\CookedPCConsole.\n\nRocket League restart is required for the changes to take effect.", "Success - Restart Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            statusLabel.Text = "Downloaded the configured online files and copied them into TAGame\\CookedPCConsole. Rocket League restart required.";
            return;
        }

        MessageBox.Show(this, $"The profile '{selectedProfileName}' uses unsupported type '{profile.Type}'. Use 'local' or 'online'.", "Unsupported Config", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
    }

    bool TryCopyFromLocalSource(string installRootValue, AlphaProfile profile, IReadOnlyCollection<FileMapping> mappings, string cookedPath)
    {
        var sourceRoot = ResolveLocalSourceRoot(installRootValue, profile.SourceRoot);
        if (!Directory.Exists(sourceRoot))
        {
            MessageBox.Show(this, $"The source folder '{sourceRoot}' was not found.", "Source Folder Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        try
        {
            foreach (var mapping in mappings)
            {
                var sourceFile = Path.Combine(sourceRoot, mapping.Source);
                if (!File.Exists(sourceFile))
                {
                    MessageBox.Show(this, $"The source file '{sourceFile}' was not found.", "Source File Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                var targetFile = Path.Combine(cookedPath, mapping.Target);
                var targetDirectory = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrEmpty(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.Copy(sourceFile, targetFile, true);
            }

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Download or copy failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    bool TryCopyFromRemoteSource(AlphaProfile profile, IReadOnlyCollection<FileMapping> mappings, string cookedPath)
    {
        try
        {
            foreach (var mapping in mappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.Source))
                {
                    MessageBox.Show(this, "Each online mapping requires a full URL in 'source'.", "Config Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                var bytes = Http.GetByteArrayAsync(mapping.Source).GetAwaiter().GetResult();

                var targetFile = Path.Combine(cookedPath, mapping.Target);
                var targetDirectory = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrEmpty(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.WriteAllBytes(targetFile, bytes);
            }

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Download or copy failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    string ResolveLocalSourceRoot(string installRootValue, string? sourceRoot)
    {
        if (string.IsNullOrWhiteSpace(sourceRoot))
        {
            return installRootValue;
        }

        if (Path.IsPathRooted(sourceRoot))
        {
            return sourceRoot;
        }

        return Path.GetFullPath(Path.Combine(installRootValue, sourceRoot));
    }

    string CombineUrl(string baseUrl, string relativePath)
    {
        var normalizedBaseUrl = baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : baseUrl + "/";
        var normalizedRelativePath = relativePath.Replace('\\', '/');
        return new Uri(new Uri(normalizedBaseUrl, UriKind.Absolute), normalizedRelativePath).ToString();
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

    string? TryFindRocketLeagueInstall()
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

    string? GetSteamInstallPathFromRegistry()
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
