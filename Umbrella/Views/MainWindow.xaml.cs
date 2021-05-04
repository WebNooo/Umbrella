using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Umbrella.ViewModel;
using Application = System.Windows.Application;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Umbrella
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notify;
        public MainWindow()
        {
            //var app = new Core.App();
            InitializeComponent();
            DataContext = new MainWindowVm();
            notify = new NotifyIcon
            {
                Icon = new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Images/NHack.ico"))?.Stream),
                Text = "Umbrella Активатор"
            };

            notify.Click += (s, e) =>
            {
                notify.Visible = false;
                //Visibility = Visibility.Visible;
                Show();
            };

        }


        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                notify.Visible = true;
                Hide();
            }

            base.OnStateChanged(e);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = MessageBox.Show("Вы точно хотите закрыть программу?", "Завершение программы", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != System.Windows.Forms.DialogResult.Yes;
        }
    }
}
