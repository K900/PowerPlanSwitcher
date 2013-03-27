using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PowerPlanSwitcher
{
    internal static class Program
    {
        private const string Version = "0.1";
        private static NotifyIcon _trayIcon;

        private static readonly string ControlExe = Path.Combine(
            Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), "system32"),
            "control.exe");

        #region Information / helpers
        private static bool IsCharging()
        {
            return SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;
        }

        private static int ChargeLevel()
        {
            return (int)Math.Round(SystemInformation.PowerStatus.BatteryLifePercent * 100);
        }

        private static string ChargeString()
        {
            var format = IsCharging() ? Strings.Charging : Strings.Discharging;
            return String.Format(format, ChargeLevel());
        }
        #endregion

        #region UI things
        public static void UpdateMenu(ContextMenuStrip menu)
        {
            menu.Items.Clear();

            foreach (var plan in PowerPlanManager.FindAll())
            {
                var item = new ToolStripMenuItem
                    {
                        Text = plan.Name,
                        Tag = plan.Guid,
                        Checked = (plan.Guid == PowerPlanManager.Active.Guid)
                    };

                item.Click += delegate(object sender, EventArgs e)
                    {
                        PowerPlanManager.Active = new PowerPlan
                            {
                                Name = null,
                                Guid = (Guid) ((ToolStripMenuItem) sender).Tag
                            };
                    };

                menu.Items.Add(item);
            }

            menu.Items.Add(new ToolStripSeparator());

            // Charge indicator
            var charge = new ToolStripMenuItem {Text = ChargeString(), Enabled = false};

            menu.Items.Add(charge);

            // Version info
            var version = new ToolStripMenuItem {Text = String.Format(Strings.VersionString, Version), Enabled = false};

            menu.Items.Add(version);

            menu.Items.Add(new ToolStripSeparator());

            // Control panel shortcut
            var cpanel = new ToolStripMenuItem {Text = Strings.MoreSettings};
            cpanel.Click += (sender, e) => Process.Start(ControlExe, "/name Microsoft.PowerOptions");

            menu.Items.Add(cpanel);

            // Exit
            var exit = new ToolStripMenuItem {Text = Strings.Exit};
            exit.Click += delegate
                {
                    _trayIcon.Visible = false;
                    Application.Exit();
                };

            menu.Items.Add(exit);
        }

        private static void UpdateIcon()
        {
            var online = IsCharging();
            var percent = ChargeLevel();

            Bitmap iconBitmap;

            if (percent < 20)
            {
                iconBitmap = online ? Icons.gpm_battery_000_charging : Icons.gpm_battery_000;
            }
            else if (percent < 40)
            {
                iconBitmap = online ? Icons.gpm_battery_020_charging : Icons.gpm_battery_020;
            }
            else if (percent < 60)
            {
                iconBitmap = online ? Icons.gpm_battery_040_charging : Icons.gpm_battery_040;
            }
            else if (percent < 80)
            {
                iconBitmap = online ? Icons.gpm_battery_060_charging : Icons.gpm_battery_060;
            }
            else if (percent < 100)
            {
                iconBitmap = online ? Icons.gpm_battery_080_charging : Icons.gpm_battery_080;
            }
            else
            {
                iconBitmap = online ? Icons.gpm_battery_100_charging : Icons.gpm_battery_100;
            }

            _trayIcon.Icon = Icon.FromHandle(iconBitmap.GetHicon());
            _trayIcon.Text = ChargeString();
        }
        #endregion

        #region Event handlers
        private static void PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.StatusChange)
            {
                UpdateIcon();
            }
        }

        private static void MenuOpening(object sender, CancelEventArgs e)
        {
            UpdateMenu((ContextMenuStrip)sender);
        }
        #endregion

        #region Left click tray icon superhack
        private static void TrayIconClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                                                   BindingFlags.Instance | BindingFlags.NonPublic);
            mi.Invoke(_trayIcon, null);
        }
        #endregion

        private static void Main()
        {
            // Set up icon
            _trayIcon = new NotifyIcon();
            UpdateIcon();
            _trayIcon.MouseUp += TrayIconClick;

            SystemEvents.PowerModeChanged += PowerModeChanged;

            // Create menu
            var menu = new ContextMenuStrip();
            menu.Opening += MenuOpening;
            UpdateMenu(menu);

            // Add menu and start
            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.Visible = true;

            Application.Run();
        }                                    
    }
}