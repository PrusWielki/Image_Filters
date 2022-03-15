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
        public string filterName;

        private TextBox[] textBox;
        public NewFilterWindow()
        {
           
            InitializeComponent();
        }
        private void CreateMatrix_Click(object sender, RoutedEventArgs e)
        {
            //get data from textboxes
            bias=Double.Parse(BiasTextBox.Text);
            factor = Double.Parse(FactorTextBox.Text);
            filterMatrix = new double[Int32.Parse(ColumnTextBox.Text), Int32.Parse(RowTextBox.Text)];
            filterName = FilterNameTextBox.Text;
            //display matrix with default values = 0
            ShowMatrix();
            
        }

        //Copy data from textboxes to matrix variable
        public void SaveDataToMatrix()
        {

            for(int i=0;i< filterMatrix.GetLength(0); i++)
            {

                for(int j=0;j< filterMatrix.GetLength(1); j++)
                {
                    filterMatrix[i,j]=Int32.Parse(textBox[i* filterMatrix.GetLength(1)+j].Text);

                }
            }


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
                textBox = new TextBox[filterMatrix.GetLength(0) * filterMatrix.GetLength(1)];
                


                for (int i = 0; i < filterMatrix.GetLength(0); i++)
                {
                    stackPanel[i] = new StackPanel();
                    stackPanel[i].Orientation=Orientation.Horizontal;
                               
                    for (int j=0; j < filterMatrix.GetLength(1); j++)
                    {
                        textBox[i * filterMatrix.GetLength(1) + j] = new TextBox();
                        textBox[i * filterMatrix.GetLength(1) + j].Height = 20;
                        textBox[i * filterMatrix.GetLength(1) + j].Width= 20;
                        textBox[i * filterMatrix.GetLength(1) + j].Text = "0";
                        textBox[i * filterMatrix.GetLength(1) + j].TextChanged += NewFilterWindow_TextChanged;

                        stackPanel[i].Children.Add(textBox[i * filterMatrix.GetLength(1) + j]);
                    }

                    mainStackPanel.Children.Add(stackPanel[i]);
                    

                }
            }

        }
        //this is not a good solution, but it works (sometimes)
        private void NewFilterWindow_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveDataToMatrix();
        }
    }
}
