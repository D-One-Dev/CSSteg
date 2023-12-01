using System;
using System.Drawing;
using System.Windows.Forms;

namespace CSSteg
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private enum State
        {
            Hiding,
            Filling_With_Zeros
        };

        private Bitmap EncryptText(string text, Bitmap bmp) //encrypting text in image
        {
            State state = State.Hiding; //initially, we'll be hiding characters in the image
            int charIndex = 0; //index of the character that is being hidden
            int charValue = 0; //integer value of the character
            long pixelElementIndex = 0; //index of the color element (R/G/B) that is currently being processed
            int zeros = 0; //number of trailing zeros that have been added when finishing the process

            //pixel elements
            int R = 0, G = 0, B = 0, A = 0;

            for (int i = 0; i < bmp.Height; i++) //pass through the rows
            {
                for (int j = 0; j < bmp.Width; j++) //pass through each row
                {
                    Color pixel = bmp.GetPixel(j, i); //current pixel

                    //clearing the least significant bit (LSB) from each pixel element
                    R = pixel.R - pixel.R % 2;
                    G = pixel.G - pixel.G % 2;
                    B = pixel.B - pixel.B % 2;
                    A = pixel.A - pixel.A % 2;

                    //for each pixel, pass through its elements (A/R/G/B)
                    for (int n = 0; n < 4; n++)
                    {
                        //int shift = 0;
                        //check if new 12 bits has been processed
                        if (pixelElementIndex % 12 == 0)
                        {
                            //check if the whole process has finished
                            //we can say that it's finished when 12 zeros are added
                            if (state == State.Filling_With_Zeros && zeros == 12)
                            {
                                //apply the last pixel on the image
                                //even if only a part of its elements have been affected
                                if ((pixelElementIndex - 1) % 4 < 3)
                                {
                                    bmp.SetPixel(j, i, Color.FromArgb(A, R, G, B));
                                }

                                return bmp; //return the bitmap with the text hidden in
                            }

                            //check if all characters have been hidden
                            if (charIndex >= text.Length)
                            {
                                state = State.Filling_With_Zeros; //start adding zeros to mark the end of the text
                            }
                            else
                            {
                                charValue = text[charIndex++]; //move to the next character and process again
                            }
                        }

                        // check which pixel element has the turn to hide a bit in its LSB
                        switch (pixelElementIndex % 4)
                        {
                            case 0:
                                {
                                    if (state == State.Hiding)
                                    {
                                        /**
                                        the rightmost bit in the character will be (charValue % 2)
                                        to put this value instead of the LSB of the pixel element just add it to it
                                        recall that the LSB of the pixel element had been cleared
                                        before this operation
                                        **/
                                        A += charValue % 2;
                                        // removes the added rightmost bit of the character
                                        // such that next time we can reach the next one
                                        charValue /= 2;
                                    }
                                }
                                break;
                            case 1:
                                {
                                    if (state == State.Hiding)
                                    {
                                        R += charValue % 2;

                                        charValue /= 2;
                                    }
                                }
                                break;
                            case 2:
                                {
                                    if (state == State.Hiding)
                                    {
                                        G += charValue % 2;

                                        charValue /= 2;
                                    }
                                }
                                break;
                            case 3:
                                {
                                    if (state == State.Hiding)
                                    {
                                        B += charValue % 2;

                                        charValue /= 2;
                                    }

                                    bmp.SetPixel(j, i, Color.FromArgb(A, R, G, B));
                                }
                                break;
                        }

                        pixelElementIndex++;

                        if (state == State.Filling_With_Zeros)
                        {
                            zeros++; //increment the value of zeros until it is 12
                        }
                    }
                }
            }
            return bmp;
        }

        private string DecryptText(Bitmap bmp) //decrypting text from the image
        {
            int colorUnitIndex = 0;
            int charValue = 0;

            string extractedText = String.Empty; //text that will be extracted from the image

            //pass through the rows
            for (int i = 0; i < bmp.Height; i++)
            {
                //pass through each row
                for (int j = 0; j < bmp.Width; j++)
                {
                    Color pixel = bmp.GetPixel(j, i);

                    //for each pixel, pass through its elements (A/R/G/B)
                    for (int n = 0; n < 4; n++)
                    {
                        switch (colorUnitIndex % 4)
                        {
                            case 0:
                                {
                                    /**
                                    get the LSB from the pixel element (pixel.R % 2)
                                    then add one bit to the right of the current character
                                    this can be done by (charValue = charValue * 2)
                                    replace the added bit (which value is by default 0) with
                                    the LSB of the pixel element, simply by addition
                                    **/
                                    charValue = charValue * 2 + pixel.A % 2;
                                }
                                break;
                            case 1:
                                {
                                    charValue = charValue * 2 + pixel.R % 2;
                                }
                                break;
                            case 2:
                                {
                                    charValue = charValue * 2 + pixel.G % 2;
                                }
                                break;
                            case 3:
                                {
                                    charValue = charValue * 2 + pixel.B % 2;
                                }
                                break;
                        }

                        colorUnitIndex++;

                        //if 12 bits has been added add the current character to the result text
                        if (colorUnitIndex % 12 == 0)
                        {
                            //reverse since each time the process occurs on the right (for simplicity)
                            charValue = reverseBits(charValue);

                            //can only be 0 if it is the stop character (the 8 zeros)
                            if (charValue == 0)
                            {
                                return extractedText;
                            }

                            //convert the character value from int to char
                            char c = (char)charValue;
                            //add the current character to the result text
                            extractedText += c.ToString();
                        }
                    }
                }
            }
            return extractedText;
        }

        private int reverseBits(int n) //reversing bits
        {
            int result = 0;
            for (int i = 0; i < 12; i++)
            {
                result = result * 2 + n % 2;
                n /= 2;
            }
            return result;
        }

        private void button1_Click(object sender, EventArgs e) //open image button
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.Image = new Bitmap(openFileDialog1.FileName);
                logBox.Text = "Opened file " + openFileDialog1.FileName + "\r\n" + logBox.Text;
            }
        }
        private void button2_Click(object sender, EventArgs e) //encrypt text button
        {
            string text = textBox1.Text;
            if(text == "")
            {
                logBox.Text = "Enter text that you want to encrypt\r\n" + logBox.Text;
            }
            else
            {
                Bitmap image = new Bitmap(pictureBox1.Image);
                Bitmap result = EncryptText(text, image);
                pictureBox1.Image = result;
                logBox.Text = "Text encrypted successfully\r\n" + logBox.Text;
            }
        }

        private void button3_Click(object sender, EventArgs e) //save image button
        {
            if(saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filename = saveFileDialog1.FileName;
                pictureBox1.Image.Save(filename);
                logBox.Text = "Saved file " + filename + "\r\n" + logBox.Text;
            }
        }

        private void button4_Click(object sender, EventArgs e) //decrypt text button
        {
            string result = DecryptText(new Bitmap(pictureBox1.Image));
            if(result == "")
            {
                logBox.Text = "Image does not contain encrypted text\r\n" + logBox.Text;
            }
            else
            {
                textBox1.Text = result;
                logBox.Text = "Text decrypted successfully\r\n" + logBox.Text;
            }
        }
    }
}
