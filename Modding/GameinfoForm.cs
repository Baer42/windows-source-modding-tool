﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace windows_source1ide
{
    public partial class GameinfoForm : DevExpress.XtraEditors.XtraForm
    {
        Steam sourceSDK;
        SourceSDK.KeyValue gameinfo;

        List<String[]> searchPaths;

        public GameinfoForm(Steam sourceSDK)
        {
            InitializeComponent();
            this.sourceSDK = sourceSDK;
        }

        private void GameinfoForm_Load(object sender, EventArgs e)
        {
            string modPath = sourceSDK.GetModPath();

            string gameinfoPath = modPath + "\\gameinfo.txt";

            gameinfo = SourceSDK.KeyValue.readChunkfile(gameinfoPath);

            textGame.EditValue = gameinfo.getValue("game");
            textTitle.EditValue = gameinfo.getValue("title");
            textTitle2.EditValue = gameinfo.getValue("title2");

            string type = gameinfo.getValue("type");
            if (type == "multiplayer_only")
            {
                textType.EditValue = "Multi-player";
            }
            else if (type == "singleplayer_only")
            {
                textType.EditValue = "Single-player";
            }
            else
            {
                textType.EditValue = "Both";
            }
            switchDifficulty.EditValue = (gameinfo.getValue("nodifficulty") == "1" ? false : true);
            switchPortals.EditValue = (gameinfo.getValue("hasportals") == "1" ? true : false);
            switchCrosshair.EditValue = (gameinfo.getValue("nocrosshair") == "1" ? false : true);
            switchAdvCrosshair.EditValue = (gameinfo.getValue("advcrosshair") == "1" ? true : false);
            switchModels.EditValue = (gameinfo.getValue("nomodels") == "1" ? false : true);

            textDeveloper.EditValue = gameinfo.getValue("developer");
            textDeveloperURL.EditValue = gameinfo.getValue("developer_url");
            textManual.EditValue = gameinfo.getValue("manual");
            string icon = gameinfo.getValue("icon");

            if (File.Exists(modPath + "\\" + icon + ".tga"))
                pictureEdit2.Image = new TGASharpLib.TGA(modPath + "\\" + icon + ".tga").ToBitmap();

            if (File.Exists(modPath + "\\" + icon + "_big.tga"))
                pictureEdit1.Image = new TGASharpLib.TGA(modPath + "\\" + icon + "_big.tga").ToBitmap();

            switchNodegraph.EditValue = (gameinfo.getValue("nodegraph") == "0" ? false : true);
            textGamedata.EditValue = gameinfo.getValue("gamedata");
            textInstance.EditValue = gameinfo.getValue("instancepath");
            switchVR.EditValue = (gameinfo.getValue("supportsvr") == "1" ? true : false);

            comboGames.Properties.Items.Clear();
            string appID = gameinfo.getChild("filesystem").getValue("steamappid");
            foreach (KeyValuePair<string, string> item in sourceSDK.GetGamesList())
            {
                comboGames.Properties.Items.Add(item.Key);
                string gameAppID = sourceSDK.GetGameAppId(item.Key).ToString();
                if (appID == gameAppID.ToString())
                {
                    comboGames.EditValue = item.Key;
                }
            }

            pictureEdit1.Properties.ContextMenuStrip = new ContextMenuStrip();

            searchList.BeginUnboundLoad();
            searchList.Nodes.Clear();
            searchPaths = new List<string[]>();
            foreach (SourceSDK.KeyValue searchPath in gameinfo.getChild("filesystem").getChild("searchpaths").getChildrenList())
            {
                searchPaths.Add(new string[] { searchPath.getKey(), searchPath.getValue() });
                searchList.AppendNode(new object[] { searchPath.getKey(), searchPath.getValue() }, null);
            }
            searchList.EndUnboundLoad();
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            string modPath = sourceSDK.GetModPath();

            gameinfo.setValue("game", textGame.EditValue != null ? textGame.EditValue.ToString() : "");
            gameinfo.setValue("title", textTitle.EditValue != null ? textTitle.EditValue.ToString() : "");
            gameinfo.setValue("title2", textTitle2.EditValue != null ? textTitle2.EditValue.ToString() : "");

            string type;
            if (textType.EditValue.ToString() == "Multi-player")
            {
                type = "multiplayer_only";
            }
            else if (textType.EditValue.ToString() == "Single-player")
            {
                type = "singleplayer_only";
            }
            else
            {
                type = "both";
            }
            gameinfo.setValue("type", type);

            gameinfo.setValue("nodifficulty", (switchDifficulty.IsOn ? "0" : "1"));
            gameinfo.setValue("hasportals", (switchPortals.IsOn ? "1" : "0"));
            gameinfo.setValue("nocrosshair", (switchCrosshair.IsOn ? "0" : "1"));
            gameinfo.setValue("advcrosshair", (switchAdvCrosshair.IsOn ? "1" : "0"));
            gameinfo.setValue("nomodels", (switchModels.IsOn ? "0" : "1"));

            gameinfo.setValue("developer", textDeveloper.EditValue != null ? textDeveloper.EditValue.ToString() : "");
            gameinfo.setValue("developer_url", textDeveloperURL.EditValue != null ? textDeveloperURL.EditValue.ToString() : "");
            gameinfo.setValue("manual", textManual.EditValue != null ? textManual.EditValue.ToString() : "");
            gameinfo.setValue("icon", "resource/icon");

            if (pictureEdit2.Image != null)
                new TGASharpLib.TGA((Bitmap)pictureEdit2.Image).Save(modPath + "\\resource\\icon.tga");
            else if (File.Exists(modPath + "\\resource\\icon.tga"))
                File.Delete(modPath + "\\resource\\icon.tga");

            if (pictureEdit1.Image != null)
                new TGASharpLib.TGA((Bitmap)pictureEdit1.Image).Save(modPath + "\\resource\\icon_big.tga");
            else if (File.Exists(modPath + "\\resource\\icon_big.tga"))
                File.Delete(modPath + "\\resource\\icon_big.tga");

            gameinfo.setValue("nodegraph", switchNodegraph.IsOn ? "1" : "0");
            gameinfo.setValue("gamedata", textGamedata.EditValue != null ? textGamedata.EditValue.ToString() : "");
            gameinfo.setValue("instancepath", textInstance.EditValue != null ? textInstance.EditValue.ToString() : "");
            gameinfo.setValue("supportsvr", switchVR.IsOn ? "1" : "0");

            int appID = sourceSDK.GetGameAppId(comboGames.EditValue.ToString());
            gameinfo.getChild("filesystem").setValue("steamappid", appID.ToString());

            SourceSDK.KeyValue searchPathsKV = gameinfo.getChild("filesystem").getChild("searchpaths");
            searchPathsKV.clearChildren();
            foreach (String[] searchPath in searchPaths)
            {
                searchPathsKV.addChild(new SourceSDK.KeyValue(searchPath[0], searchPath[1]));
            }

            string path = modPath + "\\gameinfo.txt";

            SourceSDK.KeyValue.writeChunkFile(path, gameinfo, false, new UTF8Encoding(false));

            Close();
        }

        private void pictureIconLarge_Click(object sender, EventArgs e)
        {
            if (dialogIcon.ShowDialog() == DialogResult.OK)
            {
                Bitmap original = new TGASharpLib.TGA(dialogIcon.FileName).ToBitmap();


                Bitmap large = new Bitmap(32, 32);
                Bitmap small = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(large))
                    g.DrawImage(original, 0, 0, 32, 32);

                using (Graphics g = Graphics.FromImage(small))
                    g.DrawImage(original, 0, 0, 16, 16);

                pictureEdit2.Image = small;
                pictureEdit1.Image = large;
            }
        }

        private void pictureEdit1_Click(object sender, EventArgs e)
        {
            if (dialogIcon.ShowDialog() == DialogResult.OK)
            {
                Bitmap original = new TGASharpLib.TGA(dialogIcon.FileName).ToBitmap();


                Bitmap large = new Bitmap(32, 32);
                Bitmap small = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(large))
                    g.DrawImage(original, 0, 0, 32, 32);

                using (Graphics g = Graphics.FromImage(small))
                    g.DrawImage(original, 0, 0, 16, 16);

                pictureEdit2.Image = small;
                pictureEdit1.Image = large;
            }
        }

        private void buttonGamedata_Click(object sender, EventArgs e)
        {
            string hammerPath = sourceSDK.GetGamePath() + "\\bin\\";
            fgdDialog.InitialDirectory = hammerPath;
            if (fgdDialog.ShowDialog() == DialogResult.OK)
            {
                Uri path1 = new Uri(hammerPath);
                Uri path2 = new Uri(fgdDialog.FileName);
                Uri diff = path1.MakeRelativeUri(path2);
                textGamedata.EditValue = diff.OriginalString;
            }
        }

        private void buttonInstance_Click(object sender, EventArgs e)
        {
            string mapsrcPath = sourceSDK.GetModPath() + "\\mapsrc\\";
            Directory.CreateDirectory(mapsrcPath);
            instanceDialog.SelectedPath = (textGamedata.EditValue.ToString() == "" ? mapsrcPath : textGamedata.EditValue.ToString());
            if (instanceDialog.ShowDialog() == DialogResult.OK)
            {
                textInstance.EditValue = instanceDialog.SelectedPath;
            }
        }

        private void buttonDown_Click(object sender, EventArgs e)
        {
            if (searchList.FocusedNode != null)
            {
                int index = searchList.GetNodeIndex(searchList.FocusedNode);
                searchList.SetNodeIndex(searchList.FocusedNode, index + 1);

                buttonUp.Enabled = (index + 1 > 0);
                buttonDown.Enabled = (index + 1 < searchPaths.Count - 1);
                buttonRemove.Enabled = true;

                String[] item = searchPaths[index];
                searchPaths.RemoveAt(index);
                searchPaths.Insert(index + 1, item);
            }
        }

        private void buttonUp_Click(object sender, EventArgs e)
        {
            if (searchList.FocusedNode != null)
            {
                int index = searchList.GetNodeIndex(searchList.FocusedNode);
                searchList.SetNodeIndex(searchList.FocusedNode, index - 1);

                buttonUp.Enabled = (index - 1 > 0);
                buttonDown.Enabled = (index - 1 < searchPaths.Count - 1);
                buttonRemove.Enabled = true;

                String[] item = searchPaths[index];
                searchPaths.RemoveAt(index);
                searchPaths.Insert(index - 1, item);
            }
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            if (searchList.FocusedNode != null)
            {
                int index = searchList.GetNodeIndex(searchList.FocusedNode);
                searchPaths.RemoveAt(index);
                searchList.Nodes.RemoveAt(index);

                if (searchList.FocusedNode != null)
                {
                    index = searchList.GetNodeIndex(searchList.FocusedNode);

                    buttonUp.Enabled = (index > 0);
                    buttonDown.Enabled = (index < searchPaths.Count - 1);
                    buttonRemove.Enabled = true;
                }
                else
                {
                    buttonUp.Enabled = false;
                    buttonDown.Enabled = false;
                    buttonRemove.Enabled = false;
                }
            }
        }

        private void searchList_FocusedNodeChanged(object sender, DevExpress.XtraTreeList.FocusedNodeChangedEventArgs e)
        {
            if (searchList.FocusedNode != null)
            {
                int index = searchList.GetNodeIndex(searchList.FocusedNode);

                buttonUp.Enabled = (index > 0);
                buttonDown.Enabled = (index < searchPaths.Count - 1);
                buttonRemove.Enabled = true;
            } else
            {
                buttonUp.Enabled = false;
                buttonDown.Enabled = false;
                buttonRemove.Enabled = false;
            }
        }

        private void buttonAddVPK_Click(object sender, EventArgs e)
        {
            string gamePath = sourceSDK.GetGamePath() + "\\";
            vpkDialog.InitialDirectory = gamePath;
            if (vpkDialog.ShowDialog() == DialogResult.OK)
            {
                Uri path1 = new Uri(gamePath);
                Uri path2 = new Uri(vpkDialog.FileName);
                Uri diff = path1.MakeRelativeUri(path2);

                string path = diff.OriginalString;
                path.Replace("\\", "/");
                path = "|all_source_engine_paths|" + path;
                path = path.Replace("_dir.vpk", ".vpk");
                searchPaths.Add(new string[] { "game", path });
                searchList.BeginUnboundLoad();
                searchList.AppendNode(new object[] { "game", path }, null);
                searchList.EndUnboundLoad();
            }
        }

        private void buttonAddDirectory_Click(object sender, EventArgs e)
        {
            string gamePath = sourceSDK.GetGamePath() + "\\";
            Directory.CreateDirectory(gamePath);
            searchDirDialog.SelectedPath = gamePath;
            if (searchDirDialog.ShowDialog() == DialogResult.OK)
            {
                Uri path1 = new Uri(gamePath);
                Uri path2 = new Uri(searchDirDialog.SelectedPath);
                Uri diff = path1.MakeRelativeUri(path2);

                string path = diff.OriginalString;
                path.Replace("\\", "/");
                path = "|all_source_engine_paths|" + path;
                searchPaths.Add(new string[] { "game", path });
                searchList.BeginUnboundLoad();
                searchList.AppendNode(new object[] { "game", path }, null);
                searchList.EndUnboundLoad();
            }
        }
    }
}