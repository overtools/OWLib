using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace TankView {
    public partial class AboutPage {
        public string ProgramNameProp => $"TankView v{Assembly.GetExecutingAssembly().GetName().Version}";
        public string TagLineProp => "TankView uses TankLib, TACTLib, and DataTool.";

        public string DisclaimerL1Prop => "This project is not affiliated with Blizzard Entertainment, Inc.";
        public string DisclaimerL2Prop => "All trademarks referenced herein are the properties of their respective owners.";
        public string DisclaimerL3Prop => $"©{DateTime.Now.Year} Blizzard Entertainment, Inc. All rights reserved.";

        public AboutPage() {
            InitializeComponent();
            if(Debugger.IsAttached) {
                ContinueClick(this, new RoutedEventArgs());
            }
        }

        private void ContinueClick(object sender, RoutedEventArgs e) {
            Application.Current.MainWindow = new MainWindow();
            Application.Current.MainWindow.Show();
            Close();
        }

        private void FirstChance(object sender, EventArgs e) {
            ContinueButton.Focus();
        }
    }
}

