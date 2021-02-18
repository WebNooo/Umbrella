using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using PropertyChanged;
using Umbrella.Interfaces;
using Umbrella.Utils;

namespace Umbrella.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class UpdateVM : ILogin
    {
        public Action<int> LoginStatus { get; set; }

        public double ProgressValue { get; set; } = 0.0;
        public double ProgressMax { get; set; } = 100.0;

        public UpdateVM()
        {
            new Thread(async () =>
            {
                var status = await Download();

                if (status)
                {
                    if (File.Exists("Update.exe"))
                    {
                        Process.Start("Update.exe");
                        Process.GetCurrentProcess().Kill();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка обновления! Программа обновления отсутствует!");
                    }
                }

            }).Start();
        }

        public async Task<bool> Download()
        {
            var client = new HttpClient();
            //
            var response = await client.GetAsync(RequestManager.ApiUri(RequestManager.Api.PWS) + "?download",
                HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Ошибка получения файлов при обновлении!");
                return false;
            }


            var total = response.Content.Headers.ContentLength.HasValue
                ? response.Content.Headers.ContentLength.Value
                : -1L;

            Application.Current.Dispatcher.Invoke(() =>
            {
                ProgressMax = Math.Round((double)total / 1024 / 1000, 2);
            });

            var canReportProgress = total != -1;

            await using var stream = await response.Content.ReadAsStreamAsync();
            var totalRead = 0L;
            var buffer = new byte[4096];
            var isMoreToRead = true;
            await using Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new FileStream("update.zip", FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            do
            {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (read == 0)
                {
                    isMoreToRead = false;
                }
                else
                {
                    var data = new byte[read];
                    await fileStream.WriteAsync(buffer, 0, read);

                    totalRead += read;

                    if (canReportProgress)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ProgressValue = Math.Round((double) totalRead / 1024 / 1000, 2);
                        });
                    }
                }
            } while (isMoreToRead);

            return true;
        }
    }
}