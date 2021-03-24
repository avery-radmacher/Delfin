using System;
using System.Drawing;
using System.IO;

namespace DelfinForWindows
{
    public class CryptionResult
    {
        public bool Success { get; internal set; }
        public string ErrMsg { get; internal set; }
        public string ErrDescription { get; internal set; }
    }

    public class IOHandler
    {
        public delegate void ErrorHandler(string errMsg, string errDescription);
        public delegate void LoadImageHandler(Bitmap bitmap);
        public delegate void LoadFileHandler(byte[] filebuffer, long fileSize);
        public delegate void SaveImageHandler();
        public delegate void SaveFileHandler();

        public ErrorHandler OnError { get; }

        public LoadImageHandler OnLoadImage { get; }

        public LoadFileHandler OnLoadFile { get; }

        public SaveImageHandler OnSaveImage { get; }

        public SaveFileHandler OnSaveFile { get; }

        public void LoadImage(string imgName)
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
                OnError("invalid path name", $"The path\r\n{imgName}\r\nis not a valid path. Please specify a valid path.");
                return;
            }
            catch (FileNotFoundException)
            {
                OnError("file not found", $"The file\r\n{imgName}\r\nwas not found.");
                return;
            }
            catch (IOException)
            {
                OnError("unexpected I/O error", "An I/O error occurred while opening the file.");
                return;
            }
            catch (Exception ex) when (ex is System.Security.SecurityException || ex is UnauthorizedAccessException)
            {
                OnError("unauthorized access", $"You don't have permission to access the file:\r\n{imgName}");
                return;
            }

            Bitmap img = null;
            bool hasImage = false;
            try
            {
                img = new Bitmap(reader);
                hasImage = true;
            }
            catch (ArgumentException)
            {
                OnError("invalid image file", $"The file\r\n{imgName}\r\ncould not be interpreted as a valid image.");
            }

            reader.Close();
            reader.Dispose();

            if (hasImage)
            {
                OnLoadImage(img);
            }
        }

        public void LoadFile(string filename)
        {
            long fileSize;
            byte[] fileBuffer;
            try
            {
                fileSize = new FileInfo(filename).Length;
                fileBuffer = File.ReadAllBytes(filename);
            }
            catch (Exception ex) when
                (ex is ArgumentException ||
                ex is ArgumentNullException ||
                ex is PathTooLongException ||
                ex is DirectoryNotFoundException ||
                ex is NotSupportedException)
            {
                // path is null, empty, or invalid due to length, drive, or characters
                OnError("invalid path name", $"The path\r\n{filename}\r\nis not a valid path. Please specify a valid path.");
                return;
            }
            catch (FileNotFoundException)
            {
                OnError("file not found", $"The file\r\n{filename}\r\nwas not found.");
                return;
            }
            catch (IOException)
            {
                OnError("unexpected I/O error", "An I/O error occurred while opening the file.");
                return;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
            {
                OnError("unauthorized access", $"You don't have permission to access the file:\r\n{filename}");
                return;
            }

            OnLoadFile(fileBuffer, fileSize);
        }

        public void SaveImage(string filename, Bitmap image)
        {
            if (filename.EndsWith(".png"))
            {
                FileStream writer;
                try
                {
                    writer = new FileStream(filename, FileMode.Create);
                }
                catch (Exception ex) when
                    (ex is ArgumentException ||
                    ex is NotSupportedException ||
                    ex is ArgumentNullException ||
                    ex is DirectoryNotFoundException ||
                    ex is PathTooLongException)
                {
                    // path is null, empty, or invalid due to length, drive, or characters
                    OnError("invalid path name", $"The path\r\n{filename}\r\nis not a valid path. Please specify a valid path.");
                    return;
                }
                catch (IOException)
                {
                    OnError("unexpected I/O error", "An I/O error occurred while using the file.");
                    return;
                }
                catch (System.Security.SecurityException)
                {
                    OnError("unauthorized access", $"You don't have permission to access the file:\r\n{filename}");
                    return;
                }

                image.Save(writer, System.Drawing.Imaging.ImageFormat.Png);
                writer.Close();
                writer.Dispose();
                OnSaveImage();
                return;
            }
            else
            {
                OnError("canceled operation", "The file was not saved.");
                return;
            }
        }

        public void SaveFile(string filename, byte[] fileBuffer)
        {
            if (filename.EndsWith(".zip"))
            {
                BinaryWriter writer;
                try
                {
                    writer = new BinaryWriter(File.Open(filename, FileMode.Create));
                    writer.Write(fileBuffer);
                }
                catch (Exception ex) when
                    (ex is ArgumentException ||
                    ex is ArgumentNullException ||
                    ex is PathTooLongException ||
                    ex is DirectoryNotFoundException ||
                    ex is NotSupportedException)
                {
                    // path is null, empty, or invalid due to length, drive, or characters
                    OnError("invalid path name", $"The path\r\n{filename}\r\nis not a valid path. Please specify a valid path.");
                    return;
                }
                catch (IOException)
                {
                    OnError("unexpected I/O error", "An I/O error occurred while using the file.");
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    OnError($"You don't have permission to access the file:\r\n{filename}", "unauthorized access");
                    return;
                }

                writer.Flush();
                writer.Close();
                writer.Dispose();
                OnSaveFile();
                return;
            }
            else
            {
                OnError("canceled operation", "The file was not saved.");
                return;
            }
        }
    }

    public class Cryptor
    {
        // string imgName, string password
        public static void Decrypt(string imgName, string password, string saveFilename, Action<CryptionResult> ProcessResult)
        {
            long pixScan = 0, byteScan = -1;
            int color;
            byte[] pairBuffer = new byte[6];
            int population = 0;
            int datum;
            Header header = new();
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
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "invalid path name",
                        ErrDescription = $"The path\r\n{imgName}\r\nis not a valid path. Please specify a valid path.",
                    };
                    ProcessResult(result);
                    return;
                }
                catch (FileNotFoundException)
                {
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "file not found",
                        ErrDescription = $"The file\r\n{imgName}\r\nwas not found.",
                    };
                    ProcessResult(result);
                    return;
                }
                catch (IOException)
                {
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "An I/O error occurred while opening the file.",
                        ErrDescription = "unexpected I/O error",
                    };
                    ProcessResult(result);
                    return;
                }
                catch (Exception ex) when (ex is System.Security.SecurityException || ex is UnauthorizedAccessException)
                {
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "unauthorized access",
                        ErrDescription = $"You don't have permission to access the file:\r\n{imgName}",
                    };
                    ProcessResult(result);
                    return;
                }
                try
                {
                    img = new Bitmap(reader);
                }
                catch (ArgumentException)
                {
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "invalid image file",
                        ErrDescription = $"The file\r\n{imgName}\r\ncould not be interpreted as a valid image.",
                    };
                    ProcessResult(result);
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
            if (saveFilename.EndsWith(".zip"))
            {
                BinaryWriter writer;
                try
                {
                    writer = new BinaryWriter(File.Open(saveFilename, FileMode.Create));
                    writer.Write(fileBuffer);
                }
                catch (Exception ex) when
                    (ex is ArgumentException ||
                    ex is ArgumentNullException ||
                    ex is PathTooLongException ||
                    ex is DirectoryNotFoundException ||
                    ex is NotSupportedException)
                {
                    // path is null, empty, or invalid due to length, drive, or characters
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "invalid path name",
                        ErrDescription = $"The path\r\n{saveFilename}\r\nis not a valid path. Please specify a valid path.",
                    };
                    ProcessResult(result);
                    return;
                }
                catch (IOException)
                {
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "unexpected I/O error",
                        ErrDescription = "An I/O error occurred while using the file.",
                    };
                    ProcessResult(result);
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = $"You don't have permission to access the file:\r\n{saveFilename}",
                        ErrDescription = "unauthorized access",
                    };
                    ProcessResult(result);
                    return;
                }

                writer.Flush();
                writer.Close();
                writer.Dispose();
                ProcessResult(new CryptionResult() { Success = true });
                return;
            }
            else
            {
                CryptionResult result = new()
                {
                    Success = false,
                    ErrMsg = "canceled operation",
                    ErrDescription = "The file was not saved.",
                };
                ProcessResult(result);
                return;
            }
        }

        // string imgName, string fileName, string password
        public static void Encrypt(string imgName, string filename, string password, string saveFilename, Action<CryptionResult> ProcessResult)
        {
            long pixScan = 0, byteScan, fileSize;
            int pixX, pixY;
            int color, A, R, G, B;
            byte[] pairBuffer = new byte[6];
            int population = 0;
            int datum;
            Bitmap img;
            Header header = new();
            byte[] headerBuffer;
            byte[] fileBuffer;

            // load the zip file or quit nicely on failure
            try
            {
                fileSize = new FileInfo(filename).Length;
                fileBuffer = File.ReadAllBytes(filename);
            }
            catch (Exception ex) when
                (ex is ArgumentException ||
                ex is ArgumentNullException ||
                ex is PathTooLongException ||
                ex is DirectoryNotFoundException ||
                ex is NotSupportedException)
            {
                // path is null, empty, or invalid due to length, drive, or characters
                CryptionResult result = new()
                {
                    Success = false,
                    ErrMsg = "invalid path name",
                    ErrDescription = $"The path\r\n{filename}\r\nis not a valid path. Please specify a valid path.",
                };
                ProcessResult(result);
                return;
            }
            catch (FileNotFoundException)
            {
                CryptionResult result = new()
                {
                    Success = false,
                    ErrMsg = "file not found",
                    ErrDescription = $"The file\r\n{filename}\r\nwas not found.",
                };
                ProcessResult(result);
                return;
            }
            catch (IOException)
            {
                CryptionResult result = new()
                {
                    Success = false,
                    ErrMsg = "unexpected I/O error",
                    ErrDescription = "An I/O error occurred while opening the file.",
                };
                ProcessResult(result);
                return;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
            {
                CryptionResult result = new()
                {
                    Success = false,
                    ErrMsg = "unauthorized access",
                    ErrDescription = $"You don't have permission to access the file:\r\n{filename}",
                };
                ProcessResult(result);
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
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "invalid path name",
                        ErrDescription = $"The path\r\n{imgName}\r\nis not a valid path. Please specify a valid path.",
                    };
                    ProcessResult(result);
                    return;
                }
                catch (FileNotFoundException)
                {
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "file not found",
                        ErrDescription = $"The file\r\n{imgName}\r\nwas not found.",
                    };
                    ProcessResult(result);
                    return;
                }
                catch (IOException)
                {
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "unexpected I/O error",
                        ErrDescription = "An I/O error occurred while opening the file.",
                    };
                    ProcessResult(result);
                    return;
                }
                catch (Exception ex) when (ex is System.Security.SecurityException || ex is UnauthorizedAccessException)
                {
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "unauthorized access",
                        ErrDescription = $"You don't have permission to access the file:\r\n{imgName}",
                    };
                    ProcessResult(result);
                    return;
                }
                try
                {
                    img = new Bitmap(reader);
                }
                catch (ArgumentException)
                {
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "invalid image file",
                        ErrDescription = $"The file\r\n{imgName}\r\ncould not be interpreted as a valid image.",
                    };
                    ProcessResult(result);
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
            if (saveFilename.EndsWith(".png"))
            {
                FileStream writer;
                try
                {
                    writer = new FileStream(saveFilename, FileMode.Create);
                }
                catch (Exception ex) when
                    (ex is ArgumentException ||
                    ex is NotSupportedException ||
                    ex is ArgumentNullException ||
                    ex is DirectoryNotFoundException ||
                    ex is PathTooLongException)
                {
                    // path is null, empty, or invalid due to length, drive, or characters
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "invalid path name",
                        ErrDescription = $"The path\r\n{saveFilename}\r\nis not a valid path. Please specify a valid path.",
                    };
                    ProcessResult(result);
                    return;
                }
                catch (IOException)
                {
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "unexpected I/O error",
                        ErrDescription = "An I/O error occurred while using the file.",
                    };
                    ProcessResult(result);
                    return;
                }
                catch (System.Security.SecurityException)
                {
                    CryptionResult result = new()
                    {
                        Success = false,
                        ErrMsg = "unauthorized access",
                        ErrDescription = $"You don't have permission to access the file:\r\n{saveFilename}",
                    };
                    ProcessResult(result);
                    return;
                }

                img.Save(writer, System.Drawing.Imaging.ImageFormat.Png);
                writer.Close();
                writer.Dispose();
                ProcessResult(new CryptionResult() { Success = true });
                return;
            }
            else
            {
                CryptionResult result = new()
                {
                    Success = false,
                    ErrMsg = "canceled operation",
                    ErrDescription = "The file was not saved.",
                };
                ProcessResult(result);
                return;
            }
        }
    }
}
