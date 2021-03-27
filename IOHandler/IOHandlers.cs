using System;
using System.Drawing;
using System.IO;

namespace IOHandler
{
    public class FileSystemBitmapLoader : ILoader<Bitmap>
    {
        public ErrorHandler HandleError { get; }

        public string Filename { get; set; }

        public FileSystemBitmapLoader(ErrorHandler errorHandler = null)
        {
            HandleError = errorHandler ?? IWithErrorHandler.DefaultErrorHandler;
        }

        public Bitmap Load()
        {
            FileStream reader;
            try
            {
                reader = new FileStream(Filename, FileMode.Open);
            }
            catch (Exception ex) when
                (ex is ArgumentException ||
                ex is ArgumentNullException ||
                ex is DirectoryNotFoundException ||
                ex is NotSupportedException ||
                ex is PathTooLongException)
            {
                // path is null, empty, or invalid due to length, drive, or characters
                HandleError("invalid path name", $"The path\r\n{Filename}\r\nis not a valid path. Please specify a valid path.");
                return null;
            }
            catch (FileNotFoundException)
            {
                HandleError("file not found", $"The file\r\n{Filename}\r\nwas not found.");
                return null;
            }
            catch (IOException)
            {
                HandleError("unexpected I/O error", "An I/O error occurred while opening the file.");
                return null;
            }
            catch (Exception ex) when (ex is System.Security.SecurityException || ex is UnauthorizedAccessException)
            {
                HandleError("unauthorized access", $"You don't have permission to access the file:\r\n{Filename}");
                return null;
            }

            Bitmap img = null;
            try
            {
                img = new Bitmap(reader);
            }
            catch (ArgumentException)
            {
                HandleError("invalid image file", $"The file\r\n{Filename}\r\ncould not be interpreted as a valid image.");
            }

            reader.Dispose();

            return img;
        }
    }

    public class FileSystemByteArrayLoader : ILoader<byte[]>
    {
        public ErrorHandler HandleError { get; }

        public string Filename { get; set; }

        public string FileExtension { get; }

        public FileSystemByteArrayLoader(string fileExtension, ErrorHandler errorHandler = null)
        {
            HandleError = errorHandler ?? IWithErrorHandler.DefaultErrorHandler;
            FileExtension = fileExtension;
        }

        public byte[] Load()
        {
            if (!Filename.EndsWith(FileExtension))
            {
                HandleError("Wrong file type", $"Expected {Filename} to end with {FileExtension}");
                return null;
            }

            long fileSize;
            byte[] fileBuffer;
            try
            {
                fileSize = new FileInfo(Filename).Length;
                fileBuffer = File.ReadAllBytes(Filename);
            }
            catch (Exception ex) when
                (ex is ArgumentException ||
                ex is ArgumentNullException ||
                ex is PathTooLongException ||
                ex is DirectoryNotFoundException ||
                ex is NotSupportedException)
            {
                // path is null, empty, or invalid due to length, drive, or characters
                HandleError("invalid path name", $"The path\r\n{Filename}\r\nis not a valid path. Please specify a valid path.");
                return null;
            }
            catch (FileNotFoundException)
            {
                HandleError("file not found", $"The file\r\n{Filename}\r\nwas not found.");
                return null;
            }
            catch (IOException)
            {
                HandleError("unexpected I/O error", "An I/O error occurred while opening the file.");
                return null;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
            {
                HandleError("unauthorized access", $"You don't have permission to access the file:\r\n{Filename}");
                return null;
            }

            if (fileBuffer.LongLength != fileSize) throw new ApplicationException("Buffers can be other sizes, apparently.");
            return fileBuffer;
        }
    }

    public class FileSystemBitmapHandler : IHandler<Bitmap>
    {
        public ErrorHandler HandleError { get; }

        public string Filename { get; set; }

        public FileSystemBitmapHandler(ErrorHandler errorHandler = null)
        {
            HandleError = errorHandler ?? IWithErrorHandler.DefaultErrorHandler;
        }

        public void Handle(Bitmap item)
        {
            if (!Filename.EndsWith(".png"))
            {
                HandleError("Wrong file type", $"Expected {Filename} to be a .png file.");
                return;
            }

            FileStream writer;
            try
            {
                writer = new FileStream(Filename, FileMode.Create);
            }
            catch (Exception ex) when
                (ex is ArgumentException ||
                ex is NotSupportedException ||
                ex is ArgumentNullException ||
                ex is DirectoryNotFoundException ||
                ex is PathTooLongException)
            {
                // path is null, empty, or invalid due to length, drive, or characters
                HandleError("invalid path name", $"The path\r\n{Filename}\r\nis not a valid path. Please specify a valid path.");
                return;
            }
            catch (IOException)
            {
                HandleError("unexpected I/O error", "An I/O error occurred while using the file.");
                return;
            }
            catch (System.Security.SecurityException)
            {
                HandleError("unauthorized access", $"You don't have permission to access the file:\r\n{Filename}");
                return;
            }

            item.Save(writer, System.Drawing.Imaging.ImageFormat.Png);
            writer.Dispose();
        }
    }

    public class FileSystemByteArrayHandler : IHandler<byte[]>
    {
        public ErrorHandler HandleError { get; }

        public string Filename { get; set; }

        public string FileExtension { get; }

        public FileSystemByteArrayHandler(string fileExtension, ErrorHandler errorHandler = null)
        {
            HandleError = errorHandler ?? IWithErrorHandler.DefaultErrorHandler;
            FileExtension = fileExtension;
        }

        public void Handle(byte[] item)
        {
            if (!Filename.EndsWith(FileExtension))
            {
                HandleError("Wrong file type", $"Expected {Filename} to end with {FileExtension}");
                return;
            }

            BinaryWriter writer;
            try
            {
                writer = new BinaryWriter(File.Open(Filename, FileMode.Create));
                writer.Write(item);
            }
            catch (Exception ex) when
                (ex is ArgumentException ||
                ex is ArgumentNullException ||
                ex is PathTooLongException ||
                ex is DirectoryNotFoundException ||
                ex is NotSupportedException)
            {
                // path is null, empty, or invalid due to length, drive, or characters
                HandleError("invalid path name", $"The path\r\n{Filename}\r\nis not a valid path. Please specify a valid path.");
                return;
            }
            catch (IOException)
            {
                HandleError("unexpected I/O error", "An I/O error occurred while using the file.");
                return;
            }
            catch (UnauthorizedAccessException)
            {
                HandleError($"You don't have permission to access the file:\r\n{Filename}", "unauthorized access");
                return;
            }

            writer.Flush();
            writer.Close();
            writer.Dispose();
        }
    }
}
