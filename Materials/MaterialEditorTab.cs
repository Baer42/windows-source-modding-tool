﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using static source_modding_tool.MaterialEditor;
using System.IO;
 
using DevExpress.XtraBars;
using SourceSDK.Materials;
using SourceSDK;
using SourceSDK.Packages;
using System.Diagnostics;
using SourceSDK.Packages.VPKPackage;
using source_modding_tool.Materials;

namespace source_modding_tool
{
    public partial class MaterialEditorTab : DevExpress.XtraEditors.XtraUserControl, ShaderInterface
    {
        public Dictionary<string, PictureEdit> PictureEdits { get; set; }
        public Dictionary<string, Texture> Textures { get; set; }
        string[] detail = null;

        public Launcher Launcher { get; set; }
        public PackageManager PackageManager { get; set; }
        object popupMenuActivator;

        public string RelativePath { get; set; } = "";

        int textureWidth = 512;
        int textureHeight = 512;

        [Browsable(true)]
        public event EventHandler OnUpdated;

        public string VMT
        {
            get
            {
                return KeyValue.writeChunk(GetVMT(), true);
            }
        }

        public string Shader
        {
            get
            {
                return shaderCombo.EditValue.ToString();
            }
            set
            {
                shaderCombo.EditValue = value;
            }
        }

        public MaterialEditorTab(Launcher launcher, PackageManager packageManager)
        {
            this.Launcher = launcher;
            this.PackageManager = packageManager;
            InitializeComponent();
            PopulatePictureEdits();
            ClearMaterial();

            UpdatePreview();
        }

        public void PopulatePictureEdits()
        {
            PictureEdits = new Dictionary<string, PictureEdit>();
            PictureEdits.Add("tooltexture", pictureToolTexture);
            PictureEdits.Add("basetexture", pictureBaseTexture);
            PictureEdits.Add("basetexture2", pictureBaseTexture2);
            PictureEdits.Add("bumpmap", pictureBumpMap);
            PictureEdits.Add("envmapmask", pictureEnvMapMask);
            PictureEdits.Add("blendmodulatetexture", pictureBlendModulateTexture);
        }

        public void ClearMaterial()
        {
            Textures = new Dictionary<string, Texture>();

            foreach (KeyValuePair<string, PictureEdit> kv in PictureEdits)
            {
                Textures.Add(kv.Key, new Texture());
                kv.Value.Image = null;
            }

            //textEdit1.EditValue = "concrete/new_material";

            comboSurfaceProp.EditValue = string.Empty;
            comboSurfaceProp2.EditValue = string.Empty;
            comboDetail.EditValue = string.Empty;

            shaderCombo.EditValue = "LightmappedGeneric";
        }

        public void ClearTexture(PictureEdit pictureEdit)
        {
            string tag = pictureEdit.Tag.ToString();
            pictureEdit.Image = null;
            Textures[tag].bitmap = null;
            Textures[tag].bytes = null;
            Textures[tag].relativePath = string.Empty;

            CreateToolTexture();
        }

        public void comboDetail_EditValueChanged(object sender, EventArgs e)
        {
            switch (comboDetail.EditValue.ToString())
            {
                case "noise":
                    detail = new string[4] { "detail\\noise_detail_01", "7.74", "0.8", "0" };
                    break;
                case "metal":
                    detail = new string[4] { "detail\\metal_detail_01", "4.283", ".65", "0" };
                    break;
                case "rock":
                    detail = new string[4] { "detail\\rock_detail_01", "11", "1", "0" };
                    break;
                case "plaster":
                    detail = new string[4] { "detail\\plaster_detail_01", "6.783", ".8", "0" };
                    break;
                case "wood":
                    detail = new string[4] { "detail\\wood_detail_01", "2.583", ".8", "0" };
                    break;
                default:
                    detail = null;
                    break;
            }

            UpdatePreview();
        }

        private void contextClear_ItemClick(object sender, ItemClickEventArgs e)
        {
            PictureEdit pictureEdit = (PictureEdit)popupMenuActivator;
            ClearTexture(pictureEdit);
        }

        private void contextLoad_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        { LoadTexture((PictureEdit)popupMenuActivator); }

        private void CreateToolTexture()
        {
            Bitmap basetexture = Textures["basetexture"].bitmap;
            Bitmap basetexture2 = Textures["basetexture2"].bitmap;

            if (basetexture == null)
            {
                Textures["tooltexture"].bitmap = null;
                Textures["tooltexture"].bytes = null;
                Textures["tooltexture"].relativePath = string.Empty;
            }

            if (basetexture != null && basetexture2 == null)
            {
                Textures["tooltexture"].bitmap = basetexture;
                Textures["tooltexture"].bytes = Textures["basetexture"].bytes;
                Textures["tooltexture"].relativePath = string.Empty;
            }
            else if (basetexture != null && basetexture2 != null)
            {
                Bitmap tooltexture = null;

                // Resize images
                int width = basetexture.Width;
                int height = basetexture.Height;

                // Merge images
                tooltexture = new Bitmap(width, height);

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        Color baseColor = basetexture.GetPixel(i, j);
                        Color baseColor2 = basetexture2.GetPixel(i, j);

                        float baseMultiply = Math.Min(Math.Max(2.5f - (float)(i + j) / (width + height) * 4, 0), 1);
                        float baseMultiply2 = 1 - baseMultiply;

                        Color toolColor = Color.FromArgb((int)(baseColor.R * baseMultiply + baseColor2.R * baseMultiply2),
                                                         (int)(baseColor.G * baseMultiply + baseColor2.G * baseMultiply2),
                                                         (int)(baseColor.B * baseMultiply + baseColor2.B * baseMultiply2));
                        tooltexture.SetPixel(i, j, toolColor);
                    }
                }

                Textures["tooltexture"].bitmap = tooltexture;
                Textures["tooltexture"].bytes = VTF.FromBitmap(tooltexture, Launcher);
                Textures["tooltexture"].relativePath = string.Empty;
            }

            pictureToolTexture.Image = Textures["tooltexture"].bitmap;
        }

        public void LoadMaterial(PackageFile file)
        {
            string data = System.Text.Encoding.UTF8.GetString(file.Data);

            string fullPath = "";

            SourceSDK.KeyValue vmt = SourceSDK.KeyValue.ReadChunk(data);

            string relativePath = file.Path + "/" + file.Filename;

            this.RelativePath = relativePath.Substring("materials/".Length);

            foreach (KeyValuePair<string, PictureEdit> kv in PictureEdits)
            {
                if (vmt != null)
                {
                    string value = vmt.getChildren()[0].getValue("$" + kv.Key);

                    if (value != null && value != "")
                    {
                        PackageFile textureFile = PackageManager.GetFile("materials/" + value + ".vtf");

                        if (textureFile != null)
                        {
                            Textures[kv.Key].relativePath = textureFile.Path + "/" + textureFile.Filename + ".vtf";
                            Textures[kv.Key].bytes = textureFile.Data;
                            Textures[kv.Key].bitmap = VTF.ToBitmap(Textures[kv.Key].bytes, Launcher);
                            kv.Value.Image = Textures[kv.Key].bitmap;
                        } else
                        {
                            ClearTexture(kv.Value);
                        }

                    }
                }
                else
                {
                    ClearTexture(kv.Value);
                }
            }

            if (vmt != null && vmt.getValue("$normalmapalphaenvmapmask") == "1" && Textures["bumpmap"].bitmap != null)
            {
                Textures["envmapmask"].bitmap = new Bitmap(Textures["bumpmap"].bitmap.Width, Textures["bumpmap"].bitmap.Height);
                for (int i = 0; i < Textures["bumpmap"].bitmap.Width; i++)
                {
                    for (int j = 0; j < Textures["bumpmap"].bitmap.Height; j++)
                    {
                        int alpha = Textures["bumpmap"].bitmap.GetPixel(i, j).A;
                        Textures["envmapmask"].bitmap.SetPixel(i, j, Color.FromArgb(alpha, alpha, alpha));
                    }
                }
                Textures["envmapmask"].bytes = VTF.FromBitmap(Textures["envmapmask"].bitmap, Launcher);
                Textures["envmapmask"].relativePath = "";
                pictureEnvMapMask.Image = Textures["envmapmask"].bitmap;
            }

            UpdatePreview();
        }

        public void LoadTexture(PictureEdit pictureEdit)
        {
            string tag = pictureEdit.Tag.ToString();

            int width = textureWidth;
            int height = textureHeight;

            if (openBitmapFileDialog.ShowDialog() == DialogResult.OK)
            {
                string type = new FileInfo(openBitmapFileDialog.FileName).Extension;

                string modPath = Launcher.GetCurrentMod().InstallPath;

                Uri path1 = new Uri(modPath + "\\");
                Uri path2 = new Uri(openBitmapFileDialog.FileName);
                Uri diff = path1.MakeRelativeUri(path2);

                if (type == ".vtf")
                {
                    Textures[tag].relativePath = diff.OriginalString;
                    Textures[tag].bytes = File.ReadAllBytes(openBitmapFileDialog.FileName);
                    Textures[tag].bitmap = VTF.ToBitmap(Textures[tag].bytes, Launcher);
                }
                else
                {
                    Textures[tag].relativePath = string.Empty;
                    Image originalBitmap = Bitmap.FromFile(openBitmapFileDialog.FileName);
                    width = (int)Math.Pow(2, Math.Floor(Math.Log(originalBitmap.Width, 2)));
                    height = (int)Math.Pow(2, Math.Floor(Math.Log(originalBitmap.Height, 2)));
                    Textures[tag].bitmap = new Bitmap(originalBitmap, width, height);
                    originalBitmap.Dispose();
                    Textures[tag].bytes = VTF.FromBitmap(Textures[tag].bitmap, Launcher);
                }

                if (Textures[tag].bitmap != null)
                    pictureEdit.Image = Textures[tag].bitmap;
            }

            CreateToolTexture();
        }

        public void pictureBaseTexture_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                LoadTexture((PictureEdit)sender);
            }

            UpdatePreview();
        }

        private void popupMenu_Popup(object sender, EventArgs e) { popupMenuActivator = popupMenu.Activator; }

        public void SaveMaterial()
        {
            string shader = "VertexLitGeneric";

            SourceSDK.KeyValue vmt = new SourceSDK.KeyValue(shader);

            string fullPath = (Launcher.GetCurrentMod().InstallPath + "\\" + RelativePath).Replace(" / ", "\\");

            Directory.CreateDirectory(fullPath.Substring(0, fullPath.LastIndexOf("\\")));

            bool hasNormalMap = false;
            bool hasSpecularMap = false;

            foreach (KeyValuePair<string, Texture> texture in Textures)
            {
                if (texture.Value.bitmap != null)
                {
                    switch (texture.Key)
                    {
                        case "tooltexture":
                            if (texture.Value.bytes != Textures["basetexture"].bytes)
                                File.WriteAllBytes(fullPath + "_" + texture.Key + ".vtf", texture.Value.bytes);

                            break;
                        case "envmapmask":
                            hasSpecularMap = true;
                            break;
                        case "bumpmap":
                            hasNormalMap = true;
                            break;
                        default:
                            File.WriteAllBytes(fullPath + "_" + texture.Key + ".vtf", texture.Value.bytes);
                            break;
                    }
                }
            }

            if (hasNormalMap && hasSpecularMap)
            {
                Bitmap normalBitmap = Textures["bumpmap"].bitmap;
                Bitmap specularBitmap = new Bitmap(Textures["envmapmask"].bitmap,
                                                   normalBitmap.Width,
                                                   normalBitmap.Height);

                for (int i = 0; i < normalBitmap.Width; i++)
                {
                    for (int j = 0; j < normalBitmap.Height; j++)
                    {
                        Color normalColor = normalBitmap.GetPixel(i, j);
                        Color specularColor = specularBitmap.GetPixel(i, j);
                        normalBitmap.SetPixel(i,
                                              j,
                                              Color.FromArgb(specularColor.R,
                                                             normalColor.R,
                                                             normalColor.G,
                                                             normalColor.B));
                    }
                }
                Textures["bumpmap"].bytes = VTF.FromBitmap(Textures["bumpmap"].bitmap, Launcher);
                File.WriteAllBytes(fullPath + "_bumpmap.vtf", Textures["bumpmap"].bytes);
            }
            else if (hasNormalMap)
                File.WriteAllBytes(fullPath + "_bumpmap.vtf", Textures["bumpmap"].bytes);
            
            else if (hasSpecularMap)
                File.WriteAllBytes(fullPath + "_envmapmask.vtf", Textures["envmapmask"].bytes);

            File.WriteAllText(fullPath + ".vmt", VMT, new UTF8Encoding(false));
        }

        public KeyValue GetVMT()
        {
            SourceSDK.KeyValue vmt = new SourceSDK.KeyValue(Shader);

            bool hasNormalMap = false;
            bool hasSpecularMap = false;

            string materialRelativePath = RelativePath;
            if (RelativePath.StartsWith("/materials/"))
                materialRelativePath = RelativePath.Substring("/materials/".Length);

            foreach (KeyValuePair<string, Texture> texture in Textures)
            {
                if (texture.Value.bitmap != null)
                {
                    switch (texture.Key)
                    {
                        case "tooltexture":
                            if (texture.Value.bytes == Textures["basetexture"].bytes)
                            {
                                vmt.addChild(new KeyValue("$" + texture.Key, materialRelativePath + "_basetexture"));
                            }
                            else
                            {
                                vmt.addChild(new KeyValue("$" + texture.Key, materialRelativePath + "_" + texture.Key));
                            }
                            break;
                        case "envmapmask":
                            hasSpecularMap = true;
                            break;
                        case "bumpmap":
                            hasNormalMap = true;
                            break;
                        default:
                            vmt.addChild(new KeyValue("$" + texture.Key, materialRelativePath + "_" + texture.Key));
                            break;
                    }
                }
            }

            if (hasNormalMap && hasSpecularMap)
            {
                vmt.addChild(new KeyValue("$bumpmap", materialRelativePath + "_bumpmap"));
                vmt.addChild(new KeyValue("$normalmapalphaenvmapmask", "1"));
                vmt.addChild(new KeyValue("$envmap", "env_cubemap"));
            }
            else if (hasNormalMap)
            {
                vmt.addChild(new KeyValue("$bumpmap", materialRelativePath + "_bumpmap"));
            }
            else if (hasSpecularMap)
            {
                vmt.addChild(new KeyValue("$envmap", "env_cubemap"));
                vmt.addChild(new KeyValue("$envmapmask", materialRelativePath + "_envmapmask"));
            }

            if (detail != null)
            {
                vmt.addChild(new KeyValue("$detail", detail[0]));
                vmt.addChild(new KeyValue("$detailscale", detail[1]));
                vmt.addChild(new KeyValue("$detailblendfactor", detail[2]));
                vmt.addChild(new KeyValue("$detailblendmode", detail[3]));
            }

            vmt.addChild(new KeyValue("$surfaceprop", comboSurfaceProp.EditValue.ToString()));
            vmt.addChild(new KeyValue("$surfaceprop2", comboSurfaceProp2.EditValue.ToString()));

            return vmt;
        }

        private void UpdatePreview()
        {
            if (OnUpdated != null) {
                OnUpdated.Invoke(this, new EventArgs());
            };
        }

        private void pictureBaseTexture_EditValueChanged(object sender, EventArgs e)
        {

        }

        private void EditValueChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }
    }
}
