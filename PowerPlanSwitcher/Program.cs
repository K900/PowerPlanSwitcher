using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PowerPlanSwitcher
{
    static class Program
    {
        private static NotifyIcon trayIcon;
        private static string control_exe = Path.Combine(
                Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), "system32"),
                "control.exe");

        private const string VERSION = "0.1";

        public static void updateMenu(ContextMenuStrip menu)
        {
            menu.Items.Clear();

            foreach (PowerPlan plan in PowerPlanManager.FindAll())
            {
                ToolStripMenuItem item = new ToolStripMenuItem();
                item.Text = plan.Name;
                item.Tag = plan.GUID;
                item.Checked = (plan.GUID == PowerPlanManager.Active.GUID);

                item.Click += new EventHandler(delegate(object sender, EventArgs e)
                {
                    PowerPlanManager.Active = new PowerPlan {
                        Name = null,
                        GUID = (Guid)((ToolStripMenuItem)sender).Tag 
                    };
                });

                menu.Items.Add(item);
            }

            menu.Items.Add(new ToolStripSeparator());

            // Charge indicator
            ToolStripMenuItem charge = new ToolStripMenuItem();
            charge.Text = chargeString();
            charge.Enabled = false;

            menu.Items.Add(charge);

            // Version info
            ToolStripMenuItem version = new ToolStripMenuItem();
            version.Text = String.Format("Version {0}", VERSION);
            version.Enabled = false;

            menu.Items.Add(version);

            menu.Items.Add(new ToolStripSeparator());

            // Control panel shortcut
            ToolStripMenuItem cpanel = new ToolStripMenuItem();
            cpanel.Text = "More settings...";
            cpanel.Click += new EventHandler(delegate(object sender, EventArgs e)
            {
                Process.Start(control_exe, "/name Microsoft.PowerOptions");
            });

            menu.Items.Add(cpanel);

            // Exit
            ToolStripMenuItem exit = new ToolStripMenuItem();
            exit.Text = "Exit";
            exit.Click += new EventHandler(delegate(object sender, EventArgs e)
            {
                trayIcon.Visible = false;
                Application.Exit();
            });

            menu.Items.Add(exit);
        }

        static void Main()
        {
            // Set up icon
            trayIcon = new NotifyIcon();
            updateIcon();
            trayIcon.MouseUp += new MouseEventHandler(trayIcon_MouseUp);

            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);

            // Create menu
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Opening += new CancelEventHandler(menu_Opening);
            updateMenu(menu);

            // Add menu and start
            trayIcon.ContextMenuStrip = menu;
            trayIcon.Visible = true;
            
            Application.Run();
        }

        static void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.StatusChange)
            {
                updateIcon();
            }
        }

        private static bool isCharging()
        {
            return SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;
        }

        private static int chargeLevel()
        {
            return (int)Math.Round(SystemInformation.PowerStatus.BatteryLifePercent * 100);
        }

        private static string chargeString()
        {
            string format;
            if (isCharging())
            {
                format = "Charging: {0}%";
            }
            else
            {
                format = "Discharging: {0}%";
            }
            return String.Format(format, chargeLevel());
        }

        private static void updateIcon()
        {
            bool online = isCharging();
            int percent = chargeLevel();

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

            trayIcon.Icon = Icon.FromHandle(iconBitmap.GetHicon());
            trayIcon.Text = chargeString();
        }

        static void trayIcon_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                System.Reflection.MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                mi.Invoke(trayIcon, null);
            };
        }

        static void menu_Opening(object sender, CancelEventArgs e)
        {
            updateMenu((ContextMenuStrip)sender);
        }   
    }
}
