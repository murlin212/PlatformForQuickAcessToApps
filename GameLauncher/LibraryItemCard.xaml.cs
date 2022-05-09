using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GameLauncher
{
    /// <summary>
    /// Interaction logic for LibraryItemCard.xaml
    /// </summary>
    public partial class LibraryItemCard : UserControl
    {
        public string ExecPlayPath { get; set; }
        public string ImgPath { get; set; }
        public int Id { get; }
        private readonly SQLiteConnection dbconnection;

        public event RoutedEventHandler DelCard;

        public LibraryItemCard(int id, string textTitle, SQLiteConnection sQLConnection)
        {
            InitializeComponent();
            Id = id; 
            GameTitle.Text = textTitle;
            dbconnection = sQLConnection;
            ImgPath = $@"{Directory.GetParent(Environment.CurrentDirectory).Parent.FullName}\Assets\DefaultCardImage.jpg";
            SetImage();
        }

        public LibraryItemCard(int id, string textTitle, string exePath, string imgPath, SQLiteConnection sQLConnection)
        {
            InitializeComponent();
            Id = id;
            GameTitle.Text = textTitle;
            ExecPlayPath = exePath;
            ImgPath = imgPath;
            dbconnection = sQLConnection;
            SetImage();
        }

        private void SetImage()
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(ImgPath);
            bitmap.EndInit();
            ImageViewer.Source = bitmap;
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.InitialDirectory = "c:\\";
            dlg.Filter = "Image files (*.jpg;*.png;*.jpeg;*.bmp)|*.jpg;*.png;*.jpeg;*.bmp|All Files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == true)
            {               
                ImgPath = dlg.FileName;
                SetImage();
                ImgDBUpdate();
            }
        }

        private void ImgDBUpdate()
        {
            dbconnection.Open();
            SQLiteCommand ins_cmd = dbconnection.CreateCommand();

            ins_cmd.CommandText = $"UPDATE CardData SET imgPath = '{ImgPath}' WHERE id = {Id};";
            ins_cmd.ExecuteNonQuery();

            dbconnection.Close();
        }

        private void Play_App_ButtonClicked(object sender, RoutedEventArgs e)
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(ExecPlayPath);
            p.Start();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DelCard != null)
            {
                DelCard(this, e);
            }

        }
    }
}
