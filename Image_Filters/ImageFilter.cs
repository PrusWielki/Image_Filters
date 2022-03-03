using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Image_Filters
{
    abstract class ImageFilter
    {
        protected string name;

        public ImageFilter(string _name) { name = _name; }
        public string getName() { return name; }
        abstract public Image applyFilter(Image imgSource);
        public override string ToString() { return name; }
    }
    class Invert : ImageFilter
    {
        public Invert(string _name) : base(_name)
        {
        }

        public override Image applyFilter(Image imgSource)
        {
            Bitmap bmpDest = new Bitmap(imgSource.Width,
         imgSource.Height);

            ColorMatrix clrMatrix = new ColorMatrix(new float[][]
               {
            new float[] {-1, 0, 0, 0, 0},
            new float[] {0, -1, 0, 0, 0},
            new float[] {0, 0, -1, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {1, 1, 1, 0, 1}
               });

            using (ImageAttributes attrImage = new ImageAttributes())
            {

                attrImage.SetColorMatrix(clrMatrix);

                using (Graphics g = Graphics.FromImage(bmpDest))
                {
                    g.DrawImage(imgSource, new Rectangle(0, 0,
                    imgSource.Width, imgSource.Height), 0, 0,
                    imgSource.Width, imgSource.Height, GraphicsUnit.Pixel,
                    attrImage);
                }
            }

            return bmpDest;
        }
    }
    class BrightnessCorrection : ImageFilter
    {
        public BrightnessCorrection(string _name) : base(_name)
        {
        }

        public override Image applyFilter(Image imgSource)
        {
            Bitmap bmpDest = new Bitmap(imgSource.Width,
         imgSource.Height);

            ColorMatrix clrMatrix = new ColorMatrix(new float[][]
               {
            new float[] {1, 0, 0, 0, 0},
            new float[] {0, 1, 0, 0, 0},
            new float[] {0, 0, 1, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {0.5F, 0.5F, 0.5F, 0, 1}
               });

            using (ImageAttributes attrImage = new ImageAttributes())
            {

                attrImage.SetColorMatrix(clrMatrix);

                using (Graphics g = Graphics.FromImage(bmpDest))
                {
                    g.DrawImage(imgSource, new Rectangle(0, 0,
                    imgSource.Width, imgSource.Height), 0, 0,
                    imgSource.Width, imgSource.Height, GraphicsUnit.Pixel,
                    attrImage);
                }
            }

            return bmpDest;
        }
    }
    class ContrastEnchancement : ImageFilter
    {
        public ContrastEnchancement(string _name) : base(_name)
        {
        }

        public override Image applyFilter(Image imgSource)
        {
            double value = 30;
            value = (100.0f + value) / 100.0f;
            value *= value;
            Bitmap newBitmap = (Bitmap)imgSource.Clone();
            BitmapData data = newBitmap.LockBits(
                new Rectangle(0, 0, newBitmap.Width, newBitmap.Height),
                ImageLockMode.ReadWrite,
                newBitmap.PixelFormat);
            int height = newBitmap.Height;
            int width = newBitmap.Width;

            unsafe
            {
                for (int y = 0; y < height; ++y)
                {
                    byte* row = (byte*)data.Scan0 + (y * data.Stride);
                    int columnOffset = 0;
                    for (int x = 0; x < width; ++x)
                    {
                        byte B = row[columnOffset];
                        byte G = row[columnOffset + 1];
                        byte R = row[columnOffset + 2];

                        float Red = R / 255.0f;
                        float Green = G / 255.0f;
                        float Blue = B / 255.0f;
                        Red = (float)((((Red - 0.5f) * value) + 0.5f) * 255.0f);
                        Green = (float)((((Green - 0.5f) * value) + 0.5f) * 255.0f);
                        Blue = (float)((((Blue - 0.5f) * value) + 0.5f) * 255.0f);

                        int iR = (int)Red;
                        iR = iR > 255 ? 255 : iR;
                        iR = iR < 0 ? 0 : iR;
                        int iG = (int)Green;
                        iG = iG > 255 ? 255 : iG;
                        iG = iG < 0 ? 0 : iG;
                        int iB = (int)Blue;
                        iB = iB > 255 ? 255 : iB;
                        iB = iB < 0 ? 0 : iB;

                        row[columnOffset] = (byte)iB;
                        row[columnOffset + 1] = (byte)iG;
                        row[columnOffset + 2] = (byte)iR;

                        columnOffset += 4;
                    }
                }
            }

            newBitmap.UnlockBits(data);

            return newBitmap;
        }
    }
    class GammaCorrection : ImageFilter
    {
        public GammaCorrection(string _name) : base(_name)
        {
        }

        public override Image applyFilter(Image imgSource)
        {
            float gamma = 1.5F;
            // Set the ImageAttributes object's gamma value.
            ImageAttributes attributes = new ImageAttributes();
            attributes.SetGamma(gamma);

            // Draw the image onto the new bitmap
            // while applying the new gamma value.
            Point[] points = { new Point(0, 0), new Point(imgSource.Width, 0), new Point(0, imgSource.Height), };
            Rectangle rect = new Rectangle(0, 0, imgSource.Width, imgSource.Height);

            // Make the result bitmap.
            Bitmap bm = new Bitmap(imgSource.Width, imgSource.Height);
            using (Graphics gr = Graphics.FromImage(bm))
            {
                gr.DrawImage(imgSource, points, rect, GraphicsUnit.Pixel, attributes);
            }

            // Return the result.
            return bm;


        }
    }
    class ConvolutionFilterBase : ImageFilter
    {
        protected double factor;
        protected double bias;
        protected double[,] filterMatrix;

        public ConvolutionFilterBase(string _name, double _factor, double _bias, double[,] _filterMatrix) : base(_name)
        {
            factor = _factor;
            bias = _bias;
            filterMatrix = _filterMatrix;
        }

        public override Image applyFilter(Image imgSource)
        {
            Bitmap sourceBitmap = (Bitmap)imgSource.Clone();
            BitmapData sourceData = sourceBitmap.LockBits(new Rectangle(0, 0,
                                sourceBitmap.Width, sourceBitmap.Height),
                                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);


            byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];
            byte[] resultBuffer = new byte[sourceData.Stride * sourceData.Height];


            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);


            sourceBitmap.UnlockBits(sourceData);


            double blue = 0.0;
            double green = 0.0;
            double red = 0.0;


            int filterWidth = filterMatrix.GetLength(1);
            int filterHeight = filterMatrix.GetLength(0);


            int filterOffset = (filterWidth - 1) / 2;
            int calcOffset = 0;


            int byteOffset = 0;


            for (int offsetY = filterOffset; offsetY <
                 sourceBitmap.Height - filterOffset; offsetY++)
            {
                for (int offsetX = filterOffset; offsetX <
                     sourceBitmap.Width - filterOffset; offsetX++)
                {
                    blue = 0;
                    green = 0;
                    red = 0;


                    byteOffset = offsetY *
                                    sourceData.Stride +
                                    offsetX * 4;


                    for (int filterY = -filterOffset;
                         filterY <= filterOffset; filterY++)
                    {
                        for (int filterX = -filterOffset;
                             filterX <= filterOffset; filterX++)
                        {


                            calcOffset = byteOffset +
                                         (filterX * 4) +
                                         (filterY * sourceData.Stride);


                            blue += (double)(pixelBuffer[calcOffset]) *
                                     filterMatrix[filterY + filterOffset,
                                     filterX + filterOffset];


                            green += (double)(pixelBuffer[calcOffset + 1]) *
                                      filterMatrix[filterY + filterOffset,
                                      filterX + filterOffset];


                            red += (double)(pixelBuffer[calcOffset + 2]) *
                                    filterMatrix[filterY + filterOffset,
                                    filterX + filterOffset];
                        }
                    }


                    blue = factor * blue + bias;
                    green = factor * green + bias;
                    red = factor * red + bias;


                    if (blue > 255)
                    { blue = 255; }
                    else if (blue < 0)
                    { blue = 0; }


                    if (green > 255)
                    { green = 255; }
                    else if (green < 0)
                    { green = 0; }


                    if (red > 255)
                    { red = 255; }
                    else if (red < 0)
                    { red = 0; }


                    resultBuffer[byteOffset] = (byte)(blue);
                    resultBuffer[byteOffset + 1] = (byte)(green);
                    resultBuffer[byteOffset + 2] = (byte)(red);
                    resultBuffer[byteOffset + 3] = 255;
                }
            }


            Bitmap resultBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);


            BitmapData resultData = resultBitmap.LockBits(new Rectangle(0, 0,
                                    resultBitmap.Width, resultBitmap.Height),
                                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);


            Marshal.Copy(resultBuffer, 0, resultData.Scan0, resultBuffer.Length);
            resultBitmap.UnlockBits(resultData);


            return resultBitmap;
        }
    }
    class Blur3x3Filter : ConvolutionFilterBase
    {
        public Blur3x3Filter(string _name) : base(_name, 1.0, 0.0, new double[,] { { 0.0, 0.2, 0.0, }, { 0.2, 0.2, 0.2, }, { 0.0, 0.2, 0.2, }, })
        {
        }
    }
    class Gaussian3x3BlurFilter : ConvolutionFilterBase
    {
        public Gaussian3x3BlurFilter(string _name) : base(_name, 1.0 / 16.0, 0.0, new double[,] { { 1, 2, 1, }, { 2, 4, 2, }, { 1, 2, 1, }, })
        {
        }
    }
    class Sharpen3x3Filter : ConvolutionFilterBase
    {
        public Sharpen3x3Filter(string _name) : base(_name, 1.0, 0.0, new double[,] { { 0, -1, 0, }, { -1, 5, -1, }, { 0, -1, 0, }, })
        {
        }
    }

}
