﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using static windows_source1ide.Steam;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using DevExpress.XtraBars;
using windows_source1ide.Tools;
using windows_source1ide.SourceSDK;
using System.Threading;

namespace windows_source1ide
{
    public partial class MainForm : DevExpress.XtraEditors.XtraForm
    {
        Game game = null;
        Steam sourceSDK;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form_Load(object sender, EventArgs e)
        {
            Text = Application.ProductName;
            string currentGame = Properties.Settings.Default.currentGame;
            string currentMod = Properties.Settings.Default.currentMod;

            sourceSDK = new Steam();
            updateGamesCombo();

            gamesCombo.EditValue = currentGame;
            modsCombo.EditValue = currentMod;
        }

        private void updateGamesCombo()
        {
            string currentGame = (gamesCombo.EditValue != null ? gamesCombo.EditValue.ToString() : "");
            repositoryGamesCombo.Items.Clear();
            foreach (KeyValuePair<string, string> item in sourceSDK.GetGamesList())
            {
                repositoryGamesCombo.Items.Add(item.Key);
            }

            if (repositoryGamesCombo.Items.Count > 0 && repositoryGamesCombo.Items.Contains(currentGame))
                gamesCombo.EditValue = currentGame;
            else if (repositoryGamesCombo.Items.Count > 0)
                gamesCombo.EditValue = repositoryGamesCombo.Items[0];
            else
                gamesCombo.EditValue = "";
        }

        private void updateModsCombo()
        {
            string currentMod = (modsCombo.EditValue != null ? modsCombo.EditValue.ToString() : "");
            repositoryModsCombo.Items.Clear();
            foreach (KeyValuePair<string, string> item in sourceSDK.GetModsList(gamesCombo.EditValue.ToString()))
            {
                repositoryModsCombo.Items.Add(item.Key);
            }

            if (repositoryModsCombo.Items.Count > 0 && repositoryModsCombo.Items.Contains(currentMod))
                modsCombo.EditValue = currentMod;
            else if (repositoryModsCombo.Items.Count > 0)
                modsCombo.EditValue = repositoryModsCombo.Items[0];
            else
                modsCombo.EditValue = "";
        }

        private void gamesCombo_EditValueChanged(object sender, EventArgs e)
        {
            sourceSDK.setCurrentGame((gamesCombo.EditValue != null ? gamesCombo.EditValue.ToString() : ""));
            updateModsCombo();
            Properties.Settings.Default.currentGame = gamesCombo.EditValue.ToString();
            Properties.Settings.Default.Save();
        }

        private void barButtonHammer_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            sourceSDK.RunHammer(Application.StartupPath);
        }

        private void barButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            Close();
        }

        private void barButtonModOpenFolder_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            sourceSDK.OpenModFolder(modsCombo.EditValue.ToString());
        }

        private void barButtonGameinfo_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            GameinfoForm form = new GameinfoForm(sourceSDK);
            form.ShowDialog();
            updateGamesCombo();
            updateModsCombo();
        }

        private void barButtonChapters_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            ChaptersForm form = new ChaptersForm(sourceSDK);
            form.ShowDialog();
        }

        private void modsCombo_EditValueChanged(object sender, EventArgs e)
        {
            sourceSDK.setCurrentMod(modsCombo.EditValue.ToString());
            Properties.Settings.Default.currentMod = modsCombo.EditValue.ToString();
            Properties.Settings.Default.Save();

            buttonModStart.Enabled = (modsCombo.EditValue != null && modsCombo.EditValue.ToString() != "");
            barMod.Enabled = (modsCombo.EditValue != null && modsCombo.EditValue.ToString() != "");
            barLevelDesign.Enabled = (modsCombo.EditValue != null && modsCombo.EditValue.ToString() != "");
            barParticles.Enabled = (modsCombo.EditValue != null && modsCombo.EditValue.ToString() != "");

            buttonHLMV.Enabled = (modsCombo.EditValue != null && modsCombo.EditValue.ToString() != "");
            buttonFaceposer.Enabled = (modsCombo.EditValue != null && modsCombo.EditValue.ToString() != "");
            
            buttonHammerPropper.Enabled = (modsCombo.EditValue != null && modsCombo.EditValue.ToString() != "");
        }

        private void buttonModStop_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (game.modProcess != null)
                game.Stop();
        }

        private void assetsCopierButton_ItemClick(object sender, ItemClickEventArgs e)
        {
            AssetsCopierForm form = new AssetsCopierForm(gamesCombo.EditValue.ToString(), modsCombo.EditValue.ToString());
            form.ShowDialog();
        }

        private void barButtonItem3_ItemClick(object sender, ItemClickEventArgs e)
        {
            NewModForm form = new NewModForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                string folder = form.modFolder;
                string title = form.modTitle;
                string game = form.game;
                string gameBranch = form.gameBranch;

                string mod = title + " (" + folder + ")";

                sourceSDK.setCurrentGame(game);
                sourceSDK.setCurrentMod(mod);

                updateModsCombo();

                gamesCombo.EditValue = game;
                modsCombo.EditValue = title + " (" + folder + ")";
            }
        }

        private void importMapButton_ItemClick(object sender, ItemClickEventArgs e)
        {
            ModSelectionDialog dialog = new ModSelectionDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string game = dialog.game;
                string mod = dialog.mod;

                AssetsCopierForm assetsCopierForm = new AssetsCopierForm(game, mod, sourceSDK.GetModPath(gamesCombo.EditValue.ToString(), modsCombo.EditValue.ToString()));
                if (assetsCopierForm.ShowDialog() == DialogResult.OK)
                {

                }
            }
        }

        private void menuButton_ItemClick(object sender, ItemClickEventArgs e)
        {
            GamemenuForm form = new GamemenuForm(sourceSDK);
            form.ShowDialog();
        }

        private void buttonVPKExplorer_ItemClick(object sender, ItemClickEventArgs e)
        {
            VPKExplorer form = new VPKExplorer(sourceSDK);
            form.ShowDialog();
        }

        private void barButtonClean_ItemClick(object sender, ItemClickEventArgs e)
        {
            string modPath = sourceSDK.GetModPath(gamesCombo.EditValue.ToString(),modsCombo.EditValue.ToString());

            if (File.Exists(modPath + "\\Gamestate.txt"))
                File.Delete(modPath + "\\Gamestate.txt");
            if (File.Exists(modPath + "\\demoheader.tmp"))
                File.Delete(modPath + "\\demoheader.tmp");
            if (File.Exists(modPath + "\\ep1_gamestats.dat"))
                File.Delete(modPath + "\\ep1_gamestats.dat");
            if (File.Exists(modPath + "\\modelsounds.cache"))
                File.Delete(modPath + "\\modelsounds.cache");
            if (File.Exists(modPath + "\\stats.txt"))
                File.Delete(modPath + "\\stats.txt");
            if (File.Exists(modPath + "\\voice_ban.dt"))
                File.Delete(modPath + "\\voice_ban.dt");
            if (File.Exists(modPath + "\\cfg\\config.cfg"))
                File.Delete(modPath + "\\cfg\\config.cfg");
            if (File.Exists(modPath + "\\cfg\\server_blacklist.txt"))
                File.Delete(modPath + "\\cfg\\server_blacklist.txt");
            if (File.Exists(modPath + "\\sound\\sound.cache"))
                File.Delete(modPath + "\\sound\\sound.cache");
            if (File.Exists(modPath + "\\voice_ban.dt"))
                File.Delete(modPath + "\\voice_ban.dt");
            if (Directory.Exists(modPath + "\\materialsrc"))
                Directory.Delete(modPath + "\\materialsrc", true);
            if (Directory.Exists(modPath + "\\downloadlists"))
                Directory.Delete(modPath + "\\downloadlists", true);
            if (Directory.Exists(modPath + "\\mapsrc"))
                Directory.Delete(modPath + "\\mapsrc", true);
            if (Directory.Exists(modPath + "\\save"))
                Directory.Delete(modPath + "\\save", true);
            if (Directory.Exists(modPath + "\\screenshots"))
                Directory.Delete(modPath + "\\screenshots",true);
        }

        private void buttonCrafty_ItemClick(object sender, ItemClickEventArgs e)
        {
            Process.Start("Tools\\Crafty\\Crafty.exe");
        }

        private void buttonBatchCompiler_ItemClick(object sender, ItemClickEventArgs e)
        {
            Process.Start("Tools\\BatchCompiler\\Batch Compiler.exe");
        }

        private void buttonCrowbar_ItemClick(object sender, ItemClickEventArgs e)
        {
            Process.Start("Tools\\Crowbar\\Crowbar.exe");
        }

        private void buttonHLMV_ItemClick(object sender, ItemClickEventArgs e)
        {
            string gamePath = sourceSDK.GetGamePath(gamesCombo.EditValue.ToString());

            string toolPath = gamePath + "\\bin\\hlmv.exe";
            Process.Start(toolPath);
        }

        private void barButtonFaceposer_ItemClick(object sender, ItemClickEventArgs e)
        {
            string gamePath = sourceSDK.GetGamePath(gamesCombo.EditValue.ToString());

            string toolPath = gamePath + "\\bin\\hlfaceposer.exe";
            Process.Start(toolPath);
        }

        private void barButtonItem5_ItemClick(object sender, ItemClickEventArgs e)
        {
            Process.Start("Tools\\TerrainGenerator\\TerrainGenerator.exe");
        }

        private void buttonIngameTools_ItemClick(object sender, ItemClickEventArgs e)
        {
            game = new Game(sourceSDK, panel1);
            game.StartTools();

            FormBorderStyle = FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            modProcessUpdater.Enabled = true;

            buttonModStart.Enabled = false;
            barButtonRun.Enabled = false;
            buttonModStop.Visibility = BarItemVisibility.Always;
            game.modProcess.Exited += new EventHandler(modExited);
        }

        private void buttonVMFtoMDL_ItemClick(object sender, ItemClickEventArgs e)
        {
            VMFtoMDL form = new VMFtoMDL(sourceSDK);
            form.ShowDialog();
        }

        private void barButtonItem2_ItemClick(object sender, ItemClickEventArgs e)
        {
            PCF.CreateManifest(sourceSDK);
            XtraMessageBox.Show("Particle manifest generated.");
        }

        private void buttonHammerPropper_ItemClick(object sender, ItemClickEventArgs e)
        {
            sourceSDK.RunPropperHammer();
        }

        private void buttonOpenPrefabsFolder_ItemClick(object sender, ItemClickEventArgs e)
        {
            string gamePath = sourceSDK.GetGamePath();
            Process.Start(gamePath + "\\bin\\Prefabs");
        }

        private void buttonModStart_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            game = new Game(sourceSDK, panel1);
            game.Start();
            FormBorderStyle = FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            modProcessUpdater.Enabled = true;

            buttonModStart.Enabled = false;
            barButtonRun.Enabled = false;
            buttonModStop.Visibility = BarItemVisibility.Always;
            game.modProcess.Exited += new EventHandler(modExited);
        }

        private void modExited(object sender, EventArgs e)
        {
            buttonModStart.Enabled = true;
            barButtonRun.Enabled = true;
            buttonModStop.Visibility = BarItemVisibility.Never;
            game.modProcess = null;
            //FormBorderStyle = FormBorderStyle.Sizable;
        }


        private void ModForm_ResizeEnd(object sender, EventArgs e)
        {
            if (game != null)
                game.Resize();
        }

        private void ModForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (game != null)
                game.Stop();
        }

        private void barButtonRunFullscreen_ItemClick(object sender, ItemClickEventArgs e)
        {
            game = new Game(sourceSDK, panel1);
            game.StartFullScreen();

            buttonModStart.Enabled = false;
            barButtonRun.Enabled = false;
            buttonModStop.Visibility = BarItemVisibility.Always;
            game.modProcess.Exited += new EventHandler(modExited);
        }

        private void modProcessUpdater_Tick(object sender, EventArgs e)
        {
            if (game.modProcess == null)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                modProcessUpdater.Enabled = false;
                MaximizeBox = true;
            }
        }

        private void buttonTest_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void ModForm_Resize(object sender, EventArgs e)
        {
            
        }

        private void buttonOpenMapsrcFolder_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void buttonMaterialEditor_ItemClick(object sender, ItemClickEventArgs e)
        {
            MaterialEditor form = new MaterialEditor("", sourceSDK);
            form.ShowDialog();
        }
    }
}