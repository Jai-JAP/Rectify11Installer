﻿using Microsoft.Win32;
using Rectify11Installer.Core;
using Rectify11Installer.Win32;
using Rectify11Installer.Win32.Rectify11;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Rectify11Installer
{
    public class RectifyInstaller : IRectifyInstaller
    {
        private IRectifyInstallerWizard? Wizard;
        private bool IsInstalling = true;
        #region Interface implementation
        public void Install(IRectifyInstalllerInstallOptions options)
        {
            IsInstalling = true;
            if (Wizard == null)
            {
                throw new Exception("SetParentWizard() in IRectifyInstaller was not called!");
            }

            try
            {
                InstallStatus.IsRectify11Installed = true;

                #region Setup
                Wizard.SetProgress(0);
                Wizard.SetProgressText("Initializing...");
                var backupDir = @"C:\Windows\Rectify11\Backup";
                #endregion

                var patches = Patches.GetAll();


                int i = 0;
                BeginXml();
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
                                var r = Application.StartupPath + @"\files\" + patch.Resource;
                                if (string.IsNullOrEmpty(patch.Resource))
                                    r = null;

                                //This is where we mod the file
                                if (!PatcherHelper.ReshackAddRes(Application.StartupPath + @"/files/ResourceHacker.exe",
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
                CommitXml();

                Wizard.SetProgress(0);
                Wizard.SetProgressText("Installing Apps");
                //This is commented out as it's broken
                //if (options.ShouldInstallWinver)
                //{
                //    var pkg = GetAMD64Package("microsoft-windows-winver");
                //    if (pkg != null)
                //    {
                //        if (!File.Exists("C:/Windows/Rectify11/winver.exe"))
                //            File.Copy("C:/Windows/System32/winver.exe", "C:/Windows/Rectify11/winver.exe");

                //        ReplaceFileInPackage(pkg, @"C:\Windows\System32\winver.exe", Application.StartupPath + @"\files\winver.exe");
                //    }
                //    else
                //    {
                //        _Wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Fail, IsInstalling, "Cannot find WinVer SxS package.");
                //        return;
                //    }
                //}

                //if (options.ShouldInstallWallpaper)
                //{
                //    var pkg = GetAMD64Package("microsoft-windows-s..l-wallpaper-windows");
                //    if (pkg != null)
                //    {
                //        if (!File.Exists("C:/Windows/Rectify11/img0.jpg"))
                //            File.Copy("C:/Windows/Web/Wallpaper/Windows/img0.jpg", "C:/Windows/Rectify11/img0.jpg");
                //        if (!File.Exists("C:/Windows/Rectify11/img19.jpg"))
                //            File.Copy("C:/Windows/Web/Wallpaper/Windows/img19.jpg", "C:/Windows/Rectify11/img19.jpg");

                //        ReplaceFileInPackage(pkg, @"C:\Windows\Web\Wallpaper\Windows\img0.jpg", Application.StartupPath + @"\files\img0.jpg");
                //        ReplaceFileInPackage(pkg, @"C:\Windows\Web\Wallpaper\Windows\img19.jpg", Application.StartupPath + @"\files\img19.jpg");
                //    }
                //    else
                //    {
                //        _Wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Fail, IsInstalling, "Cannot find wallper SxS package.");
                //        return;
                //    }
                //}

                Wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Success, IsInstalling, "");
                return;
            }
            catch (Exception ex)
            {
                Wizard.CompleteInstaller(RectifyInstallerWizardCompleteInstallerEnum.Fail, IsInstalling, ex.ToString());
            }
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
                BeginXml();
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
                CommitXml();

                Wizard.SetProgressText("Restoring old wallpapers and Winver");
                Wizard.SetProgress(0);


                //This is commented out as it's broken
                if (options.RestoreWallpapers)
                {
                    var pkg = GetAMD64Package("microsoft-windows-winver");
                    if (pkg != null)
                    {
                        //ReplaceFileInPackage(pkg, @"C:\Windows\Web\Wallpaper\Windows\img0.jpg", "C:/Windows/Rectify11/img0.jpg");
                        ///ReplaceFileInPackage(pkg, @"C:\Windows\Web\Wallpaper\Windows\img19.jpg", "C:/Windows/Rectify11/img19.jpg");
                    }
                }

                if (options.RestoreWinver)
                {
                    var pkg = GetAMD64Package("microsoft-windows-s..l-wallpaper-windows");
                    if (pkg != null)
                    {
                        //ReplaceFileInPackage(pkg, @"C:\Windows\System32\winver.exe", "C:/Windows/Rectify11/winver.exe");
                    }
                }

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
        private string? PoqExecXml;
        private void BeginXml()
        {
            PoqExecXml = "<?xml version='1.0' encoding='utf-8'?>\n";
            PoqExecXml += "<PendingTransaction Version=\"3.1\">";
            PoqExecXml += "    <POQ postAction=\"reboot\">";
        }
        private void CommitXml()
        {
            PoqExecXml += "    </POQ>";
            PoqExecXml += "</PendingTransaction>";
            File.WriteAllText(@"C:\Windows\Rectify11\pending.xml", PoqExecXml);

            RegistryKey? s = Registry.LocalMachine.OpenSubKey("SYSTEM", true);
            if (s != null)
            {
                RegistryKey? c = s.OpenSubKey("CurrentControlSet", true);
                if (c != null)
                {
                    RegistryKey? cc = c.OpenSubKey("Control", true);
                    if (cc != null)
                    {
                        RegistryKey? sessionManager = cc.OpenSubKey("Session Manager", true);
                        if (sessionManager != null)
                        {
                            sessionManager.SetValue("SetupExecute", @"C:\Windows\System32\poqexec.exe /display_progress \??\C:\Windows\Rectify11\pending.xml");
                            sessionManager.Close();
                        }
                        else
                        {
                            throw new Exception("Failed to open key: Session Manager");
                        }
                        cc.Close();
                    }
                    else
                    {
                        throw new Exception("Failed to open key: Control");
                    }
                    c.Close();
                }
                else
                {
                    throw new Exception("Failed to open key: CurrentControlSet");
                }
                s.Close();
            }
            else
            {
                throw new Exception("Failed to open key: SYSTEM");
            }
        }
        private void ReplaceFileInPackage(Package usr, string hardlinkTarget, string source)
        {
            string dllName = Path.GetFileName(source);
            var WinSxSFilePath = usr.Path + @"\" + dllName;
            WinSxSFilePath = WinSxSFilePath.Replace(@"C:\Windows\", @"\SystemRoot\WinSxS\");

            //Rename old hardlink
            try
            {
                if (File.Exists(hardlinkTarget + ".bak"))
                    File.Delete(hardlinkTarget + ".bak");
            }
            catch { }
            File.Move(hardlinkTarget, hardlinkTarget + ".bak");

            //Delete old hardlink
            ScheduleForDeletion(hardlinkTarget + ".bak");

            //rename old file
            File.Move(WinSxSFilePath, WinSxSFilePath + ".bak");

            //copy new file over
            File.Move(source, WinSxSFilePath, true);

            //create hardlink
            if (!NativeMethods.CreateHardLinkA(hardlinkTarget, WinSxSFilePath, IntPtr.Zero))
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
                if (!NativeMethods.MoveFileEx(path, null, NativeMethods.MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT))
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
