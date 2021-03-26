using System;
using System.Drawing;
using System.IO;

namespace DelfinForWindows
{

    public class IOHandler
    {
        public delegate void ErrorHandler(string errMsg, string errDescription);
        public delegate void LoadImageHandler(Bitmap bitmap);
        public delegate void LoadFileHandler(byte[] filebuffer, long fileSize);
        public delegate void SaveImageHandler();
        public delegate void SaveFileHandler();

        public static void LoadImage(string imgName, LoadImageHandler onLoadImage, ErrorHandler onError)
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
                onError("invalid path name", $"The path\r\n{imgName}\r\nis not a valid path. Please specify a valid path.");
                return;
            }
            catch (FileNotFoundException)
            {
                onError("file not found", $"The file\r\n{imgName}\r\nwas not found.");
                return;
            }
            catch (IOException)
            {
                onError("unexpected I/O error", "An I/O error occurred while opening the file.");
                return;
            }
            catch (Exception ex) when (ex is System.Security.SecurityException || ex is UnauthorizedAccessException)
            {
                onError("unauthorized access", $"You don't have permission to access the file:\r\n{imgName}");
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
                onError("invalid image file", $"The file\r\n{imgName}\r\ncould not be interpreted as a valid image.");
            }

            reader.Close();
            reader.Dispose();

            if (hasImage)
            {
                onLoadImage(img);
            }
        }

        public static void LoadFile(string filename, LoadFileHandler onLoadFile, ErrorHandler onError)
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
                onError("invalid path name", $"The path\r\n{filename}\r\nis not a valid path. Please specify a valid path.");
                return;
            }
            catch (FileNotFoundException)
            {
                onError("file not found", $"The file\r\n{filename}\r\nwas not found.");
                return;
            }
            catch (IOException)
            {
                onError("unexpected I/O error", "An I/O error occurred while opening the file.");
                return;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
            {
                onError("unauthorized access", $"You don't have permission to access the file:\r\n{filename}");
                return;
            }

            onLoadFile(fileBuffer, fileSize);
        }

        public static void SaveImage(string filename, Bitmap image, SaveImageHandler onSaveImage, ErrorHandler onError)
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
                    onError("invalid path name", $"The path\r\n{filename}\r\nis not a valid path. Please specify a valid path.");
                    return;
                }
                catch (IOException)
                {
                    onError("unexpected I/O error", "An I/O error occurred while using the file.");
                    return;
                }
                catch (System.Security.SecurityException)
                {
                    onError("unauthorized access", $"You don't have permission to access the file:\r\n{filename}");
                    return;
                }

                image.Save(writer, System.Drawing.Imaging.ImageFormat.Png);
                writer.Close();
                writer.Dispose();
                onSaveImage();
                return;
            }
            else
            {
                onError("canceled operation", "The file was not saved.");
                return;
            }
        }

        public static void SaveFile(string filename, byte[] fileBuffer, SaveFileHandler onSaveFile, ErrorHandler onError)
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
                    onError("invalid path name", $"The path\r\n{filename}\r\nis not a valid path. Please specify a valid path.");
                    return;
                }
                catch (IOException)
                {
                    onError("unexpected I/O error", "An I/O error occurred while using the file.");
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    onError($"You don't have permission to access the file:\r\n{filename}", "unauthorized access");
                    return;
                }

                writer.Flush();
                writer.Close();
                writer.Dispose();
                onSaveFile();
                return;
            }
            else
            {
                onError("canceled operation", "The file was not saved.");
                return;
            }
        }
    }
}