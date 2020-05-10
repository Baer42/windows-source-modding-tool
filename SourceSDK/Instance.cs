﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace source_modding_tool.SourceSDK
{
    class Instance
    {
        private Control parent = null;
        private Launcher launcher = null;
        public Process modProcess = null;

        public Instance(Launcher launcher, Control parent)
        {
            this.launcher = launcher;
            this.parent = parent;
        }

        internal void Stop()
        {
            if (modProcess != null)
            {
                modProcess.Kill();
                modProcess = null;
            }
        }

        public void Command(string command)
        {
            if (modProcess == null)
                return;

            string gamePath = launcher.GetCurrentGame().installPath;
            string modPath = launcher.GetCurrentMod().installPath;

            Debug.Write(modPath);

            string exePath = string.Empty;

            foreach (string file in Directory.GetFiles(gamePath))
            {
                if (new FileInfo(file).Extension == ".exe")
                {
                    exePath = file;
                    break;
                }
            }

            Program.SetParent(modProcess.MainWindowHandle, IntPtr.Zero);
            Program.ShowWindow((int)modProcess.MainWindowHandle, 0);

            Process process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = "-hijack " + command;
            process.Start();
            process.EnableRaisingEvents = true;
            process.WaitForInputIdle();
            Program.ShowWindow((int)modProcess.MainWindowHandle, 9);
            Program.SetParent(modProcess.MainWindowHandle, parent.Handle);
        }

        public void Resize()
        {
            if (modProcess != null)
            {
                //Command("-width " + parent.Width + " -height " + parent.Height);
                //File.WriteAllText(sourceSDK.GetModPath() + "\\cfg\\cmd.cfg", "mat_setvideomode " + parent.Width + " " + parent.Height + " 1");
                Program.MoveWindow(modProcess.MainWindowHandle, 0, 0, parent.Width, parent.Height, true);
            }
        }

        public void KillExistant()
        {
            foreach (var process in Process.GetProcessesByName("hl2.exe"))
            {
                process.Kill();
            }
        }

        public Process Start()
        {
            return Start("");
        }

        public Process Start(string command)
        {
            KillExistant();

            Game game = launcher.GetCurrentGame();
            string modPath = launcher.GetCurrentMod().installPath;
            string modFolder = new DirectoryInfo(modPath).Name;

            string exePath = launcher.GetCurrentGame().getExePath();

            modProcess = new Process();
            modProcess.StartInfo.FileName = exePath;

            switch(game.engine)
            {
                case Engine.SOURCE:
                    modProcess.StartInfo.Arguments = "-game \"" +
                    modPath +
                    "\" -windowed -noborder -novid 0" +
                    " -width " + parent.Width +
                    " -height " + parent.Height +
                    " -multirun " +
                    command;
                    break;
                case Engine.SOURCE2:
                    modProcess.StartInfo.Arguments = " -game " + modFolder + " -windowed -noborder -vr_enable_fake_vr_test" +
                    " -width " + parent.Width +
                    " -height " + parent.Height +
                    " " + command;
                    break;
            }
            modProcess.Start();

            AttachProcessTo(modProcess, parent);

            return modProcess;
        }

        public Process StartFullScreen()
        {
            return StartFullScreen("");
        }

        public Process StartFullScreen(string command)
        {
            Game game = launcher.GetCurrentGame();
            string modPath = launcher.GetCurrentMod().installPath;
            string modFolder = new DirectoryInfo(modPath).Name;

            Debug.Write(modPath);

            string exePath = launcher.GetCurrentGame().getExePath();

            modProcess = new Process();
            modProcess.StartInfo.FileName = exePath;

            switch (game.engine)
            {
                case Engine.SOURCE:
                    modProcess.StartInfo.Arguments = "-game \"" +
                    modPath +
                    "\" -fullscreen -width " +
                    Screen.PrimaryScreen.Bounds.Width +
                    " -height " +
                    Screen.PrimaryScreen.Bounds.Height +
                    " " + command;
                    break;
                case Engine.SOURCE2:
                    modProcess.StartInfo.Arguments = "-game " + modFolder + " -fullscreen -vr_enable_fake_vr_test -width " +
                    Screen.PrimaryScreen.Bounds.Width +
                    " -height " +
                    Screen.PrimaryScreen.Bounds.Height +
                    " " + command;
                    break;
            }
            
            modProcess.Start();
            modProcess.EnableRaisingEvents = true;
            modProcess.WaitForInputIdle();

            return modProcess;
        }

        public Process StartVR()
        {
            return StartVR("");
        }

        public Process StartVR(string command)
        {
            string modPath = launcher.GetCurrentMod().installPath;
            string modFolder = new DirectoryInfo(modPath).Name;

            Debug.Write(modPath);

            string exePath = launcher.GetCurrentGame().getExePath();

            modProcess = new Process();
            modProcess.StartInfo.FileName = exePath;
            modProcess.StartInfo.Arguments = "-vr -game " + modFolder + " " + command;
            modProcess.Start();
            modProcess.EnableRaisingEvents = true;
            modProcess.WaitForInputIdle();



            return modProcess;
        }

        public Process StartTools()
        {
            return StartTools("");
        }

        public Process StartTools(string command)
        {
            string gamePath = launcher.GetCurrentGame().installPath;
            string modPath = launcher.GetCurrentMod().installPath;

            string exePath = string.Empty;

            foreach (string file in Directory.GetFiles(gamePath))
            {
                if (new FileInfo(file).Extension == ".exe")
                {
                    exePath = file;
                    break;
                }
            }

            modProcess = new Process();
            modProcess.StartInfo.FileName = exePath;
            modProcess.StartInfo.Arguments = "-game \"" +
                modPath +
                "\" -tools -nop4 -windowed -novid -noborder 0 -width " +
                parent.Width +
                " -height " +
                parent.Height +
                " " + command;
            modProcess.Start();
            AttachProcessTo(modProcess, parent);

            return modProcess;
        }

        public void AttachProcessTo(Process process, Control parent)
        {
            if (modProcess != null)
            {
                modProcess.EnableRaisingEvents = true;
                modProcess.WaitForInputIdle();
                while (modProcess.MainWindowHandle.ToString() == "0")
                {
                    // Just wait until the window is created. Bad, right?
                }
                Program.SetParent(modProcess.MainWindowHandle, parent.Handle);

                Resize();
            }
        }
    }
}
