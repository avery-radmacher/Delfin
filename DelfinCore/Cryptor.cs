using System;
using System.Drawing;
using IOHandler;
using static IOHandler.IOHandler;

namespace DelfinForWindows
{
    public class CryptionResult
    {
        public bool Success { get; internal set; }
        public string ErrMsg { get; internal set; }
        public string ErrDescription { get; internal set; }
    }

    public class DecryptorIO
    {
        internal ILoader<Bitmap> GetImage { get; }

        internal IHandler<byte[]> SaveFile { get; }
    }

    public class EncryptorIO
    {
        internal ILoader<Bitmap> GetImage { get; }

        internal ILoader<byte[]> GetFile { get; }

        internal IHandler<Bitmap> SaveImage { get; }
    }

    public class Cryptor
    {
        // string imgName, string password
        public static void Decrypt(string imgName, string password, string saveFilename, Action<CryptionResult> ProcessResult)
        {
            bool quit = false;
            void HandleError(string err, string errDesc)
            {
                quit = true;
                ProcessResult(new CryptionResult() { Success = false, ErrMsg = err, ErrDescription = errDesc });
            }

            long pixScan = 0, byteScan = -1;
            int color;
            byte[] pairBuffer = new byte[6];
            int population = 0;
            int datum;
            Header header = new();
            byte[] fileBuffer = null;
            Bitmap img = null;
            Cipher cipher = password.Equals("") ? null : new OldCipher(password);

            // load image or quit on failure
            LoadImage(imgName, bitmap => img = bitmap, HandleError);
            if (quit) return;

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
                            CryptionResult result = new()
                            {
                                Success = false,
                                ErrMsg = "header could not be read or password is wrong",
                                ErrDescription = "header could not be read or password is wrong",
                            };
                            ProcessResult(result);
                            return;
                        }

                        // if header construction is complete, verify it and allocate buffer
                        if (header.IsComplete)
                        {
                            // ensure file fits per completed header
                            if (img.Height * img.Width * 3 / 4 < header.FileSize + header.HeaderSize)
                            {
                                CryptionResult result = new()
                                {
                                    Success = false,
                                    ErrMsg = "file is corrupt or password is wrong",
                                    ErrDescription = "file is corrupt or password is wrong",
                                };
                                ProcessResult(result);
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
                                    CryptionResult result = new()
                                    {
                                        Success = false,
                                        ErrMsg = "file is corrupt or password is wrong",
                                        ErrDescription = "file is corrupt or password is wrong",
                                    };
                                    ProcessResult(result);
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

            // prompt user to save file
            SaveFile(saveFilename, fileBuffer, () => ProcessResult(new() { Success = true }), HandleError);
        }

        // string imgName, string fileName, string password
        public static void Encrypt(string imgName, string filename, string password, string saveFilename, Action<CryptionResult> ProcessResult)
        {
            bool quit = false;
            void HandleError(string err, string errDesc)
            {
                quit = true;
                ProcessResult(new CryptionResult() { Success = false, ErrMsg = err, ErrDescription = errDesc });
            }

            long pixScan = 0, byteScan, fileSize = 0;
            int pixX, pixY;
            int color, A, R, G, B;
            byte[] pairBuffer = new byte[6];
            int population = 0;
            int datum;
            Bitmap img = null;
            Header header = new();
            byte[] headerBuffer;
            byte[] fileBuffer = null;

            // load the zip file or quit nicely on failure
            LoadFile(filename, (buffer, size) => { fileBuffer = buffer; fileSize = size; }, HandleError);
            if (quit) return;

            // load the image or quit nicely on failure
            LoadImage(imgName, bitmap => img = bitmap, HandleError);
            if (quit) return;

            // initiailze header and related items
            header.FileSize = (int)fileSize;
            headerBuffer = header.ToBuffer();
            byteScan = 0 - header.HeaderSize;

            // verify image is large enough to hold the file or quit
            if (img.Height * img.Width * 3 / 4 < fileSize + header.HeaderSize)
            {
                CryptionResult result = new()
                {
                    Success = false,
                    ErrMsg = "image too small",
                };
                ProcessResult(result);
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

            // prompt user to save file
            SaveImage(saveFilename, img, () => ProcessResult(new() { Success = true }), HandleError);
        }
    }
}
