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
using System.Windows.Shapes;

namespace Image_Filters
{
    /// <summary>
    /// Interaction logic for FilterOptionsWindow.xaml
    /// </summary>
    public partial class FilterOptionsWindow : Window
    {
        public ImageFilter chosenErrorDiffusion;
        public string chosenErrorDiffusionFilter;
        public double rErrorDiffusion;
        public double gErrorDiffusion;
        public double bErrorDiffusion;

        public double rRegions;
        public double gRegions;
        public double bRegions;
        public FilterOptionsWindow()
        {
            chosenErrorDiffusionFilter = "FloydSteinberg";
            rErrorDiffusion = 0.3;
            gErrorDiffusion = 0.59;
            bErrorDiffusion = 0.11;
            InitializeComponent();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            chosenErrorDiffusionFilter = (string)(e.AddedItems[0] as ComboBoxItem).Content;
        }
        private void FilterOptionsWindow_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(RTextBox!=null)
            rErrorDiffusion = Double.Parse(RTextBox.Text);
            if (GTextBox != null)
                gErrorDiffusion = Double.Parse(GTextBox.Text);
            if (BTextBox != null)
                bErrorDiffusion = Double.Parse(BTextBox.Text);
            if (RRegionsTextBox != null)
                rRegions = Double.Parse(RRegionsTextBox.Text);
            if (GRegionsTextBox != null)
                gRegions = Double.Parse(GRegionsTextBox.Text);
            if (BRegionsTextBox != null)
                bRegions = Double.Parse(BRegionsTextBox.Text);
        }
    }
}
