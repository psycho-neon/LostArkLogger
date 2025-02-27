﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LostArkLogger
{
    public partial class Overlay : Form
    {
        enum OverlayType {
            TotalDamage,
            SkillDamage,
            Other
        }
        public Overlay()
        {
            InitPens();
            Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            SetStyle(ControlStyles.ResizeRedraw, true);
        }
        internal void AddSniffer(Parser s)
        {
            sniffer = s;
            sniffer.onDamageEvent += AddDamageEvent;
            sniffer.onNewZone += NewZone;
        }
        public void NewZone()
        {
            Events = new ConcurrentBag<LogInfo>();
            Damages.Clear();
            GroupDamages.Clear();
            SkillDmg.Clear();

            SwitchOverlay(OverlayType.TotalDamage);
        }
        private DateTime startCombatTime = DateTime.Now;
        ConcurrentBag<LogInfo> Events = new ConcurrentBag<LogInfo>();
        private OverlayType currentOverlay = OverlayType.TotalDamage;
        string owner = "";
        public ConcurrentDictionary<String, UInt64> Damages = new ConcurrentDictionary<string, ulong>();
        public ConcurrentDictionary<String, UInt64> GroupDamages = new ConcurrentDictionary<string, ulong>();
        public ConcurrentDictionary<String, ConcurrentDictionary<String, UInt64>> SkillDmg = new ConcurrentDictionary<string, ConcurrentDictionary<string, ulong>>();
        Font font = new Font("Helvetica", 10);
        void AddDamageEvent(LogInfo log)
        {
            if(GroupDamages.Count == 0) startCombatTime = DateTime.Now;
            Events.Add(log);
            if (!GroupDamages.ContainsKey(log.Source)) GroupDamages[log.Source] = 0;
            GroupDamages[log.Source] += log.Damage;

            if(!SkillDmg.ContainsKey(log.Source))
                SkillDmg[log.Source] = new ConcurrentDictionary<string, ulong>();
            if(!SkillDmg[log.Source].ContainsKey(log.SkillName))
                SkillDmg[log.Source][log.SkillName] = 0;

            SkillDmg[log.Source][log.SkillName] += log.Damage;

            SwitchOverlay(currentOverlay);
        }
        internal Parser sniffer;
        List<Brush> brushes = new List<Brush>();
        Brush black = new SolidBrush(Color.White);
        void InitPens()
        {
            String[] colors = {"#3366cc", "#dc3912", "#ff9900", "#109618", "#990099", "#0099c6", "#dd4477", "#66aa00", "#b82e2e", "#316395", "#994499", "#22aa99", "#aaaa11", "#6633cc", "#e67300", "#8b0707", "#651067", "#329262", "#5574a6", "#3b3eac", "#b77322", "#16d620", "#b91383", "#f4359e", "#9c5935", "#a9c413", "#2a778d", "#668d1c", "#bea413", "#0c5922", "#743411" };
            foreach(var color in colors) brushes.Add(new SolidBrush(ColorTranslator.FromHtml(color)));
        }
        int barHeight = 20;
        public static string FormatNumber(UInt64 n) // https://stackoverflow.com/questions/30180672/string-format-numbers-to-millions-thousands-with-rounding
        {
            if (n < 1000) return n.ToString();
            if (n < 10000) return String.Format("{0:#,.##}K", n - 5);
            if (n < 100000) return String.Format("{0:#,.#}K", n - 50);
            if (n < 1000000) return String.Format("{0:#,.}K", n - 500);
            if (n < 10000000) return String.Format("{0:#,,.##}M", n - 5000);
            if (n < 100000000) return String.Format("{0:#,,.#}M", n - 50000);
            if (n < 1000000000) return String.Format("{0:#,,.}M", n - 500000);
            return String.Format("{0:#,,,.##}B", n - 5000000);
        }
        public Rectangle GetSpriteLocation(int i)
        {
            var imageSize = 64;
            var x = i % 16;
            var y = i / 16;
            return new Rectangle(x * imageSize, y * imageSize, imageSize, imageSize);
        }
        public String[] ClassIconIndex = { "Destroyer", "unk", "Arcana", "Berserker", "Wardancer", "Deadeye", "MartialArtist", "Gunlancer", "Gunner", "Scrapper", "Mage", "Summoner", "Warrior",
         "Soulfist", "Sharpshooter", "Artillerist", "Bard", "Glavier", "Assassin", "Deathblade", "Shadowhunter", "Paladin", "Scouter", "Reaper", "FemaleGunner", "Gunslinger", "MaleMartialArtist", "Striker", "Sorceress" };
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            e.Graphics.FillRectangle(brushes[10], 0, 0, Size.Width, barHeight);
            var title = "DPS Meter";
            if(currentOverlay == OverlayType.SkillDamage) title = "Damage details - " + owner;

            var titleBar = e.Graphics.MeasureString(title, font);
            var heightBuffer = (barHeight - titleBar.Height) / 2;
            e.Graphics.DrawString(title, font, black, 5, heightBuffer);
            if (Damages.Count == 0) return;
            var maxDamage = Damages.Max(b => b.Value);
            var totalDamage = Damages.Values.Sum(b=>(Single)b);
            var orderedDamages = Damages.OrderByDescending(b => b.Value);
            for (var i = 0; i < Damages.Count && i < 8; i++)
            {
                var elapsed = (DateTime.Now - startCombatTime).TotalSeconds;
                var playerDmg = orderedDamages.ElementAt(i);
                var barWidth = ((Single)playerDmg.Value / maxDamage) * Size.Width;
                if (barWidth < .3f) continue;
                e.Graphics.FillRectangle(brushes[i], 0, (i + 1) * barHeight, barWidth, barHeight);
                var dps = FormatNumber((ulong)(playerDmg.Value / elapsed));
                var formattedDmg = FormatNumber(playerDmg.Value) + " (" + dps + ", " + (100f * playerDmg.Value / totalDamage).ToString("#.0") + "%)";
                var nameOffset = 0;
                if (playerDmg.Key.Contains("("))
                {
                    var className = playerDmg.Key.Substring(playerDmg.Key.IndexOf("(") + 1);
                    className = className.Substring(0, className.IndexOf(")"));
                    e.Graphics.DrawImage(Properties.Resources.class_symbol_0, new Rectangle(2, (i + 1) * barHeight + 2, barHeight - 4, barHeight - 4), GetSpriteLocation(Array.IndexOf(ClassIconIndex, className)), GraphicsUnit.Pixel);
                    nameOffset += 16;
                }
                var edge = e.Graphics.MeasureString(formattedDmg, font);
                e.Graphics.DrawString(playerDmg.Key, font, black, nameOffset + 5, (i + 1) * barHeight + heightBuffer);
                e.Graphics.DrawString(formattedDmg, font, black, Size.Width - edge.Width, (i + 1) * barHeight + heightBuffer);
            }
            ControlPaint.DrawSizeGrip(e.Graphics, BackColor, ClientSize.Width - 16, ClientSize.Height - 16, 16, 16);
        }

        [DllImport("user32.dll")] static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        private void Overlay_MouseDown(object sender, MouseEventArgs e) {
            if(e.Button == MouseButtons.Left) {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);

                var index = (int)Math.Floor(e.Location.Y / (float)barHeight - 1);
                if(index < 0 || index > Damages.Count) return;
                if(currentOverlay == OverlayType.TotalDamage) {
                    owner = Damages.OrderByDescending(b => b.Value).ElementAt(index).Key;
                    SwitchOverlay(OverlayType.SkillDamage);
                }
            }
            if(e.Button == MouseButtons.Right) {
                SwitchOverlay(OverlayType.TotalDamage);
            }
        }
        private void SwitchOverlay(OverlayType type) {
            currentOverlay = type;

            if(type == OverlayType.TotalDamage) {
                Damages = GroupDamages;
            }
            if(type == OverlayType.SkillDamage) {
                if(SkillDmg.ContainsKey(owner))
                    Damages = SkillDmg[owner];
                else
                    Damages.Clear();
            }

            Invalidate();
        }
        protected override void WndProc(ref Message m)
        {
            const int wmNcHitTest = 0x84;
            const int htBottomLeft = 16;
            const int htBottomRight = 17;
            if (m.Msg == wmNcHitTest)
            {
                var x = (int)(m.LParam.ToInt64() & 0xFFFF);
                var y = (int)((m.LParam.ToInt64() & 0xFFFF0000) >> 16);
                var pt = PointToClient(new Point(x, y));
                var clientSize = ClientSize;
                if (pt.X >= clientSize.Width - 16 && pt.Y >= clientSize.Height - 16 && clientSize.Height >= 16)
                {
                    m.Result = (IntPtr)(IsMirrored ? htBottomLeft : htBottomRight);
                    return;
                }
            }
            base.WndProc(ref m);
        }

        public new void Dispose()
        {
            sniffer.onDamageEvent -= AddDamageEvent;
            sniffer.onNewZone -= NewZone;
            base.Dispose();
        }
    }
}
