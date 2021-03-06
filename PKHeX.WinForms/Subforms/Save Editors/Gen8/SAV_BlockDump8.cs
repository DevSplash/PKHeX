﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PKHeX.Core;
using static PKHeX.Core.SCBlockUtil;

namespace PKHeX.WinForms
{
    public partial class SAV_BlockDump8 : Form
    {
        private readonly SAV8SWSH SAV;
        private SCBlock CurrentBlock;

        public SAV_BlockDump8(SaveFile sav)
        {
            InitializeComponent();
            WinFormsUtil.TranslateInterface(this, Main.CurrentLanguage);
            SAV = (SAV8SWSH)sav;

            var blocks = SAV.AllBlocks.Select((z, i) => new ComboItem($"{z.Key:X8} - {i:0000} {(z.Type.IsBoolean() ? "Bool" : z.Type.ToString())}", (int)z.Key));
            CB_Key.InitializeBinding();
            CB_Key.DataSource = blocks.ToArray();

            var boolToggle = new[]
            {
                new ComboItem(nameof(SCTypeCode.Bool1), (int)SCTypeCode.Bool1),
                new ComboItem(nameof(SCTypeCode.Bool2), (int)SCTypeCode.Bool2),
                new ComboItem(nameof(SCTypeCode.Bool3), (int)SCTypeCode.Bool3),
            };
            CB_TypeToggle.InitializeBinding();
            CB_TypeToggle.DataSource = boolToggle;
        }

        private void CB_Key_SelectedIndexChanged(object sender, EventArgs e)
        {
            var key = (uint)WinFormsUtil.GetIndex(CB_Key);
            CurrentBlock = SAV.Blocks.GetBlock(key);
            L_Detail_R.Text = GetBlockSummary(CurrentBlock);
            if (CurrentBlock.Type.IsBoolean())
            {
                CB_TypeToggle.SelectedValue = (int)CurrentBlock.Type;
                CB_TypeToggle.Visible = true;
            }
            else
            {
                CB_TypeToggle.Visible = false;
            }
        }

        private void CB_TypeToggle_SelectedIndexChanged(object sender, EventArgs e)
        {
            var cType = CurrentBlock.Type;
            var cValue = (SCTypeCode)WinFormsUtil.GetIndex(CB_TypeToggle);
            if (cType == cValue)
                return;
            CurrentBlock.Type = cValue;
            L_Detail_R.Text = GetBlockSummary(CurrentBlock);
        }

        private void B_ExportAll_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() != DialogResult.OK)
                return;
            var path = fbd.SelectedPath;
            var blocks = SAV.AllBlocks;
            ExportAllBlocks(blocks, path);
        }

        private static void ExportAllBlocks(IEnumerable<SCBlock> blocks, string path)
        {
            foreach (var b in blocks.Where(z => z.Data.Length != 0))
                File.WriteAllBytes(Path.Combine(path, $"{GetBlockFileNameWithoutExtension(b)}.bin"), b.Data);
        }

        private void B_ImportFolder_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() != DialogResult.OK)
                return;

            var failed = ImportBlocksFromFolder(fbd.SelectedPath, SAV);
            if (failed.Count != 0)
            {
                var msg = string.Join(Environment.NewLine, failed);
                WinFormsUtil.Error("Failed to import:", msg);
            }
        }

        private void B_ImportCurrent_Click(object sender, EventArgs e) => ImportSelectBlock(CurrentBlock);
        private void B_ExportCurrent_Click(object sender, EventArgs e) => ExportSelectBlock(CurrentBlock);

        private void B_ExportAllSingle_Click(object sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog { FileName = "raw.bin" };
            if (sfd.ShowDialog() != DialogResult.OK)
                return;
            var blocks = SAV.Blocks.BlockInfo;
            ExportAllBlocksAsSingleFile(blocks, sfd.FileName, CHK_DataOnly.Checked, CHK_Key.Checked, CHK_Type.Checked, CHK_FakeHeader.Checked);
        }

        private void B_LoadOld_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog { FileName = "main" };
            if (ofd.ShowDialog() != DialogResult.OK)
                return;
            TB_OldSAV.Text = ofd.FileName;
            if (!string.IsNullOrWhiteSpace(TB_NewSAV.Text))
                CompareSaves();
        }

        private void B_LoadNew_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog { FileName = "main" };
            if (ofd.ShowDialog() != DialogResult.OK)
                return;
            TB_NewSAV.Text = ofd.FileName;
            if (!string.IsNullOrWhiteSpace(TB_OldSAV.Text))
                CompareSaves();
        }

        private void CompareSaves()
        {
            var p1 = TB_OldSAV.Text;
            var p2 = TB_NewSAV.Text;

            var f1 = new FileInfo(p1);
            if (!SaveUtil.IsSizeValid((int)f1.Length))
                return;
            var f2 = new FileInfo(p1);
            if (!SaveUtil.IsSizeValid((int)f2.Length))
                return;

            var s1 = SaveUtil.GetVariantSAV(p1);
            if (!(s1 is SAV8SWSH w1))
                return;
            var s2 = SaveUtil.GetVariantSAV(p2);
            if (!(s2 is SAV8SWSH w2))
                return;

            var compare = new SCBlockCompare(w1, w2);
            richTextBox1.Lines = compare.Summary().ToArray();
        }

        private static void ExportSelectBlock(SCBlock block)
        {
            var name = GetBlockFileNameWithoutExtension(block);
            using var sfd = new SaveFileDialog {FileName = $"{name}.bin"};
            if (sfd.ShowDialog() != DialogResult.OK)
                return;
            File.WriteAllBytes(sfd.FileName, block.Data);
        }

        private static void ImportSelectBlock(SCBlock blockTarget)
        {
            var key = blockTarget.Key;
            var data = blockTarget.Data;
            using var ofd = new OpenFileDialog {FileName = $"{key:X8}.bin"};
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            var path = ofd.FileName;
            var file = new FileInfo(path);
            if (file.Length != data.Length)
            {
                WinFormsUtil.Error(string.Format(MessageStrings.MsgFileSize, $"0x{file.Length:X8}"));
                return;
            }

            var bytes = File.ReadAllBytes(path);
            bytes.CopyTo(data, 0);
        }
    }
}
