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
            //convert Image to Bitmap, to perform operations on pixels
            Bitmap pic = (Bitmap)imgSource.Clone();


            for (int y = 0; (y <= (pic.Height - 1)); y++)
            {
                for (int x = 0; (x <= (pic.Width - 1)); x++)
                {
                    //get color of a pixel located at point (x,y) in the picture
                    Color inv = pic.GetPixel(x, y);
                    //invert the pixel's color
                    inv = Color.FromArgb(255, 255 - inv.R, 255 - inv.G, 255 - inv.B);
                    //assign new color to a pixel at point (x,y)
                    pic.SetPixel(x, y, inv);
                }
            }

            return pic;
        }
    }
    class BrightnessCorrection : ImageFilter
    {
        public BrightnessCorrection(string _name) : base(_name)
        {
        }

        public override Image applyFilter(Image imgSource)
        {
            //amount dictates by how much should the image be brightened
            int amount = 10;

            Bitmap bitmap = (Bitmap)imgSource.Clone();

            //lock the entire bitmap, the whole image, in system memory, so that the bitmap can be altered programatically
            //also get data about the bitmap such as the address of the first pixel or length of a row of pixels
            BitmapData bmData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            //get the width of a single row of pixels
            int stride = bmData.Stride;
            //get address of the first pixel in the bitmap
            System.IntPtr Scan0 = bmData.Scan0;

            //stores the new R,G or B value increased by "amount"
            int nVal = 0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;

                //each of the below is multiplied by 3, cause each RGB channel is dealt with separately
                int nOffset = stride - bitmap.Width * 3;
                int nWidth = bitmap.Width * 3;

                for (int y = 0; y < bitmap.Height; ++y)
                {
                    //go through the whole line at row y and increase each R,G,B value by "amount"
                    for (int x = 0; x < nWidth; ++x)
                    {
                        nVal = (int)(p[0] + amount);

                        //in case new value doesnt fit into the 0-255 range
                        if (nVal < 0) nVal = 0;
                        if (nVal > 255) nVal = 255;

                        p[0] = (byte)nVal;

                        //move the pointer to the next R,G,B value
                        ++p;
                    }
                    //skip the bytes that dont correspond to pixel's RGB values and move to the next line
                    p += nOffset;
                }
            }

            bitmap.UnlockBits(bmData);

            return bitmap;
        }
    }
    class ContrastEnchancement : ImageFilter
    {
        public ContrastEnchancement(string _name) : base(_name)
        {
        }

        public override Image applyFilter(Image imgSource)
        {
            //value dictates by how much should the contrast be changed, controlled by CONTR macro
            double value = 30;

            //value is scaled to a to 0-x format (for example 1.1 to brighten up the image or 0.9 to darken it), cause the R,G,B value will be multiplied by that value
            value = (100.0f + value) / 100.0f;
            //value *= value;


            Bitmap newBitmap = (Bitmap)imgSource.Clone();

            //lock the bitmap in the memory, so that we can get the address of the first pixel
            BitmapData data = newBitmap.LockBits(new Rectangle(0, 0, newBitmap.Width, newBitmap.Height), ImageLockMode.ReadWrite, newBitmap.PixelFormat);


            int height = newBitmap.Height;
            int width = newBitmap.Width;

            unsafe
            {
                for (int y = 0; y < height; ++y)
                {
                    //get the pointer to the y'th row, to the first R,G or B value
                    byte* row = (byte*)data.Scan0 + (y * data.Stride);


                    int columnOffset = 0; //kind of an index going through all columns
                    //go through the whole row
                    for (int x = 0; x < width; ++x)
                    {

                        //read R,G,B values, the are stored in BGR format
                        byte B = row[columnOffset];
                        byte G = row[columnOffset + 1];
                        byte R = row[columnOffset + 2];

                        //change pixel's RGB values to change it's brightness
                        float Red = R / 255.0f;
                        float Green = G / 255.0f;
                        float Blue = B / 255.0f;
                        Red = (float)((((Red - 0.5f) * value) + 0.5f) * 255.0f);
                        Green = (float)((((Green - 0.5f) * value) + 0.5f) * 255.0f);
                        Blue = (float)((((Blue - 0.5f) * value) + 0.5f) * 255.0f);


                        //normalize values to the 0-255 range
                        int iR = (int)Red;
                        iR = iR > 255 ? 255 : iR;
                        iR = iR < 0 ? 0 : iR;
                        int iG = (int)Green;
                        iG = iG > 255 ? 255 : iG;
                        iG = iG < 0 ? 0 : iG;
                        int iB = (int)Blue;
                        iB = iB > 255 ? 255 : iB;
                        iB = iB < 0 ? 0 : iB;

                        //assign new values
                        row[columnOffset] = (byte)iB;
                        row[columnOffset + 1] = (byte)iG;
                        row[columnOffset + 2] = (byte)iR;
                        //move to the next set of RGB values
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
            int width = imgSource.Width;
            int height = imgSource.Height;
            Bitmap newBitmap = (Bitmap)imgSource.Clone();

            //values needed for the S=C*R^y equation
            double gamma = 1.5;
            double c = 1;

            BitmapData srcData = newBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            //hold the amount of bytes needed to represent the image's pixels
            int bytes = srcData.Stride * srcData.Height;
            byte[] buffer = new byte[bytes];
            byte[] result = new byte[bytes];
            
            //copy image data to the buffer
            Marshal.Copy(srcData.Scan0, buffer, 0, bytes);
            newBitmap.UnlockBits(srcData);

            //current stands for the current pixel the gamma correction is applied at
            int current = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    current = y * srcData.Stride + x * 4;
                    //iterate over R,G,B
                    for (int i = 0; i < 3; i++)
                    {
                        double range = (double)buffer[current + i] / 255;
                        double correction = c * System.Math.Pow(range, gamma);
                        result[current + i] = (byte)(correction * 255);
                    }
                    //alpha channel
                    result[current + 3] = 255;
                }
            }
            //create a new bitmap with changed pixel rgb values
            Bitmap resImg = new Bitmap(width, height);
            BitmapData resData = resImg.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(result, 0, resData.Scan0, bytes);
            resImg.UnlockBits(resData);

            return resImg;
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
    class EdgeDetectionFilter : ConvolutionFilterBase
    {
        public EdgeDetectionFilter(string _name) : base(_name, 1.0, 0.0, new double[,] { { -5, 0, 0, }, { 0, 0, 0, }, { 0, 0, 5, }, })
        {
        }
    }
    class EmbossFilter : ConvolutionFilterBase
    {
        public EmbossFilter(string _name) : base(_name, 1.0, 128.0, new double[,] { { -1, 0, 0, }, { 0, 0, 0, }, { 0, 0, 1, }, })
        {
        }
    }

}
