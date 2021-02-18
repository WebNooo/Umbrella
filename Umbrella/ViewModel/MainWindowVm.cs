using System.Windows.Controls;
using PropertyChanged;
using Umbrella.Interfaces;
using Umbrella.Views.Pages;

namespace Umbrella.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowVm
    {
        public string defaultTitle = "Umbrella Активатор";
        public string Title { get; set; }

        private Page _pageContent;

        public Page PageContent
        {
            get => _pageContent;
            set
            {
                _pageContent = value;
                if (_pageContent.DataContext is ILogin login)
                {
                    login.LoginStatus += status =>
                    {
                        PageContent = status switch
                        {
                            0 => new Login {DataContext = new LoginVM()},
                            1 => new Main { DataContext = new MainVM()},
                            2 => new Update { DataContext = new UpdateVM()},
                            _ => PageContent
                        };
                    };
                }

                if (_pageContent.DataContext is IViewModel main)
                {
                    main.UpdateTitle += title =>
                    {
                            Title = defaultTitle + ", " + title;
                    };
                }


            }
        }

        public MainWindowVm()
        {
            Title = defaultTitle;
            if (Properties.Settings.Default.username != "" && 
                Properties.Settings.Default.token != "" && 
                Properties.Settings.Default.session != "")
            {
                PageContent = new Main { DataContext = new MainVM() };
            }
            else
            {
                PageContent = new Login { DataContext = new LoginVM() };
            }
        }

    }
}
