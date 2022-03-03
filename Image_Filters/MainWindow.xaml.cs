using System;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Image_Filters
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Drawing.Image? imageDrawing;
        private List<ImageFilter> selectedFilters;
        public MainWindow()
        {
            InitializeComponent();

            //initialize a list of built-in filters 
            var filters = new List<ImageFilter>();

            filters.Add(new Invert("Invert"));
            filters.Add(new BrightnessCorrection("Invert"));


            filterListView.ItemsSource = filters;

            selectedFilters = new List<ImageFilter>();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.DefaultExt = ".png";
            openFileDialog.Filter = "JPEG Files (*.jpeg)|*.jpeg,*.png|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif";

            if (openFileDialog.ShowDialog() == true)
            {
                imgorig.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                imgmod.Source = new BitmapImage(new Uri(openFileDialog.FileName));
            }
            ConvertToDrawing(imgorig);
            //txtEditor.Text = File.ReadAllText(openFileDialog.FileName);
        }
        private void ConvertToDrawing(System.Windows.Controls.Image img)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            System.Windows.Media.Imaging.BmpBitmapEncoder bbe = new BmpBitmapEncoder();
            bbe.Frames.Add(BitmapFrame.Create(new Uri(img.Source.ToString(), UriKind.RelativeOrAbsolute)));

            bbe.Save(ms);
            imageDrawing = System.Drawing.Image.FromStream(ms);


        }

        private BitmapImage ToWpfImage(System.Drawing.Image img)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);

            BitmapImage ix = new BitmapImage();
            ix.BeginInit();
            ix.CacheOption = BitmapCacheOption.OnLoad;
            ix.StreamSource = ms;
            ix.EndInit();
            return ix;
        }
        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            var item = sender as ListViewItem;

            if (item != null && (item.Content as ImageFilter) != null)//&& item.IsSelected)
            {
                //MessageBox for Debugging
               // MessageBoxResult result = MessageBox.Show("Hello MessageBox");

                selectedFilters.Add(item.Content as ImageFilter);
                selectedListView.Items.Add(item.Content as ImageFilter);// = selectedFilters;
                foreach (var filter in selectedListView.Items)
                {
                    imageDrawing = (filter as ImageFilter).applyFilter(imageDrawing);
                }
                    imgmod.Source = ToWpfImage(imageDrawing);
                ConvertToDrawing(imgorig);
                //new BitmapImage(new Uri("C:/Users/Patryk/Pictures/uyhj.png"));
                //ConvertToDrawing(imgmod);

            }
        }
        private void SelectedListViewItem_PreviewMouseLeftButtonDown(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                selectedListView.Items.RemoveAt(selectedListView.Items.IndexOf(e.AddedItems[0]));


            }
            if (selectedListView.Items.Count == 0)
            {
                imgmod.Source = imgorig.Source;
                ConvertToDrawing(imgorig);
            }
            else
            {
                foreach (var filter in selectedListView.Items)
                {
                    imageDrawing = (filter as ImageFilter).applyFilter(imageDrawing);

                }
                imgmod.Source = ToWpfImage(imageDrawing);
                ConvertToDrawing(imgorig);
            }
        }
    }
}
