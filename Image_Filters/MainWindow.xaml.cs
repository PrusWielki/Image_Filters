using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Image_Filters
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Drawing.Image? imageDrawing;
        public MainWindow()
        {
            InitializeComponent();

            //initialize a list of built-in filters 
            var filters = new List<ImageFilter>();
            filters.Add(new Invert("Invert"));
            filters.Add(new BrightnessCorrection("Brightness Correction"));
            filters.Add(new ContrastEnchancement("Contrast Enchancement"));
            filters.Add(new GammaCorrection("Gamma Correction"));
            filters.Add(new Blur3x3Filter("3x3 Blur"));
            filters.Add(new Gaussian3x3BlurFilter("Gaussian 3x3 Blur"));
            filters.Add(new Sharpen3x3Filter("3x3 Sharpen"));
            filters.Add(new EdgeDetectionFilter("Edge detection"));
            filters.Add(new EmbossFilter("Emboss filter"));
            //add initialize flters to filter list
            filterListView.ItemsSource = filters;

        }
        //Opens choose file dialog
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.DefaultExt = ".png";
            openFileDialog.Filter = "PNG Files (*.png)|*.png|EG Files (*.jpeg)|*.jpeg|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif";

            //assign chosen picture to both original and modified image
            if (openFileDialog.ShowDialog() == true)
            {
                imgorig.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                imgmod.Source = new BitmapImage(new Uri(openFileDialog.FileName));
            }
            //converts Controls.Image to Drawing.Image, necessary to be able to apply filters
            ConvertToDrawing(imgorig);
        }
        //converts Controls.Image to Drawing.Image, necessary to be able to apply filters, result is stored in property imageDrawing
        private void ConvertToDrawing(System.Windows.Controls.Image img)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            System.Windows.Media.Imaging.BmpBitmapEncoder bbe = new BmpBitmapEncoder();
            bbe.Frames.Add(BitmapFrame.Create(new Uri(img.Source.ToString(), UriKind.RelativeOrAbsolute)));

            bbe.Save(ms);
            imageDrawing = System.Drawing.Image.FromStream(ms);
        }
        //converts image back to a form that can be assgined to Control.Image
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
        //Event triggerred when a filter from filters list has been clicked, the filter is applied to the image
        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            var item = sender as ListViewItem;

            if (item != null && (item.Content as ImageFilter) != null)
            {
                //MessageBox for Debugging
                // MessageBoxResult result = MessageBox.Show("Hello MessageBox");

                //add filter to selected filters and apply all selected filters to the original image resulting in a modified image
                selectedListView.Items.Add(item.Content as ImageFilter);
                ApplyFiliters();
            }
            else
                throw new NullReferenceException();
        }
        //Event handling removal of selected filters
        private void SelectedListViewItem_PreviewMouseLeftButtonDown(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)//remove selected filter from the list
                selectedListView.Items.RemoveAt(selectedListView.Items.IndexOf(e.AddedItems[0]));
            if (0 == selectedListView.Items.Count)//if the count of filters is equal to 0, then reset the image to the original
            {
                imgmod.Source = imgorig.Source;
                ConvertToDrawing(imgorig);
            }
            else//if there are some filters selected, then reaaply them one by one to the original image, resulting in modified image
            {
                ApplyFiliters();
            }
        }
        private void ApplyFiliters()
        {
            foreach (var filter in selectedListView.Items)
            {
                if (null != imageDrawing)
                    imageDrawing = ((ImageFilter)filter).applyFilter(imageDrawing);
                else
                    throw new NullReferenceException();

            }
            if (null != imageDrawing)
                imgmod.Source = ToWpfImage(imageDrawing);
            else
                throw new NullReferenceException();
            //convert original image back to Drawing.Image so that, the next time filters are applied, they are applied one by one to the original image
            ConvertToDrawing(imgorig);
        }
    }
}
