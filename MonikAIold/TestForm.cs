// File: TestForm.cs
// Created: 10.01.2018
// 
// See <summary> tags for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Windows.Automation;
using System.Windows.Forms;
using NDde.Client;

namespace MonikAI
{
    public partial class TestForm : Form
    {
        private ManagementEventWatcher w;

        public TestForm()
        {
            this.InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void TestForm_Load(object sender, EventArgs e)
        {
            //// Process start
            WqlEventQuery q;
            try
            {
                q = new WqlEventQuery();
                q.EventClassName = "Win32_ProcessStartTrace";
                this.w = new ManagementEventWatcher(q);
                this.w.EventArrived += this.WMIEventArrived;
                this.w.Start();
            }
            catch (Exception ex)
            {
                this.textBoxLog.Text += $"An error occured starting WMI: {ex.Message}\r\n";
            }

            //// Browser tabs open
            // Firefox
            this.textBoxLog.Text += $"Firefox URL: {this.GetFirefoxURL()}\r\n";
            // Chrome
            this.textBoxLog.Text += $"Chrome URL: {this.GetChromeURL()}\r\n";
            // IE
        }

        private string GetFirefoxURL()
        {
            var firefoxProcesses = Process.GetProcessesByName("firefox");
            foreach (var firefoxProcess in firefoxProcesses.Where(x => x.MainWindowHandle != IntPtr.Zero))
            {
                var element = AutomationElement.FromHandle(firefoxProcess.MainWindowHandle);
                element = element.FindFirst(TreeScope.Subtree,
                      new AndCondition(
                          new PropertyCondition(AutomationElement.NameProperty, "Search or enter address"),
                          new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)));
                var url = ((ValuePattern)element.GetCurrentPattern(ValuePattern.Pattern)).Current.Value;
                return url;
            }
            return "";
        }

        public string GetChromeURL()
        {
            Process[] procsChrome = Process.GetProcessesByName("chrome");

            if (procsChrome.Length <= 0)
                return "";

            foreach (Process proc in procsChrome)
            {
                // the chrome process must have a window 
                if (proc.MainWindowHandle == IntPtr.Zero)
                    continue;

                // to find the tabs we first need to locate something reliable - the 'New Tab' button 
                AutomationElement root = AutomationElement.FromHandle(proc.MainWindowHandle);
                var SearchBar = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Address and search bar"));
                if (SearchBar != null)
                    return (string)SearchBar.GetCurrentPropertyValue(ValuePatternIdentifiers.ValueProperty);
            }

            return "";
        }

        private void WMIEventArrived(object sender, EventArrivedEventArgs e)
        {
            var processName = "ERROR";
            foreach (var property in e.NewEvent.Properties)
            {
                if (property.Name == "ProcessName")
                {
                    processName = (string)property.Value;
                    break;
                }
            }
            this.textBoxLog.Text += $"Process started: {processName}\r\n";
        }

        private void TestForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.w?.Stop();
        }
    }
}
