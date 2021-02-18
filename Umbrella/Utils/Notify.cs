using System;
using System.IO;
using System.Windows.Media.Imaging;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Umbrella.Utils
{
    class Notify
    {
        public static void Show(string title, string message)
        {
            var template = "<toast><visual><binding template=\"ToastImageAndText03\"><image id=\"1\" src=\"file:///"+ Path.GetFullPath("NHack.png") + "\" /><text id=\"1\">" + title + "</text><text id=\"2\">" + message + "</text></binding></visual></toast>";

                var document = new XmlDocument();
                document.LoadXml(template);
                ToastNotificationManager.CreateToastNotifier("Umbrella Активатор").Show(new ToastNotification(document));
            

        }
    }
}
