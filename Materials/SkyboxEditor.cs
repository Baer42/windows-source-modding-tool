﻿using CefSharp;
using CefSharp.WinForms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using source_modding_tool.Modding;
using SourceSDK;
using SourceSDK.Materials;
using SourceSDK.Packages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace source_modding_tool.Materials
{
    public partial class SkyboxEditor : DevExpress.XtraEditors.XtraForm
    {
        private Launcher launcher;
        private PackageManager packageManager;

        private Dictionary<string, PictureEdit> imageEdits;
        private Dictionary<string, PictureEdit> hdrImageEdits;

        public PackageFile PackageFile { get; set; } = null;

        string Skyname { get; set; } = "";

        ChromiumWebBrowser chromium;

        public SkyboxEditor(Launcher launcher)
        {
            this.launcher = launcher;
            this.packageManager = new PackageManager(launcher, "materials/skybox");

            InitializeComponent();

            SetImageEdits();
            Clear();

            UpdateSkyListCombo();

            hdrDockPanel.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Hidden;

            CefSharpSettings.ShutdownOnExit = true;

            CefSettings settings = new CefSettings
            {
                CachePath = AppDomain.CurrentDomain.BaseDirectory + "/Assets/cache/"
            };
            if (!Cef.IsInitialized)
                Cef.Initialize(settings);

            // Init chromium
            chromium = new ChromiumWebBrowser(AppDomain.CurrentDomain.BaseDirectory + "Tools/SkyboxPreviewer/index.html");
            chromium.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
            chromium.BrowserSettings.FileAccessFromFileUrls = CefState.Enabled;
            chromium.BrowserSettings.UniversalAccessFromFileUrls = CefState.Enabled;
            chromium.BrowserSettings.BackgroundColor = Cef.ColorSetARGB(255, 0, 0, 0);

            // Add the control
            this.Controls.Add(chromium);
            chromium.Dock = DockStyle.Fill;
        }

        private void SetImageEdits()
        {
            imageEdits = new Dictionary<string, PictureEdit>
            {
                {"up", upEdit },
                {"dn", dnEdit },
                {"lf", lfEdit },
                {"rt", rtEdit },
                {"ft", ftEdit },
                {"bk", bkEdit }
            };

            hdrImageEdits = new Dictionary<string, PictureEdit>
            {
                {"up", upHdrEdit },
                {"dn", dnHdrEdit },
                {"lf", lfHdrEdit },
                {"rt", rtHdrEdit },
                {"ft", ftHdrEdit },
                {"bk", bkHdrEdit }
            };
        }

        private void UpdateSkyListCombo()
        {
            List<string> skyboxList = new List<string>();
            foreach(PackageDirectory directory in packageManager.Directories)
            {
                foreach(PackageFile file in directory.Entries)
                {
                    if (file.Path.EndsWith("materials/skybox") && file.Extension == "vmt")
                    {
                        string skyName = file.Filename;

                        if (skyName.EndsWith("up") || skyName.EndsWith("dn") || skyName.EndsWith("lf") || skyName.EndsWith("rt") || skyName.EndsWith("ft") || skyName.EndsWith("bk"))
                            skyName = skyName.Substring(0, skyName.Length - 2);

                        skyboxList.Add(skyName);
                    }
                }
            }

            skyboxList = skyboxList.Distinct().ToList();
            skyboxList.Sort();

            skyListRepository.Items.Clear();

            foreach(string skybox in skyboxList)
            {
                skyListRepository.Items.Add(skybox);
            }
        }

        private void Clear()
        {
            foreach (string face in new string[] { "up", "dn", "lf", "rt", "ft", "bk" })
            {
                File.Copy(AppDomain.CurrentDomain.BaseDirectory + "Tools/SkyboxPreviewer/blank.png", AppDomain.CurrentDomain.BaseDirectory + "Tools/SkyboxPreviewer/" + face + ".png",true);
            }

            if (imageEdits != null)
            {
                foreach (PictureEdit imageEdit in imageEdits.Values)
                {
                    imageEdit.Image = null;
                }
            }
            Skyname = "";
            if (chromium != null)
                chromium.Reload();
        }

        private void Open(string skyname)
        {
            foreach (string face in new string[] { "up", "dn", "lf", "rt", "ft", "bk" })
            {
                PackageFile file = packageManager.GetFile("materials/skybox/" + skyname + face + ".vmt");

                if (file != null)
                {
                    KeyValue fileData = VMT.FromData(file.Data);

                    KeyValue baseTexture = fileData.findChildByKey("$basetexture");
                    KeyValue hdrbaseTexture = fileData.findChildByKey("$hdrbasetexture");
                    KeyValue hdrcompressedTexture = fileData.findChildByKey("$hdrcompressedtexture");

                    if (baseTexture != null)
                    {
                        string baseTexturePath = baseTexture.getValue();
                        PackageFile baseTextureFile = packageManager.GetFile("materials/" + baseTexturePath + ".vtf");

                        if (baseTextureFile != null)
                        {
                            Bitmap baseTextureImage = VTF.ToBitmap(baseTextureFile.Data, launcher);

                            imageEdits[face].Image = baseTextureImage;

                            SavePreview(baseTextureImage, face);
                        }
                    }

                    if (hdrbaseTexture != null)
                    {
                        string baseTexturePath = hdrbaseTexture.getValue();
                        PackageFile baseTextureFile = packageManager.GetFile("materials/" + baseTexturePath + ".vtf");

                        if (baseTextureFile != null)
                        {
                            Bitmap baseTextureImage = VTF.ToBitmap(baseTextureFile.Data, launcher);

                            hdrImageEdits[face].Image = baseTextureImage;
                        }
                    }
                    else if (hdrcompressedTexture != null)
                    {
                        string baseTexturePath = hdrcompressedTexture.getValue();
                        PackageFile baseTextureFile = packageManager.GetFile("materials/" + baseTexturePath + ".vtf");

                        if (baseTextureFile != null)
                        {
                            Bitmap baseTextureImage = VTF.ToBitmap(baseTextureFile.Data, launcher);

                            hdrImageEdits[face].Image = baseTextureImage;
                        }
                    }
                }
            }

            chromium.Reload();
        }

        private void Save(string skyname)
        {
            string skyboxesPath = launcher.GetCurrentMod().InstallPath + "\\materials\\skybox\\";

            Directory.CreateDirectory(skyboxesPath);

            bool hasHDRTextures = false;

            foreach (KeyValuePair<string, PictureEdit> keyValuePair in imageEdits)
            {
                string face = keyValuePair.Key;

                byte[] vtf = VTF.FromBitmap(keyValuePair.Value.Image as Bitmap, launcher, new string[] { "nonice 1", "nocompress 1", "nolod 1", "clamps 1", "clampt 1", "nomip 1" });

                File.WriteAllBytes(skyboxesPath + skyname + face + ".vtf", vtf);

                KeyValue vmtRoot;
                if (hasHDRTextures) {
                    vmtRoot = new KeyValue("Sky");
                    vmtRoot.addChild(new KeyValue("$hdrbasetexture", "skybox/" + skyname + "_hdr" + face));
                } else {
                    vmtRoot = new KeyValue("UnlitGeneric");
                }

                vmtRoot.addChild(new KeyValue("$basetexture", "skybox/" + skyname + face));
                vmtRoot.addChild(new KeyValue("$nofog", "1"));
                vmtRoot.addChild(new KeyValue("$ignorez", "1"));

                if (keyValuePair.Value.Image.Width == 2 * keyValuePair.Value.Image.Height)
                {
                    vmtRoot.addChild(new KeyValue("$basetexturetransform", "center 0 0 scale 1 2 rotate 0 translate 0 0"));
                }

                KeyValue.writeChunkFile(skyboxesPath + skyname + face + ".vmt", vmtRoot, true, new UTF8Encoding(false));
            }

            if (hasHDRTextures)
            {
                foreach (KeyValuePair<string, PictureEdit> keyValuePair in hdrImageEdits)
                {
                    string face = keyValuePair.Key;

                    byte[] vtf = VTF.FromBitmap(keyValuePair.Value.Image as Bitmap, launcher, new string[] { "nonice 1", "nocompress 1" });
                    string fileName = skyname + face;
                    File.WriteAllBytes(skyboxesPath + skyname + "_hdr" + face + ".vtf", vtf);
                }
            }
        }

        private void menu_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // New material
            if (e.Item == menuFileNew)
            {
                Clear();
            }
            // Open material
            else if (e.Item == menuFileOpen)
            {
                FileExplorer fileExplorer = new FileExplorer(launcher, FileExplorer.Mode.OPEN)
                {
                    packageManager = packageManager,
                    RootDirectory = "materials/skybox",
                    Filter = "Skybox Files (*.skybox)|*.vmt",
                    MultiSelect = false
                };
                if (fileExplorer.ShowDialog() == DialogResult.OK)
                {
                    Skyname = fileExplorer.Selection[0].Filename;
                    Skyname = Skyname.Substring(0, Skyname.Length - 2);
                    if (Skyname.EndsWith("_hdr"))
                        Skyname = Skyname.Substring(0, Skyname.Length - 4);

                    Open(Skyname);
                }
            }
            // Save material
            else if (e.Item == menuFileSave)
            {
                if (Skyname != "")
                {
                    Save(Skyname);
                } else
                {
                    menuFileSaveAs.PerformClick();
                }
            }
            // Save material as
            else if (e.Item == menuFileSaveAs)
            {
                FileExplorer fileExplorer = new FileExplorer(launcher, FileExplorer.Mode.SAVE)
                {
                    packageManager = packageManager,
                    RootDirectory = "materials/skybox",
                    Filter = "Skybox Files (*.skybox)|*.vmt",
                    MultiSelect = false
                };
                if (fileExplorer.ShowDialog() == DialogResult.OK)
                {
                    Skyname = Path.GetFileName(fileExplorer.FileName);
                    System.Diagnostics.Debugger.Break();

                    Save(Skyname);
                }
            }
        }

        private void SkyboxEditor_Load(object sender, EventArgs e)
        {
            if (PackageFile != null)
            {
                string skyname = PackageFile.Filename;
                skyname = skyname.Substring(0, skyname.Length - 2);
                if (skyname.EndsWith("_hdr"))
                    skyname = skyname.Substring(0, skyname.Length - 4);

                Open(skyname);
            }
        }

        private void cubemapEdit_ImageLoading(object sender, DevExpress.XtraEditors.Repository.SaveLoadImageEventArgs e)
        {
            RepositoryItem pictureEdit = (RepositoryItem) sender;
            PictureEdit ownerEdit = (PictureEdit) pictureEdit.OwnerEdit;

            foreach (KeyValuePair<string, PictureEdit> imageEdit in imageEdits)
            {
                if (imageEdit.Value == ownerEdit)
                {
                    SavePreview(Bitmap.FromFile(e.FileName), imageEdit.Key);
                    chromium.Reload();
                    break;
                }
            }
        }

        private void SavePreview(Image source, string face)
        {
            Bitmap bitmap = source.Clone() as Bitmap;
            switch (face)
            {
                case "up":
                    bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    break;
                case "dn":
                    bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    break;
            }

            var destImage = new Bitmap(1024, 1024);
            destImage.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    if (bitmap.Width == bitmap.Height)
                    {
                        graphics.DrawImage(bitmap, new Rectangle(0, 0, destImage.Width, destImage.Height), 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, wrapMode);
                    } else if (bitmap.Width == 2 * bitmap.Height)
                    {
                        graphics.DrawImage(bitmap, new Rectangle(0, 0, destImage.Width, destImage.Height / 2), 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, wrapMode);
                        graphics.DrawImage(bitmap, new Rectangle(0, destImage.Height / 2, destImage.Width, destImage.Height / 2), 0, bitmap.Height - 1, bitmap.Width, 1, GraphicsUnit.Pixel, wrapMode);
                    }
                }
            }

            destImage.Save(AppDomain.CurrentDomain.BaseDirectory + "Tools/SkyboxPreviewer/" + face + ".png", ImageFormat.Png);
            bitmap.Dispose();
        }
    }
}