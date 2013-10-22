using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Windows;
using DevExpress.Xpf.Core;
using log4net;

namespace Updater {
    /// <summary>
    /// Interaction logic for UpdaterWindow.xaml
    /// </summary>
    public partial class UpdaterWindow : DXWindow {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private WebClient _webClient;
        private readonly Stopwatch _stopWatch = new Stopwatch();
        private string _fileName;
        public UpdaterWindow(string urlAddress, string localAddress) {
            InitializeComponent();

            DownloadFile(urlAddress, localAddress);
        }
        public void DownloadFile(string urlAddress, string localAddress) {
            _fileName = localAddress;
            using (_webClient = new WebClient()) {
                _webClient.DownloadFileCompleted += Completed;
                _webClient.DownloadProgressChanged += ProgressChanged;

                try {
                    // The variable that will be holding the url address
                    var url = new Uri(urlAddress);

                    // Start the stopwatch which we will be using to calculate the download speed
                    _stopWatch.Start();

                    // Start downloading the file
                    _webClient.DownloadFileAsync(url, localAddress);

                }
                catch (Exception ex) {
                    _log.ErrorFormat(ex.Message);
                }
            }
        }

        // The event that will fire whenever the progress of the WebClient is changed
        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            try {
                // Calculate download speed and output it to label
                if (progressBar.Value != (Convert.ToDouble(e.BytesReceived) / 1024 / _stopWatch.Elapsed.TotalSeconds))
                    lblSpeed.Content = (Convert.ToDouble(e.BytesReceived) / 1024 / _stopWatch.Elapsed.TotalSeconds).ToString("0.00") + " kb/s";

                // Update the progressbar percentage only when the value is not the same (to avoid updating the control constantly)
                if (progressBar.Value != e.ProgressPercentage)
                    progressBar.Value = e.ProgressPercentage;

                // Show the percentage on our label (update only if the value isn't the same to avoid updating the control constantly)
                //if (progressBar.Value != e.ProgressPercentage)
                    progressBar.Value = e.ProgressPercentage;

                // Update the label with how much data have been downloaded so far and the total size of the file we are currently downloading
                lblDownloaded.Content = (Convert.ToDouble(e.BytesReceived) / 1024 / 1024).ToString("0.00") + " Mb's" + "  /  " + (Convert.ToDouble(e.TotalBytesToReceive) / 1024 / 1024).ToString("0.00") + " Mb's";
            }
            catch (Exception ex) {
                _log.ErrorFormat(ex.Message);
            }
        }

        // The event that will trigger when the WebClient is completed
        private void Completed(object sender, AsyncCompletedEventArgs e) {
            try {
                _stopWatch.Reset();
                if (!e.Cancelled) {
                    lblMessage.Content = "Загрузка завершена!";
                    btnCancel.Content = @"Установить";
                    btnCancel.Tag = 1;
                }
                else {
                    lblMessage.Content = "Загрузка отменена!";
                    btnCancel.Content = @"Закрыть";
                    btnCancel.Tag = 2;
                }
            }
            catch (Exception ex) {
                _log.ErrorFormat(ex.Message);
            }
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e) {
            try {
                switch (int.Parse(btnCancel.Tag.ToString())) {
                    case 0:
                        _webClient.CancelAsync();
                        break;
                    case 1:
                        Process.Start(_fileName);
                        Close();
                        break;
                    case 2:
                        Close();
                        break;
                }
            }
            catch (Exception ex) {
                _log.ErrorFormat(ex.Message);
            }
        }
    }
}
