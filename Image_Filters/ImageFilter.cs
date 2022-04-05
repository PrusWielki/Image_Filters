using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Image_Filters
{
    public abstract class ImageFilter
    {
        protected string name;

        public ImageFilter(string _name) { name = _name; }
        public string getName() { return name; }
        abstract public Image applyFilter(Image imgSource, bool isGrayScale);
        public override string ToString() { return name; }
    }
    class Invert : ImageFilter
    {
        public Invert(string _name) : base(_name)
        {
        }

        public override Image applyFilter(Image imgSource, bool isGrayScale)
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

        public override Image applyFilter(Image imgSource, bool isGrayScale)
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

        public override Image applyFilter(Image imgSource, bool isGrayScale)
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

        public override Image applyFilter(Image imgSource, bool isGrayScale)
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

        public override Image applyFilter(Image imgSource, bool isGrayScale)
        {
            Bitmap sourceBitmap = (Bitmap)imgSource.Clone();
            BitmapData sourceData = sourceBitmap.LockBits(new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            //initialize buffer with a size of a picture's height times it's stride (the width of a single row of pixels)
            byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];
            byte[] resultBuffer = new byte[sourceData.Stride * sourceData.Height];


            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);


            sourceBitmap.UnlockBits(sourceData);


            double blue = 0.0;
            double green = 0.0;
            double red = 0.0;

            //get convolution filter matrix height and width(built in filters are 3x3)
            int filterWidth = filterMatrix.GetLength(1);
            int filterHeight = filterMatrix.GetLength(0);

            //offset needed for when we work on pictures around the edges
            int filterOffset = (filterWidth - 1) / 2;
            int calcOffset = 0;


            int byteOffset = 0;

            //start off not from the very beggining of an image but with an offset (in the case of 3x3 filters, instead of starting from point (0,0) - start from (1,1)), the borders of an image are ignored
            for (int offsetY = filterOffset; offsetY < sourceBitmap.Height - filterOffset; offsetY++)
            {
                for (int offsetX = filterOffset; offsetX < sourceBitmap.Width - filterOffset; offsetX++)
                {
                    blue = 0;
                    green = 0;
                    red = 0;

                    //get to the correct set of aRGB values in the correct row, kind of an index
                    byteOffset = offsetY * sourceData.Stride + offsetX * 4;


                    for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
                    {
                        for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
                        {

                            //get index of one the neighbourhood pixel
                            calcOffset = byteOffset + (filterX * 4) + (filterY * sourceData.Stride);

                            //calculate new values for the pixel at byteoffset, multiple chosen neighbhour pixel by corresponding matrix value

                            blue += (double)(pixelBuffer[calcOffset]) * filterMatrix[filterY + filterOffset, filterX + filterOffset];

                            green += (double)(pixelBuffer[calcOffset + 1]) * filterMatrix[filterY + filterOffset, filterX + filterOffset];

                            red += (double)(pixelBuffer[calcOffset + 2]) * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                        }
                    }

                    //apply factor and bias to the newly calculated values
                    blue = factor * blue + bias;
                    green = factor * green + bias;
                    red = factor * red + bias;

                    //normilize newly caluclated values to the 0-255 range
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

                    //save resulting pixel
                    resultBuffer[byteOffset] = (byte)(blue);
                    resultBuffer[byteOffset + 1] = (byte)(green);
                    resultBuffer[byteOffset + 2] = (byte)(red);
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


            return resultBitmap;
        }
    }
    class Blur3x3Filter : ConvolutionFilterBase
    {
        public Blur3x3Filter(string _name) : base(_name, 1.0 / 9.0, 0.0, new double[,] { { 1, 1, 1, }, { 1, 1, 1, }, { 1, 1, 1, }, })
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

    class ErrorDiffusionBase : ImageFilter
    {


        protected double[,] filterMatrix;

        protected double rMultiplier;
        protected double gMultiplier;
        protected double bMultiplier;




        protected ErrorDiffusionBase(string _name, double[,] _matrix, double _rMultiplier, double _gMultiplier, double _bMultiplier ) : base(_name)
        {
            rMultiplier = _rMultiplier;
            gMultiplier = _gMultiplier;
            bMultiplier = _bMultiplier;
            filterMatrix = _matrix;
        }


        public override Image applyFilter(Image imgSource, bool isGrayscale)
        {
            Bitmap sourceBitmap = (Bitmap)imgSource.Clone();
            BitmapData sourceData;
           // if (isGrayscale)
               //  sourceData = sourceBitmap.LockBits(new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format16bppGrayScale);
           // else
                 sourceData = sourceBitmap.LockBits(new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            //initialize buffer with a size of a picture's height times it's stride (the width of a single row of pixels)
            byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];
            byte[] resultBuffer = new byte[sourceData.Stride * sourceData.Height];


            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);


            sourceBitmap.UnlockBits(sourceData);


            double blue = 0.0;
            double green = 0.0;
            double red = 0.0;

            double errorBlue = 0.0;
            double errorGreen = 0.0;
            double errorRed = 0.0;


            double grayScale = 0.0;
            //get convolution filter matrix height and width(built in filters are 3x3)
            int filterWidth = filterMatrix.GetLength(1);
            int filterHeight = filterMatrix.GetLength(0);

            //offset needed for when we work on pictures around the edges
            int filterWidthOffset = (filterWidth - 1) / 2;
            int filterHeightOffset = (filterHeight - 1) / 2;
            int calcOffset = 0;


            int byteOffset = 0;

            //start off not from the very beggining of an image but with an offset (in the case of 3x3 filters, instead of starting from point (0,0) - start from (1,1)), the borders of an image are ignored
            for (int offsetY = filterHeightOffset; offsetY < sourceBitmap.Height - filterHeightOffset; offsetY++)
            {
                for (int offsetX = filterWidthOffset; offsetX < sourceBitmap.Width - filterWidthOffset; offsetX++)
                {
                    blue = 0;
                    green = 0;
                    red = 0;

                    //get to the correct set of aRGB values in the correct row, kind of an index
                    byteOffset = offsetY * sourceData.Stride + offsetX * 4;

                    //approximate                    red                                      green                                 blue
                    grayScale = (double)((pixelBuffer[byteOffset+2] * rMultiplier) + (pixelBuffer[byteOffset + 1] * gMultiplier) + (pixelBuffer[byteOffset] * bMultiplier));
                    //grayScale = (double)((pixelBuffer[byteOffset + 2] * 0.5) + (pixelBuffer[byteOffset + 1] * 0.8) + (pixelBuffer[byteOffset] * 0.05));

                    //save resulting pixel
                    resultBuffer[byteOffset] = (byte)(grayScale);
                    resultBuffer[byteOffset + 1] = (byte)(grayScale);
                    resultBuffer[byteOffset + 2] = (byte)(grayScale);
                    resultBuffer[byteOffset + 3] = 255;

                    //Calculate the error
                    errorBlue = pixelBuffer[byteOffset] - grayScale;
                    errorGreen = pixelBuffer[byteOffset + 1] - grayScale;
                    errorRed = pixelBuffer[byteOffset + 2] - grayScale;

                    for (int filterY = -filterHeightOffset; filterY <= filterHeightOffset; filterY++)
                    {
                        for (int filterX = -filterWidthOffset; filterX <= filterWidthOffset; filterX++)
                        {

                            //get index of one the neighbourhood pixel
                            calcOffset = byteOffset + (filterX * 4) + (filterY * sourceData.Stride);





                            //calculate new values for the pixel at byteoffset, multiply chosen neighbhour pixel by corresponding matrix value

                            pixelBuffer[calcOffset] += (byte)(errorBlue * filterMatrix[filterY + filterHeightOffset, filterX + filterWidthOffset]);

                            pixelBuffer[calcOffset + 1] += (byte)(errorGreen * filterMatrix[filterY + filterHeightOffset, filterX + filterWidthOffset]);

                            pixelBuffer[calcOffset + 2] += (byte)(errorRed * filterMatrix[filterY + filterHeightOffset, filterX + filterWidthOffset]);
                        }
                    }



                }
            }

            //save results to new bitmap
            Bitmap resultBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);


            BitmapData resultData = resultBitmap.LockBits(new Rectangle(0, 0,
                                    resultBitmap.Width, resultBitmap.Height),
                                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);


            Marshal.Copy(resultBuffer, 0, resultData.Scan0, resultBuffer.Length);
            resultBitmap.UnlockBits(resultData);


            return resultBitmap;
        }
    }

    class FloydAndSteinbergFilter : ErrorDiffusionBase
    {
        public FloydAndSteinbergFilter(string _name, double _rMultiplier, double _gMultiplier, double _bMultiplier) : base(_name, new double[,] { { 0, 0, 0, }, { 0, 0, 7.0 / 16.0, }, { 3.0 / 16.0, 5.0 / 16.0, 1.0 / 16.0, }, }, _rMultiplier, _gMultiplier, _bMultiplier)
        {
        }
    }
    class BurkesFilter : ErrorDiffusionBase
    {
        public BurkesFilter(string _name, double _rMultiplier, double _gMultiplier, double _bMultiplier) : base(_name, new double[,] { { 0, 0, 0, 0, 0, }, { 0, 0, 0, 8.0 / 32.0, 4.0 / 32.0, }, { 2.0 / 32.0, 4.0 / 32.0, 8.0 / 32.0, 4.0 / 32.0, 2.0 / 32.0, }, }, _rMultiplier, _gMultiplier, _bMultiplier)
        {
        }
    }
    class StuckyFilter : ErrorDiffusionBase
    {
        public StuckyFilter(string _name, double _rMultiplier, double _gMultiplier, double _bMultiplier) : base(_name, new double[,] { { 0, 0, 0, 0, 0, }, { 0, 0, 0, 0, 0, }, { 0, 0, 0, 8.0 / 42.0, 4.0 / 42.0, }, { 2.0 / 42.0, 4.0 / 42.0, 8.0 / 42.0, 4.0 / 42.0, 2.0 / 42.0, }, { 1.0 / 42.0, 2.0 / 42.0, 4.0 / 42.0, 2.0 / 42.0, 1.0 / 42.0, } }, _rMultiplier, _gMultiplier, _bMultiplier)
        {
        }
    }
    class SierraFilter : ErrorDiffusionBase
    {
        public SierraFilter(string _name, double _rMultiplier, double _gMultiplier, double _bMultiplier) : base(_name, new double[,] { { 0, 0, 0, 0, 0, }, { 0, 0, 0, 0, 0, }, { 0, 0, 0, 5.0 / 32.0, 3.0 / 32.0, }, { 2.0 / 32.0, 4.0 / 32.0, 5.0 / 32.0, 4.0 / 32.0, 2.0 / 32.0, }, { 0.0, 2.0 / 32.0, 3.0 / 32.0, 2.0 / 32.0, 0.0, }, }, _rMultiplier, _gMultiplier, _bMultiplier)
        {
        }
    }
    class AtkinsonFilter : ErrorDiffusionBase
    {
        public AtkinsonFilter(string _name, double _rMultiplier, double _gMultiplier, double _bMultiplier) : base(_name, new double[,] { { 0, 0, 0, 0, 0, }, { 0, 0, 0, 0, 0, }, { 0, 0, 0, 1.0 / 8.0, 1.0 / 8.0, }, { 0.0, 1.0 / 8.0, 1.0 / 8.0, 1.0 / 8.0, 0.0, }, { 0.0, 0.0, 1.0 / 8.0, 0.0, 0.0, }, },  _rMultiplier,  _gMultiplier,  _bMultiplier)
        {
        }
    }
    class YCbCrFilter : ErrorDiffusionBase
    {
        public YCbCrFilter(string _name, double _rMultiplier, double _gMultiplier, double _bMultiplier) : base(_name, new double[,] { { 0, 0, 0, 0, 0, }, { 0, 0, 0, 0, 0, }, { 0, 0, 0, 1.0 / 8.0, 1.0 / 8.0, }, { 0.0, 1.0 / 8.0, 1.0 / 8.0, 1.0 / 8.0, 0.0, }, { 0.0, 0.0, 1.0 / 8.0, 0.0, 0.0, }, }, _rMultiplier, _gMultiplier, _bMultiplier)
        {
        }
        public override Image applyFilter(Image imgSource, bool isGrayScale)
        {
            Bitmap sourceBitmap = (Bitmap)YCbCr(imgSource);//(Bitmap)imgSource.Clone();
            BitmapData sourceData = sourceBitmap.LockBits(new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            //initialize buffer with a size of a picture's height times it's stride (the width of a single row of pixels)
            byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];
            byte[] resultBuffer = new byte[sourceData.Stride * sourceData.Height];


            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);


            sourceBitmap.UnlockBits(sourceData);


            double blue = 0.0;
            double green = 0.0;
            double red = 0.0;

            double errorBlue = 0.0;
            double errorGreen = 0.0;
            double errorRed = 0.0;


            double grayScale = 0.0;
            //get convolution filter matrix height and width(built in filters are 3x3)
            int filterWidth = filterMatrix.GetLength(1);
            int filterHeight = filterMatrix.GetLength(0);

            //offset needed for when we work on pictures around the edges
            int filterWidthOffset = (filterWidth - 1) / 2;
            int filterHeightOffset = (filterHeight - 1) / 2;
            int calcOffset = 0;


            int byteOffset = 0;

            //start off not from the very beggining of an image but with an offset (in the case of 3x3 filters, instead of starting from point (0,0) - start from (1,1)), the borders of an image are ignored
            for (int offsetY = filterHeightOffset; offsetY < sourceBitmap.Height - filterHeightOffset; offsetY++)
            {
                for (int offsetX = filterWidthOffset; offsetX < sourceBitmap.Width - filterWidthOffset; offsetX++)
                {
                    blue = 0;
                    green = 0;
                    red = 0;

                    //get to the correct set of aRGB values in the correct row, kind of an index
                    byteOffset = offsetY * sourceData.Stride + offsetX * 4;


                    resultBuffer[byteOffset] = (byte)(System.Math.Round(pixelBuffer[byteOffset] / 4.0) * 4.0);
                    resultBuffer[byteOffset + 1] = (byte)(System.Math.Round(pixelBuffer[byteOffset + 1] / 4.0) * 4.0);
                    resultBuffer[byteOffset + 2] = (byte)(System.Math.Round(pixelBuffer[byteOffset + 2] / 4.0) * 4.0);
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

            Bitmap finalBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);
            finalBitmap = (Bitmap)RGB(resultBitmap);
            return finalBitmap;
        }
        private Image YCbCr(Image imgSource)
        {
            Bitmap sourceBitmap = (Bitmap)imgSource.Clone();


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
            for (int offsetY = 0; offsetY < sourceBitmap.Height; offsetY++)
            {
                for (int offsetX = 0; offsetX < sourceBitmap.Width; offsetX++)
                {
                    blue = 0;
                    green = 0;
                    red = 0;

                    //get to the correct set of aRGB values in the correct row, kind of an index
                    byteOffset = offsetY * sourceData.Stride + offsetX * 4;


                    resultBuffer[byteOffset + 2] = (byte)(pixelBuffer[byteOffset + 2] * 0.299 + pixelBuffer[byteOffset + 1] * 0.587 + pixelBuffer[byteOffset] * 0.114);
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


            return resultBitmap;

        }
        private Image RGB(Image imgSource)
        {
            Bitmap sourceBitmap = (Bitmap)imgSource.Clone();


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
            for (int offsetY = 0; offsetY < sourceBitmap.Height; offsetY++)
            {
                for (int offsetX = 0; offsetX < sourceBitmap.Width; offsetX++)
                {
                    blue = 0;
                    green = 0;
                    red = 0;

                    //get to the correct set of aRGB values in the correct row, kind of an index
                    byteOffset = offsetY * sourceData.Stride + offsetX * 4;


                    resultBuffer[byteOffset + 2] = (byte)(pixelBuffer[byteOffset + 2] + 1.402 * (pixelBuffer[byteOffset + 1] - 128));
                    resultBuffer[byteOffset + 1] = (byte)(pixelBuffer[byteOffset + 2] - 0.34414 * (pixelBuffer[byteOffset] - 128) - 0.71414 * (pixelBuffer[byteOffset + 1] - 128));
                    resultBuffer[byteOffset] = (byte)(pixelBuffer[byteOffset + 2] + 1.772 * (pixelBuffer[byteOffset] - 128));
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


            return resultBitmap;

        }
    }
    class UniformQuantization : ImageFilter
    {


       

        protected double rMultiplier;
        protected double gMultiplier;
        protected double bMultiplier;

        private int[] rCount;
        private int[] gCount;
        private int[] bCount;

        public UniformQuantization(string _name, double _rMultiplier, double _gMultiplier, double _bMultiplier) : base(_name)
        {
            rMultiplier = _rMultiplier;
            gMultiplier = _gMultiplier;
            bMultiplier = _bMultiplier;
            

        }


        public override Image applyFilter(Image imgSource, bool isGrayscale)
        {
            //5 is a magical constant thar solves the problem with "index out of bounds"
            rCount = new int[(int)rMultiplier + 5];
            gCount = new int[(int)gMultiplier + 5];
            bCount = new int[(int)bMultiplier + 5];

            Bitmap sourceBitmap = (Bitmap)imgSource.Clone();
            BitmapData sourceData;
            // if (isGrayscale)
            //  sourceData = sourceBitmap.LockBits(new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format16bppGrayScale);
            // else
            sourceData = sourceBitmap.LockBits(new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            //initialize buffer with a size of a picture's height times it's stride (the width of a single row of pixels)
            byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];
            byte[] resultBuffer = new byte[sourceData.Stride * sourceData.Height];


            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);


            sourceBitmap.UnlockBits(sourceData);

            double rStepSize = 255 / rMultiplier;
            double gStepSize = 255 / gMultiplier;
            double bStepSize = 255 / bMultiplier;


            List<KeyValuePair<int, int>> rRegionValue=new List<KeyValuePair<int, int>>();
            List<KeyValuePair<int, int>> gRegionValue = new List<KeyValuePair<int, int>>();
            List<KeyValuePair<int, int>> bRegionValue = new List<KeyValuePair<int, int>>();

            double[] rmeans = new double[(int)rMultiplier+5];
            double[] gmeans = new double[(int)gMultiplier+5];
            double[] bmeans = new double[(int)bMultiplier+5];

            double[] rtotals = new double[(int)rMultiplier+5];
            double[] gtotals = new double[(int)gMultiplier+5];
            double[] btotals = new double[(int)bMultiplier+5];

            
            int byteOffset = 0;

            //start off not from the very beggining of an image but with an offset (in the case of 3x3 filters, instead of starting from point (0,0) - start from (1,1)), the borders of an image are ignored
            for (int offsetY = 0; offsetY < sourceBitmap.Height; offsetY++)
            {
                for (int offsetX = 0; offsetX < sourceBitmap.Width; offsetX++)
                {


                    //get to the correct set of aRGB values in the correct row, kind of an index
                    byteOffset = offsetY * sourceData.Stride + offsetX * 4;

                    rRegionValue.Add(new KeyValuePair<int,int>(GetRegion((int)rMultiplier, pixelBuffer[byteOffset + 2]), pixelBuffer[byteOffset + 2]));
                    
                    
                    
                  rCount[GetRegion((int)rMultiplier, pixelBuffer[byteOffset + 2])]++;
                    gRegionValue.Add(new KeyValuePair<int, int>(GetRegion((int)gMultiplier, pixelBuffer[byteOffset + 1]), pixelBuffer[byteOffset + 1]));



                    
                    gCount[GetRegion((int)gMultiplier, pixelBuffer[byteOffset + 1])]++;
                    bRegionValue.Add(new KeyValuePair<int, int>(GetRegion((int)bMultiplier, pixelBuffer[byteOffset]), pixelBuffer[byteOffset]));
                    bCount[GetRegion((int)bMultiplier, pixelBuffer[byteOffset])]++;

                }
            }
            foreach(var val in rRegionValue)
            {
                rmeans[val.Key] += val.Value;

            }
            foreach (var val in gRegionValue)
            {
                gmeans[val.Key] += val.Value;

            }
            foreach (var val in bRegionValue)
            {
                bmeans[val.Key] += val.Value;

            }
            for(int i = 0; i < rMultiplier; i++)
            {
                rmeans[i] = rmeans[i] / rCount[i];
            }
            for (int i = 0; i < gMultiplier; i++)
            {
                gmeans[i] = gmeans[i] / gCount[i];
            }
            for (int i = 0; i < bMultiplier; i++)
            {
                bmeans[i] = bmeans[i] / bCount[i];
            }


            for (int offsetY = 0; offsetY < sourceBitmap.Height; offsetY++)
            {
                for (int offsetX = 0; offsetX < sourceBitmap.Width; offsetX++)
                {
            

                    //get to the correct set of aRGB values in the correct row, kind of an index
                    byteOffset = offsetY * sourceData.Stride + offsetX * 4;

                    resultBuffer[byteOffset + 2]= (byte)rmeans[GetRegion((int)rMultiplier, pixelBuffer[byteOffset + 2])];
                    resultBuffer[byteOffset + 1]= (byte)gmeans[GetRegion((int)gMultiplier, pixelBuffer[byteOffset + 1])];
                    resultBuffer[byteOffset]= (byte)bmeans[GetRegion((int)bMultiplier, pixelBuffer[byteOffset])];
                    resultBuffer[byteOffset + 3]=pixelBuffer[byteOffset + 3];




                }
            }


            //save results to new bitmap
            Bitmap resultBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);


            BitmapData resultData = resultBitmap.LockBits(new Rectangle(0, 0,
                                    resultBitmap.Width, resultBitmap.Height),
                                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);


            Marshal.Copy(resultBuffer, 0, resultData.Scan0, resultBuffer.Length);
            resultBitmap.UnlockBits(resultData);


            return resultBitmap;
        }
        private int GetRegion(int amount_of_regions, double value)
        {
            int step = 255 / amount_of_regions;


            return (int)(value / step);


        }
    }
}
