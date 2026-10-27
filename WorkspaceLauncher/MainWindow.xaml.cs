using System;
using System.Windows;
using System.Windows.Controls;
using WorkspaceLauncher.ViewModels;

namespace WorkspaceLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Locate window buttons inside the Custom Window Chrome Template and wire up their behaviors
            if (GetTemplateChild("btnMinimize") is Button btnMinimize)
            {
                btnMinimize.Click += (s, e) => this.WindowState = WindowState.Minimized;
            }

            if (GetTemplateChild("btnMaximize") is Button btnMaximize)
            {
                btnMaximize.Click += (s, e) => 
                {
                    this.WindowState = (this.WindowState == WindowState.Maximized) 
                        ? WindowState.Normal 
                        : WindowState.Maximized;
                };
            }

            if (GetTemplateChild("btnClose") is Button btnClose)
            {
                btnClose.Click += (s, e) => this.Close();
            }
        }
    }
}