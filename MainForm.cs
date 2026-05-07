using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;
using Microsoft.Win32;

public class MainForm : Form
{
    TextBox pathBox;
    Button browseBtn;
    Label statusLabel;
    FlowLayoutPanel profileButtonsPanel;

    string? installRoot;
    ReturnAlphaBoostConfig config = ReturnAlphaBoostConfig.CreateDefault();
    static readonly HttpClient Http = new();

    public MainForm()
    {
        Text = "ReturnAlphaBoost";
        Width = 760;
        Height = 420;
        MinimumSize = new Size(520, 320);
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

        // Main content layout (responsive)
        var mainPanel = new TableLayoutPanel()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 1,
            RowCount = 3
        };
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Installation row
        var installGrid = new TableLayoutPanel()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 10)
        };
        installGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        installGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        installGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var pathLabel = new Label()
        {
            Text = "Install path:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 10, 6)
        };
        pathBox = new TextBox()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Margin = new Padding(0, 3, 8, 3)
        };
        browseBtn = new Button()
        {
            Text = "Browse...",
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 3, 0, 3)
        };
        browseBtn.Click += (s, e) => SelectInstallPath();

        installGrid.Controls.Add(pathLabel, 0, 0);
        installGrid.Controls.Add(pathBox, 1, 0);
        installGrid.Controls.Add(browseBtn, 2, 0);

        // Profile buttons panel
        profileButtonsPanel = new FlowLayoutPanel()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0)
        };

        mainPanel.Controls.Add(installGrid, 0, 0);
        mainPanel.Controls.Add(profileButtonsPanel, 0, 1);
        mainPanel.Controls.Add(new Panel() { Dock = DockStyle.Fill }, 0, 2);

        // Footer panel
        var footerPanel = new Panel()
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = SystemColors.ControlLight
        };

        var footerContainer = new TableLayoutPanel()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(8, 4, 8, 4)
        };
        footerContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footerContainer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        statusLabel = new Label()
        {
            AutoSize = true,
            Text = "",
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            Margin = new Padding(0, 4, 0, 0)
        };

        var footerLinks = new FlowLayoutPanel()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        var docsLink = new LinkLabel()
        {
            Text = "Docs",
            AutoSize = true,
            Margin = new Padding(0),
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
            Margin = new Padding(8, 0, 8, 0),
            ForeColor = SystemColors.ControlDark
        };

        var githubLink = new LinkLabel()
        {
            Text = "GitHub",
            AutoSize = true,
            Margin = new Padding(0),
            LinkColor = Color.FromArgb(0, 102, 204),
            VisitedLinkColor = Color.FromArgb(128, 0, 255),
            Tag = "https://github.com/timex05/ReturnAlphaBoost"
        };
        githubLink.LinkClicked += (s, e) =>
        {
            var psi = new ProcessStartInfo() { FileName = githubLink.Tag.ToString(), UseShellExecute = true };
            Process.Start(psi);
        };

        footerLinks.Controls.Add(docsLink);
        footerLinks.Controls.Add(separator);
        footerLinks.Controls.Add(githubLink);

        footerContainer.Controls.Add(statusLabel, 0, 0);
        footerContainer.Controls.Add(footerLinks, 1, 0);
        footerPanel.Controls.Add(footerContainer);

        Controls.AddRange(new Control[] { mainPanel, footerPanel });

        Load += (s, e) =>
        {
            try
            {
                statusLabel.Text = "Loading configuration from GitHub...";
                config = ReturnAlphaBoostConfig.Load();
                LoadProfileList();
                statusLabel.Text = "Configuration loaded.";
                installRoot = TryFindRocketLeagueInstall();
                if (!string.IsNullOrEmpty(installRoot))
                {
                    pathBox.Text = installRoot;
                    statusLabel.Text = "Rocket League installation found.";
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
    }

    void LoadProfileList()
    {
        profileButtonsPanel.Controls.Clear();

        var groupedProfiles = config.Profiles
            .OrderBy(profile => profile.Key)
            .GroupBy(profile => string.IsNullOrWhiteSpace(profile.Value.Type) ? "General" : profile.Value.Type)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groupedProfiles)
        {
            var groupContainer = new TableLayoutPanel()
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 0, 12, 10),
                Padding = new Padding(0)
            };
            groupContainer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            groupContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            groupContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var groupLabel = new Label()
            {
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 6),
                Text = group.Key
            };

            var groupPanel = new FlowLayoutPanel()
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            foreach (var profileEntry in group)
            {
                var profileName = profileEntry.Key;
                var profile = profileEntry.Value;

                var profileButton = new Button()
                {
                    AutoSize = true,
                    Margin = new Padding(0, 0, 8, 8),
                    Padding = new Padding(12, 6, 12, 6),
                    Text = profile.GetDisplayText(profileName),
                    Tag = profileName
                };

                profileButton.Click += (s, e) =>
                {
                    if (s is not Button button || button.Tag is not string name)
                    {
                        return;
                    }

                    var selectedProfile = config.GetProfile(name);
                    var selectedDescription = selectedProfile?.GetDescriptionText(name) ?? name;

                    var confirm = MessageBox.Show(
                        this,
                        $"{selectedDescription}\n\nThis will overwrite files in TAGame\\CookedPCConsole.\n\nDo you really want to continue?",
                        "Confirm Overwrite",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);

                    if (confirm != DialogResult.Yes)
                    {
                        statusLabel.Text = "Operation canceled.";
                        return;
                    }

                    statusLabel.Text = selectedDescription;
                    CopyFilesIntoGameFolder(name);
                };

                groupPanel.Controls.Add(profileButton);
            }

                groupContainer.Controls.Add(groupLabel, 0, 0);
                groupContainer.Controls.Add(groupPanel, 0, 1);
                profileButtonsPanel.Controls.Add(groupContainer);
        }

        if (profileButtonsPanel.Controls.Count > 0)
        {
            statusLabel.Text = "Choose a profile button to apply files.";
        }
        else
        {
            statusLabel.Text = "No profiles were found in the config file.";
        }
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
    }

    void CopyFilesIntoGameFolder(string selectedProfileName)
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

        if (!TryCopyFromRemoteSource(mappings, cookedPath))
        {
            return;
        }

        MessageBox.Show(this, "Downloaded the configured files and copied them into TAGame\\CookedPCConsole.\n\nRocket League restart is required for the changes to take effect.", "Success - Restart Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
        statusLabel.Text = "Downloaded the configured files and copied them into TAGame\\CookedPCConsole. Rocket League restart required.";
    }

    bool TryCopyFromRemoteSource(IReadOnlyCollection<FileMapping> mappings, string cookedPath)
    {
        try
        {
            foreach (var mapping in mappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.Source))
                {
                    MessageBox.Show(this, "Each mapping requires a full URL in 'source'.", "Config Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
