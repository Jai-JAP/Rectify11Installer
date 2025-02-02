//#define FORCENORMALMODE 
//uncomment that above to disable setup mode (ie: no reboot).
//File permission problems do occur unless you run this as trusted installer

using Microsoft.Win32;
using Rectify11Installer.Controls;
using Rectify11Installer.Core;
using Rectify11Installer.Pages;
using Rectify11Installer.Win32.Rectify11;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace Rectify11Installer
{
    public partial class FrmWizard : Form
    {
        private static readonly WelcomePage WelcomePage = new();
        private static readonly EulaPage EulaPage = new();
        private static readonly InstalllOptnsPage InstallOptions = new();
        private static readonly ConfirmOperationPage ConfirmOpPage = new();
        private static readonly ProgressPage ProgressPage = new();
        private static readonly ThemeChoicePage ThemeChoice = new();
        private static readonly UninstallConfirmPage UninstallConfirmPage = new();
        private static readonly RebootPage RebootPage = new();

        private WizardPage CurrentPage;

        private bool HideCloseButton = false;
        private const int CP_NOCLOSE_BUTTON = 0x200;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                if (HideCloseButton)
                    myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        }

        //Visual studio does not understand that we assign "CurrentPage" in Navigate()
#pragma warning disable CS8618
        public FrmWizard()
#pragma warning restore CS8618
        {
            InitializeComponent();

            WelcomePage.InstallButton.Click += InstallButton_Click;
            WelcomePage.UninstallButton.Click += UninstallButton_Click;
            WelcomePage.UninstallButton.Enabled = InstallStatus.IsRectify11Installed;
            WelcomePage.VersionLabel.Click += VersionLabel_Click;
            Navigate(WelcomePage);

            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }
        int clicks = 0;
        private void VersionLabel_Click(object? sender, EventArgs e)
        {
            clicks++;
            if (clicks >= 5)
            {
                clicks = 0;
                Navigate(new DebugPage());
            }
        }
        #region Welcome Page

        private bool CheckIfUpdatesPending()
        {
            WUApiLib.ISystemInformation systemInfo = new WUApiLib.SystemInformation();

            if (systemInfo.RebootRequired)
            {
                var pg = new TaskDialogPage()
                {
                    Icon = TaskDialogIcon.ShieldErrorRedBar,

                    Text = "You cannot install Rectify11 as Windows Updates are pending.",
                    Heading = "Compatibility Error",
                    Caption = "Rectify11 Setup",
                };
                TaskDialog.ShowDialog(this, pg);

                return false;
            }
            else
            {
                return true;
            }
        }
        private void InstallButton_Click(object? sender, EventArgs e)
        {
            if (CheckIfUpdatesPending())
            {
                Navigate(EulaPage);
            }

        }

        private void UninstallButton_Click(object? sender, EventArgs e)
        {
            if (CheckIfUpdatesPending())
            {
                Navigate(UninstallConfirmPage);
            }
        }
        #endregion
        #region Navigation
        public void Navigate(WizardPage page)
        {
            CurrentPage = page;

            pnlMain.Controls.Clear();
            page.Dock = DockStyle.Fill;
            pnlMain.Controls.Add(page);

            lblTopText.Text = page.WizardTopText;
            pnlTop.Visible = page.WizardShowTitle;

            if (page == WelcomePage)
            {
                pnlBottom.Visible = false;
                navigationButton1.Visible = false;
                UpdateFrame();
            }
            else if (page == EulaPage)
            {
                BtnBack.Visible = true;
                BtnNext.Visible = true;

                BtnBack.Enabled = true;
                BtnNext.Visible = true;

                BtnBack.ButtonText = "Disagree";
                BtnNext.ButtonText = "Agree";

                navigationButton1.Visible = true;
                pnlBottom.Visible = true;
                pnlTop.Visible = true;
                UpdateFrame();
            }
            else if (page == ThemeChoice)
            {
                navigationButton1.Visible = true;

                BtnBack.Visible = true;
                BtnNext.Visible = true;

                BtnBack.Enabled = true;
                BtnNext.Enabled = true;

                BtnBack.ButtonText = "Back";
                BtnNext.ButtonText = "Next";
                navigationButton1.Visible = true;
                pnlTop.Visible = true;
                UpdateFrame();
            }
            else if (page == ConfirmOpPage)
            {
                navigationButton1.Visible = true;

                BtnBack.Visible = true;
                BtnNext.Visible = true;

                BtnBack.Enabled = true;
                BtnNext.Enabled = true;

                BtnBack.ButtonText = "Back";
                BtnNext.ButtonText = "Install";
                navigationButton1.Visible = true;
                pnlTop.Visible = true;
                UpdateFrame();
            }
            else if (page == InstallOptions)
            {
                BtnBack.Visible = true;
                BtnNext.Visible = true;

                BtnBack.Enabled = true;
                BtnNext.Enabled = true;

                BtnBack.ButtonText = "Back";
                BtnNext.ButtonText = "Next";
                pnlTop.Visible = true;
                UpdateFrame();
            }
            else if (page == ProgressPage)
            {
                pnlBottom.Visible = false;
                UpdateFrame();
            }
            else if (page == UninstallConfirmPage)
            {
                BtnBack.Visible = true;
                BtnNext.Visible = true;

                BtnBack.Enabled = true;
                BtnNext.Enabled = true;

                BtnBack.ButtonText = "Back";
                BtnNext.ButtonText = "Uninstall";
                navigationButton1.Visible = true;
                pnlBottom.Visible = true;
                UpdateFrame();
            }
            else if (page == RebootPage)
            {
                pnlBottom.Visible = false;
                navigationButton1.Visible = false;
                pnlTop.Visible = true;
                UpdateFrame();
            }

            else
            {
                BtnBack.Visible = false;
                BtnNext.Visible = false;

                BtnBack.Enabled = false;
                BtnNext.Visible = false;

                BtnBack.ButtonText = "Back";
                BtnNext.ButtonText = "Next";
                navigationButton1.Visible = false;
            }

            FixColors();
        }
        internal void Complete(RectifyInstallerWizardCompleteInstallerEnum type, bool IsInstalling, string errorDescription)
        {
            HideCloseButton = false;
            ControlBox = true;
            pnlBottom.Visible = true;
            UpdateFrame();
        }
        #endregion
        private void Form1_Shown(object sender, EventArgs e)
        {
            var buildNumber = Environment.OSVersion.Version.Build;
            try
            {
                _ = DarkMode.AllowDarkModeForWindow(this.Handle, true);
            }
            catch
            {

            }

            try
            {
                SetTitlebarColor();
            }
            catch { }

            try
            {
                //mica

                bool extend = Theme.DarkModeBool;

                if (buildNumber >= 22523)
                {
                    int micaValue = 0x02;
                    _ = DwmSetWindowAttribute(this.Handle, WindowCompositionAttribute.DWMWA_SYSTEMBACKDROP_TYPE, ref micaValue, Marshal.SizeOf(typeof(int)));
                }

                else
                {
                    int trueValue = 0x01;
                    _ = DwmSetWindowAttribute(this.Handle, WindowCompositionAttribute.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));
                }

                UpdateFrame();
            }
            catch
            {

            }

            try
            {
                var attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
                var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
                DwmSetWindowAttribute(this.Handle, attribute, ref preference, sizeof(uint));
            }
            catch
            {

            }

            FixColors();
        }
        private void UpdateFrame()
        {
            bool DarkMode = Theme.DarkModeBool;
            MARGINS m = new();

            if (DarkMode)
            {
                m.cyTopHeight = this.Height;

                //  BackColor = Color.Black;
            }
            else
            {
                BackColor = Color.White;
                // pnlTop.BackColor = Color.White;
                if (pnlBottom.Visible)
                {
                    //pnlBottom.BackColor = Color.Black;
                }
                else
                {
                    //pnlBottom.BackColor = Color.White;
                }

                //panel1.BackColor = Color.Black;

                m.cyTopHeight = pnlTop.Height;
                m.cyBottomHeight = pnlBottom.Height;

            }
            if (Environment.OSVersion.Version.Build >= 19041)
            {
                _ = DwmExtendFrameIntoClientArea(this.Handle, ref m);
            }
        }
        private void FixColors()
        {
            if (Theme.DarkModeBool)
            {
                this.ForeColor = Color.White;
                foreach (var item in GetAllControls(this))
                {
                    if (item is not RichTextBox)
                    {
                        if (item.Parent.GetType() != typeof(FakeCommandLink))
                            item.ForeColor = Color.White;
                    }
                }
            }
            else
            {
                this.ForeColor = Color.Gray;
                foreach (var item in GetAllControls(this))
                {
                    if (item.Parent.GetType() != typeof(FakeCommandLink))
                        item.ForeColor = Color.Black;
                }
            }
        }
        private IEnumerable<Control> GetAllControls(Control container)
        {
            List<Control> controlList = new();
            foreach (Control c in container.Controls)
            {
                controlList.AddRange(GetAllControls(c));
                controlList.Add(c);
            }
            return controlList;
        }
        private void SetTitlebarColor()
        {
            bool darkTheme = Theme.DarkModeBool;
            var buildNumber = Environment.OSVersion.Version.Build;

            //Enable dark title bar
            if (buildNumber < 18362)
            {
                SetPropW(this.Handle, "UseImmersiveDarkModeColors", new IntPtr(darkTheme ? 1 : 0));
            }
            else
            {
                WindowCompositionAttributeData d = new()
                {
                    Attribute = WindowCompositionAttribute.WCA_USEDARKMODECOLORS
                };
                int size = Marshal.SizeOf(typeof(bool));
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(darkTheme, ptr, false);

                d.Data = ptr;
                d.SizeOfData = size;
                _ = SetWindowCompositionAttribute(this.Handle, ref d);
            }
            if (Marshal.GetLastWin32Error() != 0) { throw new Win32Exception(); }
        }
        #region Win32

        [DllImport("uxtheme.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);
        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(IntPtr hwnd, WindowCompositionAttribute dwAttribute, ref int pvAttribute, int cbAttribute);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetPropW(IntPtr hwnd, string prop, IntPtr value);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        // Import dwmapi.dll and define DwmSetWindowAttribute in C# corresponding to the native function.
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern long DwmSetWindowAttribute(IntPtr hwnd,
                                                         DWMWINDOWATTRIBUTE attribute,
                                                         ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
                                                         uint cbAttribute);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }
        public enum WindowCompositionAttribute
        {
            // ...
            WCA_ACCENT_POLICY = 19,
            WCA_USEDARKMODECOLORS = 26,
            DWMWA_MICA_EFFECT = 1029,
            DWMWA_SYSTEMBACKDROP_TYPE = 38
            // ...
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;      // width of left border that retains its size
            public int cxRightWidth;     // width of right border that retains its size
            public int cyTopHeight;      // height of top border that retains its size
            public int cyBottomHeight;   // height of bottom border that retains its size
        };

        // The enum flag for DwmSetWindowAttribute's second parameter, which tells the function what attribute to set.
        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_WINDOW_CORNER_PREFERENCE = 33
        }

        // The DWM_WINDOW_CORNER_PREFERENCE enum for DwmSetWindowAttribute's third parameter, which tells the function
        // what value of the enum to set.
        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        [DllImport("DwmApi.dll")]
        static extern int DwmExtendFrameIntoClientArea(
         IntPtr hwnd,
         ref MARGINS pMarInset);
        #endregion
        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (CurrentPage == EulaPage || CurrentPage == UninstallConfirmPage)
            {
                //We have disagreed with the license or clicked back on uninstall page
                Navigate(WelcomePage);
            }
            else if (CurrentPage == ConfirmOpPage)
            {
                //The user clicked on "Back" when on the confirm install/uninstall page
                Navigate(ThemeChoice);
            }
            else if (CurrentPage == ThemeChoice)
            {
                //The user clicked on "Back" when on the theme selection page
                Navigate(InstallOptions);
            }
            else if (CurrentPage == InstallOptions)
            {
                //The user clicked on "Back" when on the confirm install/uninstall page
                Navigate(EulaPage);
            }
        }
        private async void BtnNext_Click(object sender, EventArgs e)
        {
            if (CurrentPage == EulaPage)
            {
                //Show install options page
                Navigate(InstallOptions);

            }
            else if (CurrentPage == InstallOptions)
            {
                //We are about to install it
                Navigate(ThemeChoice);

            }
            else if (CurrentPage == ThemeChoice)
            {
                //We are about to install it
                Navigate(ConfirmOpPage);

                //Change text
                ConfirmOpPage.TextLable.Text = "You are about to do the following:\n\nInstall Rectify11 on this Windows 11 Installation\n\nPlease close everything and save your work as a reboot will be required at the end of the installation.";
            }
            else if (CurrentPage == ConfirmOpPage || CurrentPage == UninstallConfirmPage)
            {
                var oldPage = CurrentPage;
                //Install/Uninstall Rectify11
                Navigate(ProgressPage);

                //Normal mode
#if FORCENORMALMODE
                IRectifyInstaller installer = new RectifyInstaller();
                installer.SetParentWizard(new RectifyInstallerWizard(this, ProgressPage));

                HideCloseButton = true;
                ControlBox = false;
                pnlBottom.Visible = false;
                UpdateFrame();

                if (oldPage == ConfirmOpPage)
                {
                    IRectifyInstalllerInstallOptions options = InstallOptions;
                    var thread = new Thread(delegate ()
                    {
                        installer.Install(options);
                    });
                    thread.Start();
                }
                else if (oldPage == UninstallConfirmPage)
                {
                    IRectifyInstalllerUninstallOptions options = UninstallConfirmPage;
                    var thread = new Thread(delegate ()
                    {
                        installer.Uninstall(options);
                    });
                    thread.Start();
                }
                else
                {
                    var pg = new TaskDialogPage()
                    {
                        Icon = TaskDialogIcon.ShieldErrorRedBar,

                        Text = "An internal setup error has occured. This is caused by misconfiguration in the setup wizard. Error:\nUnknown CurrentPage Value: " + CurrentPage.GetType().ToString() + "\nOld Page: " + oldPage.GetType().ToString(),
                        Heading = "Compatibility Error",
                        Caption = "Rectify11 Setup",
                    };
                    TaskDialog.ShowDialog(this, pg);
                }
#else
                var wizard = new RectifyInstallerWizard(this, ProgressPage);
                wizard.SetProgress(0);
                wizard.SetProgressText("Copying Files");

                try
                {
                    if (!Directory.Exists(@"C:\Windows\Rectify11\"))
                    {
                        Directory.CreateDirectory(@"C:\Windows\Rectify11\");
                    }
                }
                catch (Exception ex)
                {
                    wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Fail, true, @"Unable to create C:\Windows\Rectify11 folder:\n" + ex.ToString());
                }
                string iniPath = @"C:\Windows\Rectify11\work.ini";

                //delete old files
                if (File.Exists(iniPath))
                    File.Delete(iniPath);

                if (oldPage == ConfirmOpPage)
                {
                    IRectifyInstalllerInstallOptions options = InstallOptions;
                    IRectifyInstalllerThemeOptions themeoptions = ThemeChoice;
                    //install

                    try
                    {
                        IniFile f = new IniFile(iniPath);
                        f.Write("InstallEP", options.ShouldInstallExplorerPatcher.ToString());
                        f.Write("InstallASDF", options.ShouldInstallASDF.ToString());
                        f.Write("InstallWP", options.ShouldInstallWallpaper.ToString());
                        f.Write("InstallVer", options.ShouldInstallWinver.ToString());
                        f.Write("DoSafeInstall", options.DoSafeInstall.ToString());

                        f.Write("DarkTheme", themeoptions.Dark.ToString());
                        f.Write("LightTheme", themeoptions.Light.ToString());
                        f.Write("BlackTheme", themeoptions.Black.ToString());
                        f.Write("Mode", "Install");
                    }
                    catch (Exception ex)
                    {
                        wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Fail, true, @"Failed to create:" + iniPath + "\n" + ex.ToString());
                        return;
                    }
                    try
                    {
                        SetupMode.Enter();
                    }
                    catch (Exception ex)
                    {
                        wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Fail, true, @"Failed to enter setup mode:\n" + ex.ToString());
                        return;
                    }

                    // install most apps
                    wizard.SetProgressText("Preparing Files...");
                    string tempfldr = @"C:\Windows\Rectify11";
                    File.WriteAllBytes(Path.Combine(tempfldr, "7za.exe"), Properties.Resources._7za_exe);
                    File.WriteAllBytes(Path.Combine(tempfldr, "files.7z"), Properties.Resources.files_7z);
                    if (Directory.Exists(tempfldr + @"\files"))
                    {
                        Directory.Delete(tempfldr + @"\files", true);
                    }
                    await Task.Run(() => PatcherHelper.SevenzExtract(Path.Combine(tempfldr, "7za.exe"), Path.Combine(tempfldr, "files"), Path.Combine(tempfldr, "files.7z"), tempfldr));

                    // eh
                    wizard.SetProgressText("Installing theme");
                    if (Directory.Exists(@"C:\Windows\Resources\Themes\rectify11"))
                    {
                        Directory.Delete(@"C:\Windows\Resources\Themes\rectify11", true);
                    }
                    Directory.Move(tempfldr + @"\files\themes\rectify11", @"C:\Windows\Resources\Themes\rectify11");
                    File.Copy(tempfldr + @"\files\themes\black.theme", @"C:\Windows\Resources\Themes\black.theme", true);
                    File.Copy(tempfldr + @"\files\themes\blacknonhighcontrastribbon.theme", @"C:\Windows\Resources\Themes\blacknonhighcontrastribbon.theme", true);
                    File.Copy(tempfldr + @"\files\themes\darkcolorized.theme", @"C:\Windows\Resources\Themes\darkcolorized.theme", true);
                    File.Copy(tempfldr + @"\files\themes\darkrectified.theme", @"C:\Windows\Resources\Themes\darkrectified.theme", true);
                    File.Copy(tempfldr + @"\files\themes\lightrectified.theme", @"C:\Windows\Resources\Themes\lightrectified.theme", true);
                    var basee = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
                    var themes = basee.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ThemeManager", RegistryKeyPermissionCheck.ReadWriteSubTree);
                    if ((themes != null) && (!File.Exists(@"C:\Windows\system32\SecureUxTheme.dll")))
                    {
                        if (themeoptions.Light)
                            themes.SetValue("DllName", @"%SystemRoot%\resources\Themes\rectify11\Aero.msstyles", RegistryValueKind.String);
                        else if (themeoptions.Dark)
                            themes.SetValue("DllName", @"%SystemRoot%\resources\Themes\rectify11\Dark.msstyles", RegistryValueKind.String);
                        else if (themeoptions.Black)
                            themes.SetValue("DllName", @"%SystemRoot%\resources\Themes\rectify11\Black.msstyles", RegistryValueKind.String);
                    }
                    themes = basee.OpenSubKey(@"Control Panel\Desktop", RegistryKeyPermissionCheck.ReadWriteSubTree);
                    if (themes != null)
                    {
                        themes.SetValue(@"WallpaperStyle", 10.ToString());
                        themes.SetValue(@"TileWallpaper", 0.ToString());
                        if ((themeoptions.Dark) || (themeoptions.Black))
                            themes.SetValue(@"Wallpaper", @"%windir%\Web\Wallpaper\Rectify11\img19.jpg");
                        else if (themeoptions.Light)
                            themes.SetValue(@"Wallpaper", @"%windir%\Web\Wallpaper\Rectify11\img0.jpg");
                    }
                    basee.Close();
                    if (!Directory.Exists(@"C:\Windows\contextmenus"))
                        Directory.Move(tempfldr + @"\files\contextmenus", @"C:\Windows\contextmenus");
                    await Task.Run(() => PatcherHelper.RunAsyncCommands("shell.exe", "-r -i -s", @"C:\Windows\contextmenus\nilesoft-shell-1.6"));
                    await Task.Run(() => PatcherHelper.RunAsyncCommands("powercfg.exe", "-change -monitor-timeout-ac 0", @"C:\Windows\system32"));
                    await Task.Run(() => PatcherHelper.RunAsyncCommands("powercfg.exe", "-change -monitor-timeout-dc 0", @"C:\Windows\system32"));
                    await Task.Run(() => PatcherHelper.RunAsyncCommands("schtasks.exe", "/create /tn mfe /xml " + tempfldr + @"\files\mfe.xml", @"C:\Windows\system32"));
                    await Task.Run(() => PatcherHelper.RunAsyncCommands("schtasks.exe", "/create /tn asdf /xml " + tempfldr + @"\files\asdf.xml", @"C:\Windows\system32"));
                    if (!Directory.Exists(@"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\3.1.27"))
                        await Task.Run(() => PatcherHelper.RunAsyncCommands(tempfldr + @"\files\3.1core.exe", "/install /quiet /norestart", tempfldr));
                    if (!Directory.Exists(@"C:\Windows\MicaForEveryone"))
                        Directory.Move(tempfldr + @"\files\MicaForEveryone", @"C:\Windows\MicaForEveryone");
                    if (themeoptions.Light)
                        File.Copy(tempfldr + @"\files\light.conf", @"C:\Windows\MicaForEveryone\MicaForEveryone.conf");
                    else if (themeoptions.Dark)
                        File.Copy(tempfldr + @"\files\dark.conf", @"C:\Windows\MicaForEveryone\MicaForEveryone.conf");
                    else if (themeoptions.Black)
                        File.Copy(tempfldr + @"\files\black.conf", @"C:\Windows\MicaForEveryone\MicaForEveryone.conf");
                    string[] files = Directory.GetFiles(tempfldr + @"\files\segvar");
                    Shell32.Shell shell = new();
                    Shell32.Folder fontFolder = shell.NameSpace(0x14);
                    var basekey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                    var r11 = basekey.OpenSubKey(@"Software\Rectify11", RegistryKeyPermissionCheck.ReadWriteSubTree);
                    if (r11 != null)
                    {
                        var t = r11.GetValue("FontsInstalled");
                        if (t == null)
                        {
                            foreach (string file in files)
                            {
                                fontFolder.CopyHere(file, 4);
                            }
                            r11.SetValue("FontsInstalled", 1, RegistryValueKind.DWord);
                        }
                    }
                    await Task.Run(() => PatcherHelper.RunAsyncCommands("reg.exe", "import " + tempfldr + @"\files\FIX.reg", tempfldr));
                    await Task.Run(() => PatcherHelper.RunAsyncCommands("rundll32.exe", "setupapi,InstallHinfSection DefaultInstall 132 " + tempfldr + @"\files\cursors\install.inf", tempfldr));
                    await Task.Run(() => PatcherHelper.RunAsyncCommands("rundll32.exe", "setupapi,InstallHinfSection DefaultInstall 132 " + tempfldr + @"\files\cursors\linstall.inf", tempfldr));
                    await Task.Run(() => PatcherHelper.RunAsyncCommands("rundll32.exe", "setupapi,InstallHinfSection DefaultInstall 132 " + tempfldr + @"\files\cursors\xlinstall.inf", tempfldr));
                    TaskDialogPage pg;
                    if (File.Exists(@"C:\Windows\system32\SecureUxTheme.dll"))
                    {
                        pg = new TaskDialogPage()
                        {
                            Icon = TaskDialogIcon.Information,
                            Text = "Since you have SecureUxTheme installed, you have to manually apply the theme using ThemeTool.",
                            Heading = "Last step",
                            Caption = "Info",
                        };
                        TaskDialog.ShowDialog(this, pg);
                    }
                    else if (!File.Exists(@"C:\Program Files (x86)\UltraUXThemePatcher\uninstall.exe"))
                    {
                        Process process = Process.Start(tempfldr + @"\files\UltraUXThemePatcher_4.3.4.exe");
                        await process.WaitForExitAsync();
                    }

                    if (options.ShouldInstallExplorerPatcher)
                    {
                        Process process = Process.Start(tempfldr + @"\files\ep_setup.exe");
                        await process.WaitForExitAsync();
                        await PatcherHelper.RunAsyncCommands("regsvr32.exe", "/s \"%PROGRAMFILES%\\ExplorerPatcher\\ExplorerPatcher.amd64.dll\"", tempfldr);
                        await Task.Run(() => PatcherHelper.RunAsyncCommands("reg.exe", "import " + tempfldr + @"\files\ep.reg", tempfldr));
                        // todo: add start menu registry depending on user choice
                    }
                    RebootPage.Start();
                    Navigate(RebootPage);
                }
                else if (oldPage == UninstallConfirmPage)
                {
                    IRectifyInstalllerUninstallOptions options = UninstallConfirmPage;
                    try
                    {
                        IniFile f = new IniFile(iniPath);
                        f.Write("RemoveEP", options.RemoveExplorerPatcher.ToString());
                        f.Write("RemoveThemes", options.RemoveThemesAndThemeTool.ToString());
                        f.Write("RemoveWP", options.RestoreWallpapers.ToString());
                        f.Write("Mode", "Uninstall");
                    }
                    catch (Exception ex)
                    {
                        wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Fail, true, @"Failed to create:" + iniPath + "\n" + ex.ToString());
                        return;
                    }
                    try
                    {
                        SetupMode.Enter();
                    }
                    catch (Exception ex)
                    {
                        wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Fail, true, @"Failed to enter setup mode:\n" + ex.ToString());
                        return;
                    }
                    if (File.Exists(@"C:\Program Files (x86)\UltraUXThemePatcher\uninstall.exe"))
                    {
                        var process = Process.Start(@"C:\Program Files (x86)\UltraUXThemePatcher\uninstall.exe");
                        process.WaitForExit();
                    }

                    // ultrauxthemepatcher uninstall wizard is just E
                    // RebootPage.Start();
                    Navigate(RebootPage);
                }
                else
                {
                    var pg = new TaskDialogPage()
                    {
                        Icon = TaskDialogIcon.ShieldErrorRedBar,

                        Text = "An internal setup error has occured. This is caused by misconfiguration in the setup wizard. Error:\nUnknown CurrentPage Value: " + CurrentPage.GetType().ToString() + "\nOld Page: " + oldPage.GetType().ToString(),
                        Heading = "Compatibility Error",
                        Caption = "Rectify11 Setup",
                    };
                    TaskDialog.ShowDialog(this, pg);
                }
#endif
            }
        }
        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            switch (e.Category)
            {
                case UserPreferenceCategory.General:
                    Form1_Shown(this, new EventArgs());
                    lblTopText.Invalidate();
                    break;
            }
        }

        private void NavigationButton1_Click(object sender, EventArgs e)
        {
            BtnBack_Click(sender, e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (HideCloseButton)
                e.Cancel = true;
            base.OnClosing(e);
        }

        private void FrmWizard_FormClosing(object sender, FormClosingEventArgs e)
        {
            TopMost = false;
        }

        private void FrmWizard_Load(object sender, EventArgs e)
        {

        }

        private void pnlMain_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}