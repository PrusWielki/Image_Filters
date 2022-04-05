using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
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
        private List<ImageFilter> removedFilters;
        private ObservableCollection<ImageFilter> filters;
        private NewFilterWindow newFilterWindow;
        private bool isGrayScale;
        private FilterOptionsWindow filterOptionsWindow;
        public MainWindow()
        {
            InitializeComponent();
            removedFilters = new List<ImageFilter>();
            isGrayScale = false;

            //initialize a list of built-in filters 
            filters = new ObservableCollection<ImageFilter>();
            filters.Add(new Invert("Invert"));
            filters.Add(new BrightnessCorrection("Brightness Correction"));
            filters.Add(new ContrastEnchancement("Contrast Enchancement"));
            filters.Add(new GammaCorrection("Gamma Correction"));
            filters.Add(new Blur3x3Filter("3x3 Blur"));
            filters.Add(new Gaussian3x3BlurFilter("Gaussian 3x3 Blur"));
            filters.Add(new Sharpen3x3Filter("3x3 Sharpen"));
            filters.Add(new EdgeDetectionFilter("Edge detection"));
            filters.Add(new EmbossFilter("Emboss filter"));
            filters.Add(new YCbCrFilter("YCbCrDithering", 0.3, 0.59, 0.11));
            filters.Add(new FloydAndSteinbergFilter("ErrorDiffusion",0.3,0.59,0.11));
            //filters.Add(new BurkesFilter("BurkesErrorDiffusion"));
            //filters.Add(new StuckyFilter("StuckyErrorDiffusion"));
            //filters.Add(new SierraFilter("SierraErrorDiffusion"));
            //filters.Add(new AtkinsonFilter("AtkinsonErrorDiffusion"));
            
            filters.Add(new UniformQuantization("UniformQuantization", 16, 16, 16));



            //add initialized flters to filter list
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
            if (null != img.Source)
                bbe.Frames.Add(BitmapFrame.Create(new Uri(img.Source.ToString(), UriKind.RelativeOrAbsolute)));

            else
                return;
            

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
                if (null != imgorig.Source)
                {
                    //clear the removedFilters list (task requirement)
                    removedFilters.Clear();
                    selectedListView.Items.Add(item.Content as ImageFilter);
                    ApplyFilters();
                }
            }
            else
                throw new NullReferenceException();
        }
        //Event handling removal of selected filters
        private void SelectedListViewItem_PreviewMouseLeftButtonDown(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)//remove selected filter from the list and add it to removedFilters (for the redo button)
            {
                removedFilters.Add((ImageFilter)e.AddedItems[0]);
                selectedListView.Items.RemoveAt(selectedListView.Items.IndexOf(e.AddedItems[0]));
            }
            if (0 == selectedListView.Items.Count)//if the count of filters is equal to 0, then reset the image to the original
            {
                imgmod.Source = imgorig.Source;
                ConvertToDrawing(imgorig);
            }
            else//if there are some filters selected, then reaaply them one by one to the original image, resulting in modified image
            {
                ApplyFilters();
            }
        }
        private void ApplyFilters()
        {
            if (isGrayScale)
                Grayscale_Click(null,null);
            foreach (var filter in selectedListView.Items)
            {
                if (null != imageDrawing)
                    imageDrawing = ((ImageFilter)filter).applyFilter(imageDrawing,isGrayScale);
                else
                    return;// throw new NullReferenceException();

            }
            if (null != imageDrawing)
                imgmod.Source = ToWpfImage(imageDrawing);
            else
                throw new NullReferenceException();
            //convert original image back to Drawing.Image so that, the next time filters are applied, they are applied one by one to the original image
            ConvertToDrawing(imgorig);
        }

        //opens new window with possibility to create a custom convolution filter
        private void NewFilter_Click(object sender, RoutedEventArgs e)
        {
            newFilterWindow = new NewFilterWindow();
            
            newFilterWindow.Closing += NewFilterWindow_Closing;
            newFilterWindow.ShowDialog();
        }

        //add the new filter on window closure
        private void NewFilterWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            newFilterWindow.SaveDataToMatrix();
            filters.Add(new ConvolutionFilterBase(newFilterWindow.FilterNameTextBox.Text, newFilterWindow.factor, newFilterWindow.bias, newFilterWindow.filterMatrix));
        }

        //opens save file dialog and saves the modified picture to jpeg format with a default name editedimage
        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "editedimage";
            dlg.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif";
            if (dlg.ShowDialog() == true)
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imgmod.Source));
                using (var stream = dlg.OpenFile())
                {
                    encoder.Save(stream);
                }
            }

            

        }
        //after the redo button is pressed a set of previously removed filters is reapplied
        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            foreach(var filter in removedFilters)//add all the removed filters to selectedfilters
            {
                selectedListView.Items.Add(filter);
            }
            ApplyFilters();//reapply the filters
            removedFilters.Clear();
        }
        private void Grayscale_Click(object sender, RoutedEventArgs e)
        {
            Bitmap c = (Bitmap)imageDrawing.Clone();
            Bitmap d = new Bitmap(c.Width, c.Height);
            isGrayScale = true;
            for (int i = 0; i < c.Width; i++)
            {
                for (int x = 0; x < c.Height; x++)
                {
                    Color oc = c.GetPixel(i, x);
                    int grayScale = (int)((oc.R * 0.3) + (oc.G * 0.59) + (oc.B * 0.11));
                    Color nc = Color.FromArgb(oc.A, grayScale, grayScale, grayScale);
                    d.SetPixel(i, x, nc);
                }
            }
            imageDrawing = d;
            imgmod.Source = ToWpfImage(imageDrawing);
        }
        
        private void FilterOptions_Click(object sender, RoutedEventArgs e)
        {
            int index = 0;

            foreach (var filter in filters)
            {
                if (filter.getName() == "ErrorDiffusion")
                    index = filters.IndexOf(filter);

            }
            filters.RemoveAt(index);
            foreach (var filter in filters)
            {
                
                if (filter.getName() == "UniformQuantization")
                    index = filters.IndexOf(filter);
            }
            filters.RemoveAt(index);
            filterOptionsWindow = new FilterOptionsWindow();

            filterOptionsWindow.Closing += FilterOptionsWindow_Closing;
            filterOptionsWindow.ShowDialog();
        }

        //add the new filter on window closure
        private void FilterOptionsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (filterOptionsWindow.chosenErrorDiffusionFilter == "Burkes")
                filters.Add( new BurkesFilter("ErrorDiffusion",filterOptionsWindow.rErrorDiffusion, filterOptionsWindow.gErrorDiffusion, filterOptionsWindow.bErrorDiffusion));
            else if (filterOptionsWindow.chosenErrorDiffusionFilter == "FloydSteinberg")
                filters.Add(new FloydAndSteinbergFilter("ErrorDiffusion", filterOptionsWindow.rErrorDiffusion, filterOptionsWindow.gErrorDiffusion, filterOptionsWindow.bErrorDiffusion));
            else if (filterOptionsWindow.chosenErrorDiffusionFilter == "Stucky")
                filters.Add(new StuckyFilter("ErrorDiffusion", filterOptionsWindow.rErrorDiffusion, filterOptionsWindow.gErrorDiffusion, filterOptionsWindow.bErrorDiffusion));
            else if (filterOptionsWindow.chosenErrorDiffusionFilter == "Sierra")
                filters.Add(new SierraFilter("ErrorDiffusion", filterOptionsWindow.rErrorDiffusion, filterOptionsWindow.gErrorDiffusion, filterOptionsWindow.bErrorDiffusion));
            else if (filterOptionsWindow.chosenErrorDiffusionFilter == "Atkinson")
                filters.Add(new AtkinsonFilter("ErrorDiffusion", filterOptionsWindow.rErrorDiffusion, filterOptionsWindow.gErrorDiffusion, filterOptionsWindow.bErrorDiffusion));
            filters.Add(new UniformQuantization("UniformQuantization", filterOptionsWindow.rRegions, filterOptionsWindow.gRegions, filterOptionsWindow.bRegions));

        }









        private void YCbCr_Click(object sender, RoutedEventArgs e)
        {
            Bitmap sourceBitmap = (Bitmap)imageDrawing.Clone();
           
            
            BitmapData sourceData = sourceBitmap.LockBits(new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            //initialize buffer with a size of a picture's height times it's stride (the width of a single row of pixels)
            byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];
            byte[] resultBuffer = new byte[sourceData.Stride * sourceData.Height];


            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);


            sourceBitmap.UnlockBits(sourceData);


            double blue = 0.0;
            double green = 0.0;
            double red = 0.0;

      
            int byteOffset = 0;

            //start off not from the very beggining of an image but with an offset (in the case of 3x3 filters, instead of starting from point (0,0) - start from (1,1)), the borders of an image are ignored
            for (int offsetY = 0; offsetY < sourceBitmap.Height ; offsetY++)
            {
                for (int offsetX = 0; offsetX < sourceBitmap.Width ; offsetX++)
                {
                    blue = 0;
                    green = 0;
                    red = 0;

                    //get to the correct set of aRGB values in the correct row, kind of an index
                    byteOffset = offsetY * sourceData.Stride + offsetX * 4;



                    //convert to YCbCr
                    //resultBuffer[byteOffset+2] = (byte)(pixelBuffer[byteOffset + 2]*0.299+ pixelBuffer[byteOffset + 1]*0.587+ pixelBuffer[byteOffset]*0.114);
                    //resultBuffer[byteOffset + 1] = (byte)(128 -0.168736* pixelBuffer[byteOffset + 2]-0.331264* pixelBuffer[byteOffset + 1]+0.5* pixelBuffer[byteOffset]);
                    //resultBuffer[byteOffset] = (byte)(128+0.5* pixelBuffer[byteOffset + 2]-0.418688* pixelBuffer[byteOffset + 1]-0.81312* pixelBuffer[byteOffset]);
                    //resultBuffer[byteOffset + 3] = 255;


                    resultBuffer[byteOffset + 2] =  (byte)(pixelBuffer[byteOffset + 2] * 0.299 + pixelBuffer[byteOffset + 1] * 0.587 + pixelBuffer[byteOffset] * 0.114);
                    resultBuffer[byteOffset + 1] = (byte)(128 - 0.168736 * pixelBuffer[byteOffset + 2] - 0.331264 * pixelBuffer[byteOffset + 1] + 0.5 * pixelBuffer[byteOffset]);
                    resultBuffer[byteOffset] = (byte)(128 + 0.5 * pixelBuffer[byteOffset + 2] - 0.418688 * pixelBuffer[byteOffset + 1] - 0.81312 * pixelBuffer[byteOffset]);
                    resultBuffer[byteOffset + 3] = 255;

                }
            }

            //save results to new bitmap
            Bitmap resultBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);


            BitmapData resultData = resultBitmap.LockBits(new Rectangle(0, 0,
                                    resultBitmap.Width, resultBitmap.Height),
                                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

           


            Marshal.Copy(resultBuffer, 0, resultData.Scan0, resultBuffer.Length);
            resultBitmap.UnlockBits(resultData);


            
            imageDrawing = resultBitmap;
            imgmod.Source = ToWpfImage(imageDrawing);
        }
        
    }
}
