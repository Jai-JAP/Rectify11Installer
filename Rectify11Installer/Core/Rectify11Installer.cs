﻿using Microsoft.Win32;
using Rectify11Installer.Core;
using Rectify11Installer.Win32.Rectify11;
using System.ComponentModel;

namespace Rectify11Installer
{
    public class RectifyInstaller : IRectifyInstaller
    {
        private IRectifyInstallerWizard? Wizard;
        private bool IsInstalling = true;
        #region Interface implementation
        public async void Install(IRectifyInstalllerInstallOptions options, IRectifyInstalllerThemeOptions themeOptions)
        {
            IsInstalling = true;
            if (Wizard == null)
            {
                throw new Exception("SetParentWizard() in IRectifyInstaller was not called!");
            }

            try
            {
                InstallStatus.IsRectify11Installed = true;

                string tempfldr = @"C:\Windows\Rectify11";
                #region Setup
                Wizard.SetProgress(0);
                Wizard.SetProgressText("Initializing...");
                var backupDir = @"C:\Windows\Rectify11\Backup";
                File.Copy(Application.ExecutablePath, @"C:\Windows\Rectify11\rectify11setup.exe", true);
                #endregion

                var patches = Patches.GetAll();
                int i = 0;
                foreach (var item in patches)
                {
                    if (item.DisableOnSafeMode && options.DoSafeInstall)
                    {

                    }
                    else
                    {
                        //get the package

                        var usr = GetAMD64Package(item.WinSxSPackageName);
                        if (usr == null)
                        {
                            Logger.Warn("Cannot find package: " + item.WinSxSPackageName + ", which is needed to patch " + item.DllName);
                            continue;
                        }

                        Wizard.SetProgress(i * 100 / patches.Length);
                        Wizard.SetProgressText("Patching file: " + item.DllName);

                        var WinSxSFilePath = usr.Path + @"\" + item.DllName;
                        string WinsxsDir = Path.GetFileName(usr.Path);
                        string file = WinsxsDir + "/" + item.DllName;

                        string fileProper = "C:/Windows/Rectify11/Tmp/" + file; //relative path to the file location
                        string backupDirW = backupDir + "/" + WinsxsDir; //backup dir where the file is located at

                        if (!File.Exists(WinSxSFilePath))
                        {
                            Logger.Warn("Cannot find path in package: " + WinSxSFilePath + ", which is needed to patch " + item.DllName);
                            continue;
                        }

                        if (!File.Exists(item.Systempath))
                        {
                            Logger.Warn("Hardlink target in package: " + item.WinSxSPackageName + ", which is not found at" + item.Systempath);
                            continue;
                        }

                        Directory.CreateDirectory("C:/Windows/Rectify11/Tmp/" + WinsxsDir);
                        File.Copy(WinSxSFilePath, fileProper, true);

                        Directory.CreateDirectory(backupDirW);

                        if (!File.Exists(backupDirW + "/" + item.DllName))
                        {
                            File.Copy(WinSxSFilePath, backupDirW + "/" + item.DllName, true);

                            //for now: we will only patch files that don't exist in the backup directory
                            //this is to save time during developent and avoid overwriting orginal files with modified ones

                            foreach (var patch in item.PatchInstructions)
                            {
                                var r = tempfldr + @"\files\" + patch.Resource;
                                if (string.IsNullOrEmpty(patch.Resource))
                                    r = null;

                                //This is where we mod the file
                                if (!PatcherHelper.ReshackAddRes(tempfldr + @"\files\ResourceHacker.exe",
                                    fileProper,
                                    fileProper,
                                    patch.Action, //"addoverwrite",
                                    r,
                                    patch.GroupAndLocation))//ICONGROUP,1,0
                                {
                                    Wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Fail, IsInstalling, $"Resource hacker failed at DLL: {item.DllName}\nCommand line:\n" + PatcherHelper.LastCmd + "\nSee installer.log for more information");
                                    return;
                                }
                            }

                            ReplaceFileInPackage(usr, item.Systempath, fileProper);

                            i++;
                        }
                    }
                }

                Wizard.SetProgress(0);
                Wizard.SetProgressText("Installing Optional features");
                if (options.ShouldInstallWinver)
                {   //for some reason, %windir% doesnt work, so, using C:\Windows instead
                    PatcherHelper.TakeOwnership(@"C:\Windows\System32\winver.exe", false);
                    PatcherHelper.GrantFullControl(@"C:\Windows\System32\winver.exe", "Everyone", false);
                    File.Delete(@"C:\Windows\System32\winver.exe");
                    File.Copy(tempfldr + @"\files\winver.exe", @"C:\Windows\System32\winver.exe", true);
                }
                if (options.ShouldInstallWallpaper)
                {
                    if (Directory.Exists(@"C:\Windows\Web\Wallpaper\Rectify11"))
                        Directory.Delete(@"C:\Windows\Web\Wallpaper\Rectify11", true);
                    Directory.Move(tempfldr + @"\files\rectify11_wallpapers", @"C:\Windows\Web\Wallpaper\Rectify11");
                }
                var basee = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                var themes = basee.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce", RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (themes != null)
                {
                    if (themeOptions.Light)
                        themes.SetValue("Rectify11", @"C:\Windows\Resources\Themes\lightrectified.theme", RegistryValueKind.String);
                    else if (themeOptions.Dark)
                        themes.SetValue("Rectify11", @"C:\Windows\Resources\Themes\darkrectified.theme", RegistryValueKind.String);
                    else if (themeOptions.Black)
                        themes.SetValue("Rectify11", @"C:\Windows\Resources\Themes\blacknonhighcontrastribbon.theme", RegistryValueKind.String);
                }
                basee.Close();

                if (options.ShouldInstallASDF)
                {
                    File.Copy(tempfldr + @"\files\AccentColorizer.exe", @"C:\Windows\AccentColorizer.exe", true);
                }
                Wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Success, IsInstalling, "");
                Directory.Delete(tempfldr + @"\files", true);
                File.Delete(tempfldr + @"\files.7z");
                File.Delete(tempfldr + @"\7za.exe");
                return;
            }
            catch (Exception ex)
            {
                Wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Fail, IsInstalling, ex.ToString());
            }
        }
        private void TakeOwnership(string path, bool recursive)
        {
            if (path.ToLower().StartsWith(@"c:\windows\systemresources"))
            {
                ;
            }
            _ = PatcherHelper.TakeOwnership(path, recursive);
            _ = PatcherHelper.GrantFullControl(path, "Administrators", recursive);
            _ = PatcherHelper.GrantFullControl(path, "SYSTEM", recursive);
            // _ = PatcherHelper.GrantFullControl(path, "Everyone");
        }
        public void Uninstall(IRectifyInstalllerUninstallOptions options)
        {
            IsInstalling = false;
            if (Wizard == null)
            {
                throw new Exception("SetParentWizard() in IRectifyInstaller was not called!");
            }

            try
            {
                #region Setup
                Wizard.SetProgressText("Taking ownership of system files");
                Wizard.SetProgress(1);
                var backupDir = @"C:\Windows\Rectify11\Backup";
                #endregion
                var patches = Patches.GetAll();
                int i = 0;
                foreach (var item in patches)
                {
                    Wizard.SetProgress(i * 100 / patches.Length);
                    Wizard.SetProgressText("Restoring file: " + item.DllName);

                    var usr = GetAMD64Package(item.WinSxSPackageName);
                    if (usr == null)
                    {
                        Logger.Warn("Cannot find package: " + item.WinSxSPackageName + ", which is needed to patch " + item.DllName);
                    }
                    else
                    {
                        var backupFilePath = backupDir + @"\" + Path.GetFileName(usr.Path) + @"\" + item.DllName;

                        if (!File.Exists(backupFilePath))
                        {
                            Logger.Warn("File backup path does not exist: " + backupFilePath);
                        }
                        else
                        {
                            ReplaceFileInPackage(usr, item.Systempath, backupFilePath);
                        }
                    }
                    i++;
                }

                Wizard.SetProgressText("Restoring old wallpapers and Winver");
                Wizard.SetProgress(0);

                if (options.RestoreWallpapers)
                {
                    Directory.Delete(@"C:\Windows\Web\Wallpaper\Rectify11", true);
                }



                //if (options.RestoreWinver)
                //{
                //    var pkg = GetAMD64Package("microsoft-windows-winver");
                //    if (pkg != null)
                //    {
                //        //ReplaceFileInPackage(pkg, @"C:\Windows\System32\winver.exe", "C:/Windows/Rectify11/winver.exe");
                //    }
                //}

                Wizard.SetProgress(99);
                Wizard.SetProgressText("Removing old backups");
                //Directory.Delete(@"C:\Windows\Rectify11", true);

                InstallStatus.IsRectify11Installed = false;
                Wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Success, IsInstalling, "");
                return;
            }
            catch (Exception ex)
            {
                Wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Fail, IsInstalling, ex.ToString());
            }
        }
        public void SetParentWizard(IRectifyInstallerWizard wiz)
        {
            Wizard = wiz;
        }
        #endregion
        #region Private methods
        private void ReplaceFileInPackage(Package usr, string hardlinkTarget, string source)
        {
            string dllName = Path.GetFileName(source);
            var WinSxSFilePath = usr.Path + @"\" + dllName;

            //Rename old hardlink
            try
            {
                if (File.Exists(hardlinkTarget + ".bak"))
                    File.Delete(hardlinkTarget + ".bak");
            }
            catch { }
            File.Move(hardlinkTarget, hardlinkTarget + ".bak", true);

            //Delete old hardlink
            ScheduleForDeletion(hardlinkTarget + ".bak");

            //rename old file
            File.Move(WinSxSFilePath, WinSxSFilePath + ".bak", true);

            //copy new file over
            File.Move(source, WinSxSFilePath, true);

            //create hardlink
            if (!Win32.NativeMethods.CreateHardLinkA(hardlinkTarget, WinSxSFilePath, IntPtr.Zero))
            {
                if (Wizard != null)
                    Wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Fail, IsInstalling, "CreateHardLinkW() failed: " + new Win32Exception().Message);
                throw new Exception("failure while calling MoveFileEx()");
            }

            ScheduleForDeletion(WinSxSFilePath + ".bak");
        }
        private void ScheduleForDeletion(string path)
        {
            if (!File.Exists(path))
                return;

            //schedule .bak for deletion
            try
            {
                File.Delete(path);
            }
            catch
            {
                //delete it first
                if (!Win32.NativeMethods.MoveFileEx(path, null, Win32.NativeMethods.MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT))
                {
                    if (Wizard != null)
                        Wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Fail, IsInstalling, "MoveFileEx() failed: " + new Win32Exception().Message);
                    throw new Exception("failure while calling MoveFileEx()");
                }
            }
        }
        private Package? GetAMD64Package(string name)
        {
            var usercpl = FindPackage(name);
            if (usercpl.Count == 0)
            {
                return null;
            }
            foreach (var item in usercpl)
            {
                if (item.Arch == PackageArch.Amd64)
                {
                    return item;
                }
            }
            return null;
        }
        private List<Package> FindPackage(string name)
        {
            List<Package> p = new List<Package>();
            var build = Environment.OSVersion.Version.Build.ToString();
            foreach (var item in Directory.GetDirectories(@"C:\Windows\WinSxS\"))
            {
                if (item.Contains(build) && item.Contains(name + "_"))
                {
                    var path = item.Replace(@"C:\Windows\WinSxS\", "");
                    if (path.StartsWith("amd64_"))
                    {
                        p.Add(new Package(item, PackageArch.Amd64));
                    }
                    else if (path.StartsWith("wow64_"))
                    {
                        p.Add(new Package(item, PackageArch.Wow64));
                    }
                }
            }

            return p;
        }
        #endregion
    }
}
