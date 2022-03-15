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
            //get data from textboxes
            bias=Int32.Parse(BiasTextBox.Text);
            factor = Int32.Parse(FactorTextBox.Text);
            filterMatrix = new double[Int32.Parse(ColumnTextBox.Text), Int32.Parse(RowTextBox.Text)];

            //display matrix with default values = 0
            ShowMatrix();
            
        }

        private void CreateFilter_Click(object sender, RoutedEventArgs e)
        {
            

        }
        private void ShowMatrix()
        {
            if (null != filterMatrix)
            {
                //mainStackPanel holds n (amount of rows) StackPanels each holding m (amount of columns) of text boxes, giving m*n entries
                StackPanel mainStackPanel = new StackPanel();
                mainStackPanel.Orientation = Orientation.Vertical;
                Grid.SetRow(mainStackPanel, 1);
                MainGrid.Children.Add(mainStackPanel);

                
                StackPanel[] stackPanel = new StackPanel[filterMatrix.GetLength(0)];
                TextBox[] textBox = new TextBox[filterMatrix.GetLength(0) * filterMatrix.GetLength(1)];


                for (int i = 0; i < filterMatrix.GetLength(0); i++)
                {
                    stackPanel[i] = new StackPanel();
                    stackPanel[i].Orientation=Orientation.Horizontal;

                    //RowDefinition rowDefinition = new RowDefinition();
                    //GridLength gridLength = new GridLength(20);
            
                    //rowDefinition.Height = gridLength;
                    //MainGrid.RowDefinitions.Add(rowDefinition);
                    
                    
                    for (int j=0; j < filterMatrix.GetLength(1); j++)
                    {
                        textBox[i * filterMatrix.GetLength(1) + j] = new TextBox();
                        textBox[i * filterMatrix.GetLength(1) + j].Height = 20;
                        textBox[i * filterMatrix.GetLength(1) + j].Width= 20;
                        textBox[i * filterMatrix.GetLength(1) + j].Text = "0";
                        //MainGrid.Children.Add(textBox[i * filterMatrix.GetLength(1) + j]);
                        
                        stackPanel[i].Children.Add(textBox[i * filterMatrix.GetLength(1) + j]);
                        //TopStackPanel.Children.Add(textBox[i * filterMatrix.GetLength(1) + j]);
                    }
                    //Grid.SetRow(stackPanel[i], i+1);
                    //MainGrid.Children.Add(stackPanel[i]);
                    mainStackPanel.Children.Add(stackPanel[i]);
                    
                   // Grid.SetRow(stackPanel[i], i);

                }
            }

        }
        
        
    }
}
