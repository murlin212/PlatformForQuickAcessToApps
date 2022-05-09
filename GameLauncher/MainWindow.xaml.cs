using Microsoft.Win32;
using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;

namespace GameLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const int COL_WIDTH = 300;
        private const int ROW_HEIGHT = 430;
        private int col;
        private int row;
        private SQLiteConnection sqlite_conn;
        private int current_id;
        private List <LibraryItemCard> cards;
        private double workAreaHight;
        private double workAreaWidth;
        private int maxColPerRow;

        private int _height;
        public int CustomHeight
        {
            get { return _height; }
            set
            {
                if (value != _height)
                {
                    _height = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("CustomHeight"));
                }
            }
        }

        private int _width;
        public int CustomWidth
        {
            get { return _width; }
            set
            {
                if (value != _width)
                {
                    _width = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("CustomWidth"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            dropdown.Width = 0;
            settings.Width = 0;
            aboutinfo.Width = 0;
            maxColPerRow = (int)(SystemParameters.PrimaryScreenWidth - NavbarPanel.Width) / COL_WIDTH;
            this.col = maxColPerRow;
            this.row = 0;

            CustomHeight = (int)SystemParameters.PrimaryScreenHeight;
            CustomWidth = (int)SystemParameters.PrimaryScreenWidth;

            workAreaHight = SystemParameters.WorkArea.Height;
            workAreaWidth = SystemParameters.WorkArea.Width;

            for (int i = 0; i < maxColPerRow; i++)
            {
                CardGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(COL_WIDTH) });
            }

            sqlite_conn = new SQLiteConnection("Data Source = database.db; Version = 3; New = True; Compress = True; ");
            cards = new List<LibraryItemCard>();

            CardStatsInit();
        }

        private void CardStatsInit() { 
            
            sqlite_conn.Open();

            SQLiteDataReader dataReader;
            SQLiteCommand sel_cmd = sqlite_conn.CreateCommand();
            sel_cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'CardData';";
            dataReader = sel_cmd.ExecuteReader();

            if (dataReader.Read())
            {
                dataReader.Close();
                sel_cmd.CommandText = "SELECT * FROM CardData;";
                dataReader = sel_cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    cards.Add(new LibraryItemCard(
                        dataReader.GetInt32(0), 
                        dataReader.GetString(1), 
                        dataReader.GetString(2), 
                        dataReader.GetString(3), 
                        sqlite_conn));

                    cards.Last().DelCard += CardDeleteHandler;

                    UpdateGrid(cards.Last());
                }

                if (cards.Count > 0)
                {
                    current_id = cards.Last().Id;
                }               
            }
            else
            {
                SQLiteCommand sqlite_cmd;
                string createQuery = "CREATE TABLE CardData(id INT, gameName VARCHAR(32), exePath VARCHAR(80), imgPath VARCHAR(80))";

                sqlite_cmd = sqlite_conn.CreateCommand();
                sqlite_cmd.CommandText = createQuery;
                sqlite_cmd.ExecuteNonQuery();
            }

            sqlite_conn.Close();
        }

        private void HomeScreen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
            System.Windows.Application.Current.Shutdown();
        }

        private void LibraryToggle_Checked(object sender, RoutedEventArgs e)
        {
            DoubleAnimation widthAnimation = new DoubleAnimation(CustomWidth - 230, new Duration(TimeSpan.FromSeconds(0.2)));
            dropdown.BeginAnimation(WidthProperty, widthAnimation);
            widthAnimation = new DoubleAnimation(0, new Duration(TimeSpan.FromSeconds(0.2)));

            settings.BeginAnimation(WidthProperty, widthAnimation);
            SettingsToggle.IsChecked = false;

            aboutinfo.BeginAnimation(WidthProperty, widthAnimation);
            AboutInfoToggle.IsChecked = false;
        }

        private void LibraryToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            DoubleAnimation widthAnimation = new DoubleAnimation(0, new Duration(TimeSpan.FromSeconds(0.2)));
            dropdown.BeginAnimation(WidthProperty, widthAnimation);
        }

        private void ResButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
           
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Minimized;
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            HomeButton.Background = new SolidColorBrush(Colors.Transparent);
        }

        private void CardDBInsert(LibraryItemCard card)
        {
            sqlite_conn.Open();
            SQLiteCommand ins_cmd = sqlite_conn.CreateCommand();


            ins_cmd.CommandText = $"INSERT INTO CardData VALUES (@id, @gameTItle, @execPlayPath, @imgPath);";
            ins_cmd.Parameters.AddWithValue("@id", card.Id);
            ins_cmd.Parameters.AddWithValue("@gameTitle", card.GameTitle.Text);
            ins_cmd.Parameters.AddWithValue("@execPlayPath", card.ExecPlayPath);
            ins_cmd.Parameters.AddWithValue("@imgPath", card.ImgPath);
            ins_cmd.Prepare();

            ins_cmd.ExecuteNonQuery();

            sqlite_conn.Close();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = ".exe"; // Required file extension
            fileDialog.Filter = "exe files (*.exe;*.url;)|*.exe;*.url;|All files (*.*)|*.*"; // Optional file extensions
            bool result = (bool)fileDialog.ShowDialog();
            string dialogFilePath = fileDialog.FileName;

            //Assembly assembly = Assembly.LoadFrom(fileDialog);
            //string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (result)
            {                                             
                LibraryItemCard itemcardControl = new LibraryItemCard(++current_id,System.IO.Path.GetFileNameWithoutExtension(dialogFilePath), sqlite_conn);
                itemcardControl.ExecPlayPath = dialogFilePath;
                itemcardControl.DelCard += CardDeleteHandler;
                CardDBInsert(itemcardControl);
                UpdateGrid(itemcardControl);
                cards.Add(itemcardControl);
            }        
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchtxt = sender as TextBox;
            string query = searchtxt.Text.ToLower();
            if(query == "")
            {
                ResetGrid();
                cards.ForEach(el => UpdateGrid(el));
            }
            else
            {
                ResetGrid();
                cards.Where(el => System.IO.Path.GetFileNameWithoutExtension(el.ExecPlayPath).ToLower().Contains(query))
                    .ToList()
                    .ForEach(el => UpdateGrid(el));
            }
        }

        private void ResetGrid()
        {
            CardGrid.Children.Clear();
            col = maxColPerRow;
            row = 0;
        }

        private void UpdateGrid(LibraryItemCard card)
        {
            if (col >= maxColPerRow)
            {
                CardGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(ROW_HEIGHT) });
                row++;
                col = 0;
            }

            CardGrid.Children.Add(card);
            Grid.SetColumn(card, col++);
            Grid.SetRow(card, row);            
        }

        private void CardDeleteHandler(object sender, RoutedEventArgs e)
        {
            ResetGrid();
            cards.Remove(sender as LibraryItemCard);
            cards.ForEach(el => UpdateGrid(el));

            DeleteDataBase(sender as LibraryItemCard);
        }

        private void DeleteDataBase(LibraryItemCard card)
        {
            sqlite_conn.Open();
            SQLiteCommand ins_cmd = sqlite_conn.CreateCommand();

            ins_cmd.CommandText = $"DELETE FROM CardData WHERE id=@id;";
            ins_cmd.Parameters.AddWithValue("@id", card.Id);
            ins_cmd.Prepare();

            ins_cmd.ExecuteNonQuery();

            sqlite_conn.Close();
        }

        private void SettingsToggle_Checked(object sender, RoutedEventArgs e)
        {
            DoubleAnimation widthAnimation = new DoubleAnimation(CustomWidth - 230, new Duration(TimeSpan.FromSeconds(0.2)));
            settings.BeginAnimation(WidthProperty, widthAnimation);

            widthAnimation = new DoubleAnimation(0, new Duration(TimeSpan.FromSeconds(0.2)));
            dropdown.BeginAnimation(WidthProperty, widthAnimation);
            LibraryToggle.IsChecked = false;

            aboutinfo.BeginAnimation(WidthProperty, widthAnimation);
            AboutInfoToggle.IsChecked = false;
        }
       
        private void SettingsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            DoubleAnimation widthAnimation = new DoubleAnimation(0, new Duration(TimeSpan.FromSeconds(0.2)));
            settings.BeginAnimation(WidthProperty, widthAnimation);
        }

        private void AboutInfoToggle_Checked(object sender, RoutedEventArgs e)
        {
            DoubleAnimation widthAnimation = new DoubleAnimation(CustomWidth - 230, new Duration(TimeSpan.FromSeconds(0.2)));
            aboutinfo.BeginAnimation(WidthProperty, widthAnimation);

            widthAnimation = new DoubleAnimation(0, new Duration(TimeSpan.FromSeconds(0.2)));
            dropdown.BeginAnimation(WidthProperty, widthAnimation);         
            LibraryToggle.IsChecked = false;

            settings.BeginAnimation(WidthProperty, widthAnimation);
            SettingsToggle.IsChecked = false;
        }

        private void AboutInfoToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            DoubleAnimation widthAnimation = new DoubleAnimation(0, new Duration(TimeSpan.FromSeconds(0.2)));
            aboutinfo.BeginAnimation(WidthProperty, widthAnimation);
        }

        

        private void HomeGif_MediaEnded(object sender, RoutedEventArgs e)
        {
            HomeGif.Position = TimeSpan.FromMilliseconds(1);
            HomeGif.Play();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            CustomHeight = (int)SystemParameters.PrimaryScreenHeight;
            CustomWidth = (int)SystemParameters.PrimaryScreenWidth;

            if (CustomWidth < 1920 || CustomHeight < 1080)
            {
                MessageBox.Show("Display doesn't support these window parameters!", "Resolution error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                CustomHeight = 1080;
                CustomWidth = 1920;
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            CustomHeight = (int)SystemParameters.PrimaryScreenHeight;
            CustomWidth = (int)SystemParameters.PrimaryScreenWidth;

            if (CustomWidth < 1600 || CustomHeight < 900)
            {
                MessageBox.Show("Display doesn't support these window parameters!", "Resolution error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                CustomWidth = 1600;
                CustomHeight = 900;
            }
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            CustomHeight = (int)SystemParameters.PrimaryScreenHeight;
            CustomWidth = (int)SystemParameters.PrimaryScreenWidth;

            if (CustomWidth < 1366 || CustomHeight < 768)
            {
                MessageBox.Show("Display doesn't support these window parameters!", "Resolution error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                CustomWidth = 1366;
                CustomHeight = 768;
            }
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            CustomHeight = (int)SystemParameters.PrimaryScreenHeight;
            CustomWidth = (int)SystemParameters.PrimaryScreenWidth;

            if (CustomWidth < 1024 || CustomHeight < 768)
            {
                MessageBox.Show("Display doesn't support these window parameters!", "Resolution error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                CustomWidth = 1024;
                CustomHeight = 768;
            }
        }
    }
}
