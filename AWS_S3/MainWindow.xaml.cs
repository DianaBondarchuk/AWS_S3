using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace AWS_S3
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private IAmazonS3 GetClient()
        {
            string accessKey = TxtAccessKey.Text.Trim();
            string secretKey = TxtSecretKey.Password.Trim();

            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            {
                throw new Exception("Please enter Access Key and Secret Key!");
            }

            return new AmazonS3Client(accessKey, secretKey, RegionEndpoint.EUNorth1);
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            string bucket = TxtBucket.Text.Trim();
            if (string.IsNullOrEmpty(bucket))
            {
                MessageBox.Show("Please enter bucket name!");
                return;
            }

            try
            {
                TxtStatus.Text = "Loading files...";

                using (var client = GetClient())
                {
                    var request = new ListObjectsV2Request
                    {
                        BucketName = bucket
                    };

                    var response = await client.ListObjectsV2Async(request);

                    if (response.S3Objects != null && response.S3Objects.Count > 0)
                    {
                        GridFiles.ItemsSource = response.S3Objects.Select(x => new
                        {
                            Key = x.Key,
                            Size = x.Size,
                            LastModified = x.LastModified?.ToLocalTime()
                        }).ToList();

                        TxtStatus.Text = $"Found files: {response.S3Objects.Count}";
                    }
                    else
                    {
                        GridFiles.ItemsSource = null;
                        TxtStatus.Text = "Bucket is empty";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                TxtStatus.Text = "Failed to load files";
            }
        }

        private async void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            string bucket = TxtBucket.Text.Trim();
            if (string.IsNullOrEmpty(bucket))
            {
                MessageBox.Show("Please enter bucket name first!");
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                string fileName = Path.GetFileName(path);

                try
                {
                    TxtStatus.Text = "Uploading file...";

                    using (var client = GetClient())
                    {
                        var request = new PutObjectRequest
                        {
                            BucketName = bucket,
                            Key = fileName,
                            FilePath = path
                        };

                        await client.PutObjectAsync(request);
                    }

                    MessageBox.Show("File uploaded successfully!");
                    BtnRefresh_Click(sender, e);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Upload error: " + ex.Message);
                    TxtStatus.Text = "Upload failed";
                }
            }
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            string bucket = TxtBucket.Text.Trim();
            dynamic selected = GridFiles.SelectedItem;

            if (selected == null)
            {
                MessageBox.Show("Please select a file from the table!");
                return;
            }

            string fileKey = selected.Key;

            SaveFileDialog dialog = new SaveFileDialog
            {
                FileName = fileKey
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    TxtStatus.Text = "Downloading file...";

                    using (var client = GetClient())
                    {
                        var request = new GetObjectRequest
                        {
                            BucketName = bucket,
                            Key = fileKey
                        };

                        using (var response = await client.GetObjectAsync(request))
                        {
                            await response.WriteResponseStreamToFileAsync(dialog.FileName, false, System.Threading.CancellationToken.None);
                        }
                    }

                    MessageBox.Show("File downloaded successfully!");
                    TxtStatus.Text = "Download completed";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Download error: " + ex.Message);
                    TxtStatus.Text = "Download failed";
                }
            }
        }
    }
}