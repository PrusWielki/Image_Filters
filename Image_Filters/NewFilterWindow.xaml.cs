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
    /// Interaction logic for NewFilterWindow.xaml
    /// </summary>
    public partial class NewFilterWindow : Window
    {
        public double factor;
        public double bias;
        public double[,]? filterMatrix;

        public NewFilterWindow()
        {
           
            InitializeComponent();
        }
        private void CreateMatrix_Click(object sender, RoutedEventArgs e)
        {
            bias=Int32.Parse(BiasTextBox.Text);
            factor = Int32.Parse(FactorTextBox.Text);
            filterMatrix = new double[Int32.Parse(ColumnTextBox.Text), Int32.Parse(RowTextBox.Text)];
            ShowMatrix();
            
        }
        private void ShowMatrix()
        {
            for(int i=0;i<filterMatrix.GetLength(0); i++)
            CreateATextBox();


        }
        private void CreateATextBox()

        {

            TextBox txtb = new TextBox();

            txtb.Height = 50;

            txtb.Width = 200;

            txtb.Text = "Text Box content";

            txtb.Background = new SolidColorBrush(Colors.Orange);

            txtb.Foreground = new SolidColorBrush(Colors.Black);

           
            TopStackPanel.Children.Add(txtb);
            //LayoutRoot.Children.Add(txtb);

        }
    }
}
