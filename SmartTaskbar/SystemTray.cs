﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SmartTaskbar.Properties;
using static SmartTaskbar.SafeNativeMethods;

namespace SmartTaskbar
{
    internal class SystemTray : ApplicationContext
    {
        private readonly NotifyIcon notifyIcon;
        private readonly ContextMenuStrip contextMenuStrip;
        private readonly ToolStripMenuItem about;
        private readonly ToolStripMenuItem smallIcon;
        private readonly ToolStripMenuItem animation;
        private readonly ToolStripMenuItem auto_size;
        private readonly ToolStripMenuItem auto_display;
        private readonly ToolStripMenuItem exit;

        private readonly NotifierLauncher notifierLauncher = new NotifierLauncher();

        public SystemTray()
        {
            #region Initialization

            var resource = new ResourceCulture();
            var font = new Font("Segoe UI", 9F);
            about = new ToolStripMenuItem
            {
                Text = resource.GetString(nameof(about)),
                Font = font
            };
            smallIcon = new ToolStripMenuItem
            {
                Text = resource.GetString(nameof(smallIcon)),
                Font = font
            };
            animation = new ToolStripMenuItem
            {
                Text = resource.GetString(nameof(animation)),
                Font = font
            };
            auto_size = new ToolStripMenuItem
            {
                Text = resource.GetString(nameof(auto_size)),
                Font = font
            };
            auto_display = new ToolStripMenuItem
            {
                Text = resource.GetString(nameof(auto_display)),
                Font = font
            };
            exit = new ToolStripMenuItem
            {
                Text = resource.GetString(nameof(exit)),
                Font = font
            };
            contextMenuStrip = new ContextMenuStrip
            {
                Renderer = new Win10Renderer()
            };

            contextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                about,
                smallIcon,
                animation,
                new ToolStripSeparator(),
                auto_display,
                auto_size,
                new ToolStripSeparator(),
                exit
            });

            notifyIcon = new NotifyIcon
            {
                ContextMenuStrip = contextMenuStrip,
                Text = @"SmartTaskbar v1.1.9",
                Icon = Environment.OSVersion.Version.Major.ToString() == "10" ? Resources.logo_32 : Resources.logo_blue_32,
                Visible = true
            };

            #endregion

            #region Load Event

            about.Click += (s, e) => Process.Start(@"https://github.com/ChanpleCai/SmartTaskbar/releases");

            Settings.Default.PropertyChanged += (s, e) =>
            {
                Settings.Default.Save();
                switch ((AutoModeType) Settings.Default.TaskbarState)
                {
                    case AutoModeType.Display:
                        smallIcon.Enabled = auto_display.Checked = true;
                        auto_size.Checked = false;
                        ChangeDisplayState();
                        SetIconSize(Settings.Default.IconSize);
                        break;
                    case AutoModeType.Size:
                        smallIcon.Enabled = auto_display.Checked = false;
                        auto_size.Checked = true;
                        ChangeIconSize();
                        Show();
                        break;
                    case AutoModeType.None:
                        smallIcon.Enabled = true;
                        auto_display.Checked = auto_size.Checked = false;
                        break;
                }
            };

            smallIcon.Click += (s, e) =>
            {
                Settings.Default.IconSize = smallIcon.Checked ? 0 : 1;
                SetIconSize(Settings.Default.IconSize);
            };

            animation.Click += (s, e) => animation.Checked = ChangeTaskbarAnimation();

            auto_size.Click += (s, e) => Settings.Default.TaskbarState = auto_size.Checked ? (int) AutoModeType.None : (int) AutoModeType.Size;

            auto_display.Click += (s, e) => Settings.Default.TaskbarState = auto_display.Checked ? (int) AutoModeType.None : (int) AutoModeType.Display;

            exit.Click += (s, e) =>
            {
                notifierLauncher.Stop();
                Reset();
                notifyIcon.Dispose();
                Application.Exit();
            };

            notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Right)
                    return;

                notifierLauncher.Resume();

                animation.Checked = GetTaskbarAnimation();

                if (!smallIcon.Enabled) return;

                SetIconSize(Settings.Default.IconSize);
                smallIcon.Checked = GetIconSize() == SmallIcon;
            };

            notifyIcon.MouseDoubleClick += (s, e) =>
            {
                Settings.Default.TaskbarState = (int) AutoModeType.None;
                SetIconSize(Settings.Default.IconSize);
                if (IsHide())
                    Show();
                else
                    Hide();
            };

            #endregion

            #region Load Settings

            if (Settings.Default.TaskbarState == -1)
            {
                //Run the software for the first time
                Settings.Default.TaskbarState = (int) AutoModeType.Display;
                Settings.Default.IconSize = GetIconSize();
            }
            else
            {
                switch ((AutoModeType) Settings.Default.TaskbarState)
                {
                    case AutoModeType.Display:
                        smallIcon.Enabled = auto_display.Checked = true;
                        auto_size.Checked = false;
                        break;
                    case AutoModeType.Size:
                        smallIcon.Enabled = auto_display.Checked = false;
                        auto_size.Checked = true;
                        break;
                    case AutoModeType.None:
                        smallIcon.Enabled = true;
                        auto_display.Checked = auto_size.Checked = false;
                        break;
                }
                Reset();
                smallIcon.Checked = GetIconSize() == SmallIcon;
            }

            #endregion
        }
    }
}