using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DelfinForWindows
{
    public class DelfinWorker
    {
        // string imgName, string password
        public static void Decrypt(object args)
        {
            string errMsg;
            bool success;

            // set flags to incomplete states (so they will hold these values if this thread is aborted)
            errMsg = "incomplete operation";
            success = false;

            // unpack args (failsafe, but this should never fail)
            Tuple<string, string> input;
            try
            {
                input = (Tuple<string, string>)args;
            }
            catch (InvalidCastException)
            {
                errMsg = "fatal! Improperly packed arguments";
                return;
            }
            string imgName = input.Item1;
            string password = input.Item2;

            long pixScan = 0, byteScan = -1;
            int color;
            byte[] pairBuffer = new byte[6];
            int population = 0;
            int datum;
            Header header = new Header();
            byte[] fileBuffer = null;
            Bitmap img;
            Cipher cipher = password.Equals("") ? null : new OldCipher(password);

            // load image or quit on failure
            {
                FileStream reader;
                try
                {
                    reader = new FileStream(imgName, FileMode.Open);
                }
                catch (Exception ex) when
                    (ex is ArgumentException ||
                    ex is ArgumentNullException ||
                    ex is DirectoryNotFoundException ||
                    ex is NotSupportedException ||
                    ex is PathTooLongException)
                {
                    // path is null, empty, or invalid due to length, drive, or characters
                    MessageBox.Show("The path\r\n" + imgName + "\r\nis not a valid path. Please specify a valid path.", "Invalid path name");
                    errMsg = "invalid path name";
                    return;
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("The file\r\n" + imgName + "\r\nwas not found.", "File not found");
                    errMsg = "file not found";
                    return;
                }
                catch (IOException)
                {
                    MessageBox.Show("An I/O error occurred while opening the file.", "Unexpected I/O error");
                    errMsg = "unexpected I/O error";
                    return;
                }
                catch (Exception ex) when (ex is System.Security.SecurityException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show("You don't have permission to access the file:\r\n" + imgName, "Unauthorized access");
                    errMsg = "unauthorized access";
                    return;
                }
                try
                {
                    img = new Bitmap(reader);
                }
                catch (ArgumentException)
                {
                    MessageBox.Show("The file\r\n" + imgName + "\r\ncould not be interpreted as a valid image.", "Invalid image");
                    errMsg = "invalid image file";
                    return;
                }
                reader.Close();
                reader.Dispose();
            }

            // main data-processing loop
            while (!header.IsComplete || byteScan < header.FileSize)
            {
                // read a pixel's worth of data from the image
                color = img.GetPixel((int)(pixScan % img.Width), (int)(pixScan / img.Width)).ToArgb();
                pairBuffer[population++] = (byte)(color >> 16 & 3);
                pairBuffer[population++] = (byte)(color >> 8 & 3);
                pairBuffer[population++] = (byte)(color & 3);
                pixScan++;

                // write a byte, if we have enough data in the buffer
                if (population >= 4)
                {
                    // retrieve byte from buffer and shift values
                    datum = (pairBuffer[0] << 6) | (pairBuffer[1] << 4) | (pairBuffer[2] << 2) | pairBuffer[3];
                    pairBuffer[0] = pairBuffer[4];
                    pairBuffer[1] = pairBuffer[5];
                    population -= 4;

                    // determine whether byte is part of header or part of file
                    if (byteScan < 0)
                    {
                        // construct header
                        if (cipher != null)
                        {
                            datum ^= cipher.GetByte();
                        }
                        header.AddByte((byte)datum);

                        if (header.IsUnsupported)
                        {
                            errMsg = "header could not be read or password is wrong";
                            return;
                        }

                        // if header construction is complete, verify it and allocate buffer
                        if (header.IsComplete)
                        {
                            // ensure file fits per completed header
                            if (img.Height * img.Width * 3 / 4 < header.FileSize + header.HeaderSize)
                            {
                                errMsg = "file is corrupt or password is wrong";
                                return;
                            }
                            else
                            {
                                // It seems that occasionally the FileSize can overflow (I believe on invalid password
                                // that slyly goes unnoticed) and throw a rare exception here. Just quit if so.
                                try
                                {
                                    fileBuffer = new byte[header.FileSize];
                                }
                                catch (OverflowException)
                                {
                                    errMsg = "file is corrupt or password is wrong";
                                    return;
                                }

                                byteScan = 0; // lets loop know we are no longer reading the header
                            }

                            // for HV1, replace OldCipher with Cipher if non-null
                            if (header.HeaderVersion == 1)
                            {
                                cipher = cipher == null ? null : new Cipher(password);
                            }
                        }
                    }
                    else
                    {
                        // add datum to buffer
                        fileBuffer[byteScan++] = (byte)datum;
                    }
                }
            }

            // Decrypt file using cipher, if there was a password
            if (cipher != null)
            {
                for (int i = 0; i < fileBuffer.Length; i++)
                {
                    fileBuffer[i] ^= cipher.GetByte();
                }
            }
        }

        // string imgName, string fileName, string password
        public static void Encrypt(object args)
        {
            string errMsg;
            bool success;

            // set flags to incomplete states (so they will hold these values if this thread is aborted)
            errMsg = "incomplete operation";
            success = false;

            // unpack args (failsafe, but this should never fail)
            Tuple<string, string, string> input;
            try
            {
                input = (Tuple<string, string, string>)args;
            }
            catch (InvalidCastException)
            {
                errMsg = "fatal! Improperly packed arguments";
                return;
            }
            string imgName = input.Item1;
            string fileName = input.Item2;
            string password = input.Item3;

            long pixScan = 0, byteScan, fileSize;
            int pixX, pixY;
            int color, A, R, G, B;
            byte[] pairBuffer = new byte[6];
            int population = 0;
            int datum;
            Bitmap img;
            Header header = new Header();
            byte[] headerBuffer;
            byte[] fileBuffer;

            // load the zip file or quit nicely on failure
            try
            {
                fileSize = new FileInfo(fileName).Length;
                fileBuffer = File.ReadAllBytes(fileName);
            }
            catch (Exception ex) when
                (ex is ArgumentException ||
                ex is ArgumentNullException ||
                ex is PathTooLongException ||
                ex is DirectoryNotFoundException ||
                ex is NotSupportedException)
            {
                // path is null, empty, or invalid due to length, drive, or characters
                MessageBox.Show("The path\r\n" + fileName + "\r\nis not a valid path. Please specify a valid path.", "Invalid path name");
                errMsg = "invalid path name";
                return;
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("The file\r\n" + fileName + "\r\nwas not found.", "File not found");
                errMsg = "file not found";
                return;
            }
            catch (IOException)
            {
                MessageBox.Show("An I/O error occurred while opening the file.", "Unexpected I/O error");
                errMsg = "unexpected I/O error";
                return;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
            {
                MessageBox.Show("You don't have permission to access the file:\r\n" + fileName, "Unauthorized access");
                errMsg = "unauthorized access";
                return;
            }

            // load the image or quit nicely on failure
            {
                FileStream reader;
                try
                {
                    reader = new FileStream(imgName, FileMode.Open);
                }
                catch (Exception ex) when
                    (ex is ArgumentException ||
                    ex is ArgumentNullException ||
                    ex is DirectoryNotFoundException ||
                    ex is NotSupportedException ||
                    ex is PathTooLongException)
                {
                    // path is null, empty, or invalid due to length, drive, or characters
                    MessageBox.Show("The path\r\n" + imgName + "\r\nis not a valid path. Please specify a valid path.", "Invalid path name");
                    errMsg = "invalid path name";
                    return;
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("The file\r\n" + imgName + "\r\nwas not found.", "File not found");
                    errMsg = "file not found";
                    return;
                }
                catch (IOException)
                {
                    MessageBox.Show("An I/O error occurred while opening the file.", "Unexpected I/O error");
                    errMsg = "unexpected I/O error";
                    return;
                }
                catch (Exception ex) when (ex is System.Security.SecurityException || ex is UnauthorizedAccessException)
                {
                    MessageBox.Show("You don't have permission to access the file:\r\n" + imgName, "Unauthorized access");
                    errMsg = "unauthorized access";
                    return;
                }
                try
                {
                    img = new Bitmap(reader);
                }
                catch (ArgumentException)
                {
                    MessageBox.Show("The file\r\n" + imgName + "\r\ncould not be interpreted as a valid image.", "Invalid image");
                    errMsg = "invalid image file";
                    return;
                }
                reader.Close();
                reader.Dispose();
            }

            // initiailze header and related items
            header.FileSize = (int)fileSize;
            headerBuffer = header.ToBuffer();
            byteScan = 0 - header.HeaderSize;

            // verify image is large enough to hold the file or quit
            if (img.Height * img.Width * 3 / 4 < fileSize + header.HeaderSize)
            {
                errMsg = "image too small";
                return;
            }

            // Encrypt file and header using cipher, if there was a password
            if (!password.Equals(""))
            {
                Cipher cipher = new OldCipher(password);
                for (int i = 0; i < headerBuffer.Length; i++)
                {
                    headerBuffer[i] ^= cipher.GetByte();
                }
                cipher = new Cipher(password); // HV1 encrypts the header with OldCipher and the file with Cipher (this ensures backwards compatibility)
                for (int i = 0; i < fileBuffer.Length; i++)
                {
                    fileBuffer[i] ^= cipher.GetByte();
                }
            }

            // main data-processing loop
            while (byteScan < fileSize || population != 0)
            {
                // make sure we have data to write; if not, get some more
                if (population < 3)
                {
                    // if there's more data to get, get it; if not, create fake data
                    if (byteScan < fileSize)
                    {
                        if (byteScan < 0)
                        {
                            // read a byte from the header
                            datum = headerBuffer[byteScan + header.HeaderSize];
                        }
                        else
                        {
                            // read a byte from the file
                            datum = fileBuffer[byteScan];
                        }

                        byteScan++;

                        // break it apart and put it in the buffer
                        for (int i = 3; i >= 0; i--)
                        {
                            pairBuffer[population++] = (byte)((datum >> (2 * i)) & 3);
                        }
                    }
                    else
                    {
                        // TODO: currently fake data is zeros, can replace later to be original pixel values
                        while (population < 3)
                        {
                            pairBuffer[population++] = 0;
                        }
                    }
                }

                // write a pixel's worth of data to the image
                pixX = (int)(pixScan % img.Width);
                pixY = (int)(pixScan / img.Width);
                color = img.GetPixel(pixX, pixY).ToArgb();
                A = color >> 24 & 255;
                R = color >> 16 & 252 | pairBuffer[0];
                G = color >> 8 & 252 | pairBuffer[1];
                B = color & 252 | pairBuffer[2];
                img.SetPixel(pixX, pixY, Color.FromArgb(A, R, G, B));
                pixScan++;

                // move the buffer values
                for (int i = 0; i < 3; i++)
                {
                    pairBuffer[i] = pairBuffer[i + 3];
                }

                population -= 3;
            }
        }
    }
}
