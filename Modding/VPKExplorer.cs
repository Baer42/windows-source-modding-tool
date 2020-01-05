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
using System.Diagnostics;
using DevExpress.XtraTreeList.Nodes;
using DevExpress.XtraTreeList;

namespace windows_source1ide.Tools
{
    public partial class VPKExplorer : DevExpress.XtraEditors.XtraForm
    {
        string gamePath;
        string modPath;
        Steam sourceSDK;

        string currentDirectory = "";
        Stack<string> previousDirectories = new Stack<string>();
        Stack<string> nextDirectories = new Stack<string>();

        string filter = "";

        Dictionary<string, VPK> vpks = new Dictionary<string, VPK>();

        public VPKExplorer(Steam sourceSDK)
        {
            InitializeComponent();

            this.sourceSDK = sourceSDK;
        }

        private void VPKExplorer_Load(object sender, EventArgs e)
        {
            gamePath = sourceSDK.GetGamePath();
            modPath = sourceSDK.GetModPath();

            vpks.Clear();
            foreach(string vpk in sourceSDK.getModMountedVPKs())
                vpks.Add(vpk, new VPK(vpk, sourceSDK));

            traverseFileTree();
            traverseDirectory("");
        }

        class Folder {
            List<string> files = new List<string>();
            List<Folder> folders = new List<Folder>();
        }

        private void traverseFileTree()
        {
            List<VPK.File> files = getAllFiles();

            dirs.BeginUnboundLoad();
            dirs.Nodes.Clear();

            Stack<TreeListNode> stack = new Stack<TreeListNode>();
            Stack<string> stackString = new Stack<string>();

            stack.Push(dirs.AppendNode(new object[] { "root" },null));
            stack.Peek().Tag = "";
            stack.Peek().StateImageIndex = 0;

            for (int f = 0; f < files.Count; f++)
            {
                VPK.File file = files[f];

                string[] fileSplit = file.path.Split('/');

                while (stackString.Count >= fileSplit.Length)
                {
                    stackString.Pop();
                    stack.Pop();
                }

                for (int i = stackString.Count - 1; i >= 0; i--)
                {
                    if (stackString.Peek() != fileSplit[i])
                    {
                        stackString.Pop();
                        stack.Pop();
                    }
                    else
                        break;
                }

                for (int i = stack.Count - 1; i < fileSplit.Length - 1; i++)
                {
                    string tag = fileSplit[i] + "/";
                    if (stackString.Count > 0)
                        tag = stack.Peek().Tag.ToString() + tag;

                    stack.Push(dirs.AppendNode(new object[] { fileSplit[i] }, stack.Peek()));
                    stack.Peek().Tag = tag;
                    stack.Peek().StateImageIndex = 0;
                    stackString.Push(fileSplit[i]);
                }
            }

            dirs.ExpandToLevel(0);

            dirs.EndUnboundLoad();
        }

        private void traverseDirectory(string directory)
        {
            currentDirectory = directory;
            buttonUp.Enabled = (currentDirectory != "");
            buttonBack.Enabled = (previousDirectories.Count > 0);
            buttonForward.Enabled = (nextDirectories.Count > 0);

            filter = "";
            textSearch.EditValue = "";

            if (directory.Contains("/"))
                repositoryTextSearch.NullValuePrompt = "Search in " + directory.Substring(0, directory.Length - 1).Split('/').Last();
            else
                repositoryTextSearch.NullValuePrompt = "Search";

            textDirectory.EditValue = directory;

            list.BeginUnboundLoad();
            list.Nodes.Clear();

            List<VPK.File> files = getAllFiles();

            List<string> usedFiles = new List<string>();

            for (int f = 0; f < files.Count; f++)
            {
                VPK.File file = files[f];
                string path = file.path;

                if (!path.StartsWith(directory))
                    continue;

                path = path.Substring(directory.Length);

                string[] fileSplit = path.Split('/');

                if (fileSplit.Length > 1)
                {
                    // It's a directory
                    if (usedFiles.Contains(fileSplit[0]))
                        continue;

                    TreeListNode node = list.AppendNode(new object[] { fileSplit[0], "Folder" }, null);
                    node.Tag = directory + fileSplit[0] + "/";
                    node.StateImageIndex = 0;
                    usedFiles.Add(fileSplit[0]);
                } else
                {
                    // It's a file
                    TreeListNode node = list.AppendNode(new object[] { fileSplit[0], file.type, file.pack }, null);
                    node.Tag = directory + path;
                    node.StateImageIndex = 1;
                    usedFiles.Add(path);
                } 
            }

            list.EndUnboundLoad();
        }

        private void traverseDirectoryFiltered(string directory)
        {
            buttonUp.Enabled = (currentDirectory != "");
            buttonBack.Enabled = (previousDirectories.Count > 0);
            buttonForward.Enabled = (nextDirectories.Count > 0);

            textDirectory.EditValue = "Search results for " + filter;

            list.BeginUnboundLoad();
            list.Nodes.Clear();

            List<VPK.File> files = getAllFiles();

            List<string> usedFiles = new List<string>();

            for (int f = 0; f < files.Count; f++)
            {
                VPK.File file = files[f];
                string path = file.path;

                if (!path.StartsWith(directory))
                    continue;

                string[] fileSplit = path.Split('/');

                string dir = "";
                for (int j = 0; j < fileSplit.Length; j++)
                {
                    dir = dir + fileSplit[j] + "/";

                    if (!fileSplit[j].Contains(filter))
                        continue;

                    if (j < fileSplit.Length - 1)
                    {
                        // It's a directory
                        if (usedFiles.Contains(dir))
                            continue;

                        TreeListNode node = list.AppendNode(new object[] { path, "Folder" }, null);
                        node.Tag = dir;
                        node.StateImageIndex = 0;
                        usedFiles.Add(dir);
                    } else
                    {
                        // It's a file
                        TreeListNode node = list.AppendNode(new object[] { path, file.type, file.pack }, null);
                        node.Tag = path;
                        node.StateImageIndex = 1;
                        usedFiles.Add(path);
                    }
                }
            }

            list.EndUnboundLoad();
        }

        private List<VPK.File> getAllFiles()
        {
            List<VPK.File> files = new List<VPK.File>();
            foreach (VPK vpk in vpks.Values)
                files.AddRange(vpk.files.Values);
            files = files
                .GroupBy(x => x.path)
                .Select(y => y.First())
                .Where(x => x.path.Contains(filter))
                .OrderBy(x => x.path)
                .ToList();

            return files;
        }

        private void dirs_FocusedNodeChanged(object sender, DevExpress.XtraTreeList.FocusedNodeChangedEventArgs e)
        {
            if (dirs.FocusedNode == null || dirs.FocusedNode.Tag == null)
                return;

            string directory = dirs.FocusedNode.Tag.ToString();

            if (directory != currentDirectory)
            {
                previousDirectories.Push(currentDirectory);
                nextDirectories.Clear();
            }

            traverseDirectory(directory);
        }

        private void list_DoubleClick(object sender, EventArgs e)
        {
            TreeList tree = sender as TreeList;
            TreeListHitInfo hi = tree.CalcHitInfo(tree.PointToClient(Control.MousePosition));
            if (hi.Node != null)
            {
                string tag = hi.Node.Tag.ToString();
                if (tag.EndsWith("/"))
                {
                    // It's a folder
                    if (tag != currentDirectory)
                    {
                        previousDirectories.Push(currentDirectory);
                        nextDirectories.Clear();
                    }

                    traverseDirectory(tag);
                } else
                {
                    // It's a file
                    openSelected();
                }
            }
        }

        private void buttonUp_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (currentDirectory == "")
                return;

            previousDirectories.Push(currentDirectory);
            nextDirectories.Clear();

            if (currentDirectory.Contains("/"))
                currentDirectory = currentDirectory.Substring(0, currentDirectory.LastIndexOf("/"));

            if (currentDirectory.Contains("/"))
                currentDirectory = currentDirectory.Substring(0, currentDirectory.LastIndexOf("/") + 1);
            else
                currentDirectory = "";



            traverseDirectory(currentDirectory);
        }

        private void buttonBack_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (previousDirectories.Count > 0)
            {
                nextDirectories.Push(currentDirectory);
                traverseDirectory(previousDirectories.Pop());
            }
        }

        private void buttonForward_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (nextDirectories.Count > 0)
            {
                previousDirectories.Push(currentDirectory);
                traverseDirectory(nextDirectories.Pop());
            }
        }

        private void repositoryTextSearch_EditValueChanged(object sender, EventArgs e)
        {
            filter = ((TextEdit) sender).EditValue.ToString();
            if (filter != "")
                traverseDirectoryFiltered(currentDirectory);
            else
                traverseDirectory(currentDirectory);
        }

        private void extractSelected()
        {
            var nodes = list.Selection;
            List<string> values = new List<string>();
            foreach (TreeListNode node in nodes)
            {
                values.Add(node.Tag.ToString());
            }

            foreach(string filePath in values)
            {
                sourceSDK.extractFileFromVPKs(vpks, filePath, Application.StartupPath, sourceSDK);
            }

            string modPath = sourceSDK.GetModPath();
            Process.Start(modPath);
        }

        private void openSelected()
        {
            var nodes = list.Selection;
            List<string> values = new List<string>();
            foreach (TreeListNode node in nodes)
            {
                values.Add(node.Tag.ToString());
            }

            string modPath = sourceSDK.GetModPath();

            foreach (string filePath in values)
            {
                sourceSDK.extractFileFromVPKs(vpks, filePath, Application.StartupPath, sourceSDK);
                Process.Start("notepad", modPath + "\\" + filePath);
            }
        }

        private void list_SelectionChanged(object sender, EventArgs e)
        {

        }

        private void list_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point pt = list.PointToClient(MousePosition);
                TreeListHitInfo info = list.CalcHitInfo(pt);
                if (info.HitInfoType == HitInfoType.Cell)
                {
                    string tag = info.Node.Tag.ToString();
                    if (tag.EndsWith("/"))
                    {
                        // It's a folder
                    }
                    else
                    {
                        // It's a file
                        filePopupMenu.ShowPopup(MousePosition);
                    }
                }
            }
                
        }

        private void barButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            extractSelected();
        }

        private void barButtonItem3_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            openSelected();
        }
    }
}