using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using YoutubeExplode;

namespace Dot.exe_s_ClipConvert
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            // CheckChanged events for CheckBoxes
            chkM4A.CheckedChanged += new EventHandler(CheckBox_CheckedChanged);
            chkOpus.CheckedChanged += new EventHandler(CheckBox_CheckedChanged);
            chkFlac.CheckedChanged += new EventHandler(CheckBox_CheckedChanged);
            chkMp3.CheckedChanged += new EventHandler(CheckBox_CheckedChanged);
        }

        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // Ensure only one CheckBox is selected
            if (sender is CheckBox selectedCheckBox && selectedCheckBox.Checked)
            {
                foreach (var control in this.Controls)
                {
                    if (control is CheckBox checkBox && checkBox != selectedCheckBox)
                    {
                        checkBox.Checked = false;
                    }
                }
            }
        }

        private async void btnDownload_Click(object sender, EventArgs e)
        {
            string videoUrl = txtUrl.Text;

            if (string.IsNullOrWhiteSpace(videoUrl))
            {
                MessageBox.Show("Please enter a valid YouTube URL.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            lblStatus.Text = "Downloading...";
            btnDownload.Enabled = false;

            try
            {
                var youtube = new YoutubeClient();
                var video = await youtube.Videos.GetAsync(videoUrl);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

                var streamInfo = streamManifest
                    .GetAudioOnlyStreams()
                    .OrderByDescending(s => s.Bitrate)
                    .FirstOrDefault();

                if (streamInfo != null)
                {
                    string musicFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                    string cleanTitle = CleanFileName(video.Title); // Clean file name
                    string outputFilePath = Path.Combine(musicFolderPath, $"{cleanTitle}.{streamInfo.Container.Name}");
                    outputFilePath = GetUniqueFileName(outputFilePath); // Ensure unique file name

                    await youtube.Videos.Streams.DownloadAsync(streamInfo, outputFilePath);

                    // Convert to selected format
                    if (chkM4A.Checked)
                    {
                        ConvertToFormat(outputFilePath, "m4a");
                    }
                    if (chkOpus.Checked)
                    {
                        ConvertToFormat(outputFilePath, "opus");
                    }
                    if (chkFlac.Checked)
                    {
                        ConvertToFormat(outputFilePath, "flac");
                    }
                    if (chkMp3.Checked)
                    {
                        ConvertToFormat(outputFilePath, "mp3");
                    }

                    lblStatus.Text = "Conversion completed.";
                }
                else
                {
                    lblStatus.Text = "No audio stream found.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Download failed.";
            }
            finally
            {
                btnDownload.Enabled = true;
            }
        }

        private void ConvertToFormat(string inputFile, string format)
        {
            string outputFilePath = Path.ChangeExtension(inputFile, format);
            outputFilePath = GetUniqueFileName(outputFilePath); // Ensure unique file name
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = $"-i \"{inputFile}\" \"{outputFilePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                File.Delete(inputFile);
            }
        }

        private string GetUniqueFileName(string filePath)
        {
            int count = 1;
            string fileNameOnly = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            string directory = Path.GetDirectoryName(filePath);
            string newFilePath = filePath;

            while (File.Exists(newFilePath))
            {
                string tempFileName = $"{fileNameOnly}({count++}){extension}";
                newFilePath = Path.Combine(directory, tempFileName);
            }

            return newFilePath;
        }

        private string CleanFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_'); // Replace invalid characters with '_'
            }
            return fileName;
        }
    }
}
