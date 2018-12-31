// Author Avery Radmacher 201812302239
// Project Delfin for Windows

using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DelfinForWindows
{
    enum MODE
    {
        OPEN,
        ENCRYPT,
        DECRYPT,
    }

    public partial class Form_main : Form
    {
        static string VERSION = "0.4";
        // TODO allow symbols !@#$%^&*()-_=+[{]}\\|;:'\",<.>/? in password field
        // TODO multi-thread big tasks and allow for cancellation

        MODE mode;
        bool hasImage = false, hasZip = false;
        string errMsg;

        private Button button_encrypt;
        private Button button_decrypt;
        private Button button_selectImage;
        private Button button_selectZip;
        private Button button_execute;
        private Button button_cancel;
        private TextBox textBox_info;
        private TextBox textBox_password;
        private TextBox textBox_feed;
        private Label label_password;
        private PictureBox pictureBox_delfin;
        private OpenFileDialog openFileDialog_image;
        private OpenFileDialog openFileDialog_zip;
        private SaveFileDialog saveFileDialog_image;
        private SaveFileDialog saveFileDialog_zip;

        #region info texts
        private static string cancelDecryptionInfo = "Cancel the pending decryption.";
        private static string cancelEncryptionInfo = "Cancel the pending encryption.";
        private static string decryptionInfo = "Extract the compressed files from an image.";
        private static string encryptionInfo = "Encrypt a compressed file into an image.";
        private static string feedInfo = "A description of recent actions and events will appear in the feed.";
        private static string mainDecryptInfo = "Choose an image to decrypt and specify a password for access. Leave password blank if there is no password. When you are ready, click run to select a save destination.";
        private static string mainEncryptInfo = "Choose an image and a .zip file to encrypt in the image, optionally specifying a password for extra security. When you are ready, click run to select a save destination.";
        private static string mainWelcomeInfo = "To continue, select an option from the right.\r\n\r\nMouse over an option to learn more.";
        private static string passwordInfo = "Passwords are used to decrypt or encrypt with extra security. The password can contain these characters:\r\nLetters: a-z, A-Z\r\nDigits: 0-9\r\nSymbols: (none yet)\r\n\r\nDouble-click to reveal the password.";
        private static string runDecryptionInfo = "Decrypt the file from the image.";
        private static string runEncryptionInfo = "Encrypt the file into the image.";
        private static string selectImageDecryptInfo = "Select the image from which to extract a file. Clicking again allows you to re-select an image file.";
        private static string selectImageEncryptInfo = "Select the image into which a file will be encrypted. Clicking again allows you to re-select an image file.";
        private static string selectZipEncryptInfo = "Select a .zip file to encrypt within an image. Clicking again allows you to re-select a file.";
        private static string startupInfo = "Delfin " + VERSION + "\r\nWelcome to Delfin for Windows.";
        #endregion

        public Form_main()
        {
            InitializeComponent();
            mode = MODE.OPEN;
            SetInfoText(mainWelcomeInfo);
            textBox_feed.Text = startupInfo;
            Button_cancel_Click(null, null);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_main));
            this.button_encrypt = new System.Windows.Forms.Button();
            this.button_decrypt = new System.Windows.Forms.Button();
            this.button_selectImage = new System.Windows.Forms.Button();
            this.button_selectZip = new System.Windows.Forms.Button();
            this.button_execute = new System.Windows.Forms.Button();
            this.button_cancel = new System.Windows.Forms.Button();
            this.textBox_info = new System.Windows.Forms.TextBox();
            this.textBox_password = new System.Windows.Forms.TextBox();
            this.textBox_feed = new System.Windows.Forms.TextBox();
            this.label_password = new System.Windows.Forms.Label();
            this.pictureBox_delfin = new System.Windows.Forms.PictureBox();
            this.openFileDialog_image = new System.Windows.Forms.OpenFileDialog();
            this.openFileDialog_zip = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog_image = new System.Windows.Forms.SaveFileDialog();
            this.saveFileDialog_zip = new System.Windows.Forms.SaveFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_delfin)).BeginInit();
            this.SuspendLayout();
            // 
            // button_encrypt
            // 
            resources.ApplyResources(this.button_encrypt, "button_encrypt");
            this.button_encrypt.Name = "button_encrypt";
            this.button_encrypt.UseVisualStyleBackColor = true;
            this.button_encrypt.Click += new System.EventHandler(this.Button_encrypt_Click);
            this.button_encrypt.MouseEnter += new System.EventHandler(this.Button_encrypt_MouseEnter);
            this.button_encrypt.MouseLeave += new System.EventHandler(this.MouseIdle);
            // 
            // button_decrypt
            // 
            resources.ApplyResources(this.button_decrypt, "button_decrypt");
            this.button_decrypt.Name = "button_decrypt";
            this.button_decrypt.UseVisualStyleBackColor = true;
            this.button_decrypt.Click += new System.EventHandler(this.Button_decrypt_Click);
            this.button_decrypt.MouseEnter += new System.EventHandler(this.Button_decrypt_MouseEnter);
            this.button_decrypt.MouseLeave += new System.EventHandler(this.MouseIdle);
            // 
            // button_selectImage
            // 
            resources.ApplyResources(this.button_selectImage, "button_selectImage");
            this.button_selectImage.Name = "button_selectImage";
            this.button_selectImage.UseVisualStyleBackColor = true;
            this.button_selectImage.Click += new System.EventHandler(this.Button_selectImage_Click);
            this.button_selectImage.MouseEnter += new System.EventHandler(this.Button_selectImage_MouseEnter);
            this.button_selectImage.MouseLeave += new System.EventHandler(this.MouseIdle);
            // 
            // button_selectZip
            // 
            resources.ApplyResources(this.button_selectZip, "button_selectZip");
            this.button_selectZip.Name = "button_selectZip";
            this.button_selectZip.UseVisualStyleBackColor = true;
            this.button_selectZip.Click += new System.EventHandler(this.Button_selectZip_Click);
            this.button_selectZip.MouseEnter += new System.EventHandler(this.Button_selectZip_MouseEnter);
            this.button_selectZip.MouseLeave += new System.EventHandler(this.MouseIdle);
            // 
            // button_execute
            // 
            resources.ApplyResources(this.button_execute, "button_execute");
            this.button_execute.Name = "button_execute";
            this.button_execute.UseVisualStyleBackColor = true;
            this.button_execute.Click += new System.EventHandler(this.Button_execute_Click);
            this.button_execute.MouseEnter += new System.EventHandler(this.Button_execute_MouseEnter);
            this.button_execute.MouseLeave += new System.EventHandler(this.MouseIdle);
            // 
            // button_cancel
            // 
            resources.ApplyResources(this.button_cancel, "button_cancel");
            this.button_cancel.Name = "button_cancel";
            this.button_cancel.UseVisualStyleBackColor = true;
            this.button_cancel.Click += new System.EventHandler(this.Button_cancel_Click);
            this.button_cancel.MouseEnter += new System.EventHandler(this.Button_cancel_MouseEnter);
            this.button_cancel.MouseLeave += new System.EventHandler(this.MouseIdle);
            // 
            // textBox_info
            // 
            resources.ApplyResources(this.textBox_info, "textBox_info");
            this.textBox_info.BackColor = System.Drawing.SystemColors.Window;
            this.textBox_info.Name = "textBox_info";
            this.textBox_info.ReadOnly = true;
            this.textBox_info.TabStop = false;
            // 
            // textBox_password
            // 
            resources.ApplyResources(this.textBox_password, "textBox_password");
            this.textBox_password.Name = "textBox_password";
            this.textBox_password.UseSystemPasswordChar = true;
            this.textBox_password.DoubleClick += new System.EventHandler(this.TextBox_password_DoubleClick);
            this.textBox_password.MouseEnter += new System.EventHandler(this.TextBox_password_MouseEnter);
            this.textBox_password.MouseLeave += new System.EventHandler(this.TextBox_password_MouseLeave);
            // 
            // textBox_feed
            // 
            resources.ApplyResources(this.textBox_feed, "textBox_feed");
            this.textBox_feed.BackColor = System.Drawing.SystemColors.Window;
            this.textBox_feed.Name = "textBox_feed";
            this.textBox_feed.ReadOnly = true;
            this.textBox_feed.TabStop = false;
            this.textBox_feed.MouseEnter += new System.EventHandler(this.TextBox_feed_MouseEnter);
            this.textBox_feed.MouseLeave += new System.EventHandler(this.MouseIdle);
            // 
            // label_password
            // 
            resources.ApplyResources(this.label_password, "label_password");
            this.label_password.Name = "label_password";
            // 
            // pictureBox_delfin
            // 
            resources.ApplyResources(this.pictureBox_delfin, "pictureBox_delfin");
            this.pictureBox_delfin.Name = "pictureBox_delfin";
            this.pictureBox_delfin.TabStop = false;
            // 
            // openFileDialog_image
            // 
            this.openFileDialog_image.DefaultExt = "png";
            resources.ApplyResources(this.openFileDialog_image, "openFileDialog_image");
            // 
            // openFileDialog_zip
            // 
            this.openFileDialog_zip.DefaultExt = "zip";
            resources.ApplyResources(this.openFileDialog_zip, "openFileDialog_zip");
            // 
            // saveFileDialog_image
            // 
            this.saveFileDialog_image.DefaultExt = "png";
            resources.ApplyResources(this.saveFileDialog_image, "saveFileDialog_image");
            // 
            // saveFileDialog_zip
            // 
            this.saveFileDialog_zip.DefaultExt = "zip";
            resources.ApplyResources(this.saveFileDialog_zip, "saveFileDialog_zip");
            // 
            // Form_main
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.pictureBox_delfin);
            this.Controls.Add(this.label_password);
            this.Controls.Add(this.textBox_feed);
            this.Controls.Add(this.textBox_password);
            this.Controls.Add(this.textBox_info);
            this.Controls.Add(this.button_cancel);
            this.Controls.Add(this.button_execute);
            this.Controls.Add(this.button_selectZip);
            this.Controls.Add(this.button_selectImage);
            this.Controls.Add(this.button_decrypt);
            this.Controls.Add(this.button_encrypt);
            this.MaximizeBox = false;
            this.Name = "Form_main";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_delfin)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void Button_encrypt_MouseEnter(object sender, EventArgs e)
        {
            SetInfoText(encryptionInfo);
        }

        private void Button_decrypt_MouseEnter(object sender, EventArgs e)
        {
            SetInfoText(decryptionInfo);
        }

        private void Button_selectImage_MouseEnter(object sender, EventArgs e)
        {
            if (mode == MODE.DECRYPT)
            {
                SetInfoText(selectImageDecryptInfo);
            }
            else if (mode == MODE.ENCRYPT)
            {
                SetInfoText(selectImageEncryptInfo);
            }
        }

        private void Button_selectZip_MouseEnter(object sender, EventArgs e)
        {
            SetInfoText(selectZipEncryptInfo);
        }

        private void Button_execute_MouseEnter(object sender, EventArgs e)
        {
            if (mode == MODE.DECRYPT)
            {
                SetInfoText(runDecryptionInfo);
            }
            else if (mode == MODE.ENCRYPT)
            {
                SetInfoText(runEncryptionInfo);
            }
        }

        private void Button_cancel_MouseEnter(object sender, EventArgs e)
        {
            if (mode == MODE.DECRYPT)
            {
                SetInfoText(cancelDecryptionInfo);
            }
            else if (mode == MODE.ENCRYPT)
            {
                SetInfoText(cancelEncryptionInfo);
            }
        }

        private void TextBox_password_MouseEnter(object sender, EventArgs e)
        {
            SetInfoText(passwordInfo);
        }

        private void TextBox_feed_MouseEnter(object sender, EventArgs e)
        {
            SetInfoText(feedInfo);
        }

        private void TextBox_password_MouseLeave(object sender, EventArgs e)
        {
            textBox_password.UseSystemPasswordChar = true;
            MouseIdle(sender, e);
        }

        // called when the mouse leaves most fields, to reset the info text.
        private void MouseIdle(object sender, EventArgs e)
        {
            if (mode == MODE.OPEN)
            {
                SetInfoText(mainWelcomeInfo);
            }
            else if (mode == MODE.ENCRYPT)
            {
                SetInfoText(mainEncryptInfo);
            }
            else if (mode == MODE.DECRYPT)
            {
                SetInfoText(mainDecryptInfo);
            }
        }

        private void Button_encrypt_Click(object sender, EventArgs e)
        {
            mode = MODE.ENCRYPT;
            button_encrypt.Enabled = false;
            button_decrypt.Enabled = false;
            button_selectImage.Enabled = true;
            button_selectZip.Enabled = true;
            button_execute.Enabled = false;
            button_cancel.Enabled = true;
        }

        private void Button_decrypt_Click(object sender, EventArgs e)
        {
            mode = MODE.DECRYPT;
            button_encrypt.Enabled = false;
            button_decrypt.Enabled = false;
            button_selectImage.Enabled = true;
            button_selectZip.Enabled = false;
            button_execute.Enabled = false;
            button_cancel.Enabled = true;
        }

        private void Button_selectImage_Click(object sender, EventArgs e)
        {
            if (openFileDialog_image.ShowDialog() == DialogResult.OK)
            {
                if (openFileDialog_image.FileName.EndsWith(".png"))
                {
                    hasImage = true;
                    if (mode == MODE.DECRYPT)
                    {
                        button_execute.Enabled = true;
                    }
                    else if (mode == MODE.ENCRYPT && hasZip)
                    {
                        button_execute.Enabled = true;
                    }
                    UpdateFeed("Image selected: " + ShortFileName(openFileDialog_image.FileName));
                }
                else
                {
                    hasImage = false;
                    button_execute.Enabled = false;
                    MessageBox.Show("You must select a .png file.", "Invalid file");
                }
            }
        }

        private void Button_selectZip_Click(object sender, EventArgs e)
        {
            if (openFileDialog_zip.ShowDialog() == DialogResult.OK)
            {
                if (openFileDialog_zip.FileName.EndsWith(".zip"))
                {
                    hasZip = true;
                    if (hasImage)
                    {
                        button_execute.Enabled = true;
                    }
                    UpdateFeed("Zip file selected: " + ShortFileName(openFileDialog_zip.FileName));
                }
                else
                {
                    hasZip = false;
                    button_execute.Enabled = false;
                    MessageBox.Show("You must select a .zip file", "Invalid file");
                }
            }
        }

        private void Button_execute_Click(object sender, EventArgs e)
        {
            if (textBox_password.Text.Length != 0 && !IsPasswordValid(textBox_password.Text))
            {
                MessageBox.Show("The password field contains an invalid password. Please enter a valid password or leave the password field blank.", "Invalid password");
                return;
            }

            button_encrypt.Enabled = false;
            button_decrypt.Enabled = false;
            button_selectImage.Enabled = false;
            button_selectZip.Enabled = false;
            button_execute.Enabled = false;
            button_cancel.Enabled = false;

            if (mode == MODE.ENCRYPT)
            {
                UpdateFeed("Encrypting " + ShortFileName(openFileDialog_zip.FileName) + " into " + ShortFileName(openFileDialog_image.FileName) + "...");
                if (Encrypt(openFileDialog_image.FileName, openFileDialog_zip.FileName, textBox_password.Text))
                {
                    UpdateFeed("Encryption successful.");
                }
                else
                {
                    UpdateFeed("Encryption failed. Reason: " + errMsg);
                }
            }
            else if (mode == MODE.DECRYPT)
            {
                UpdateFeed("Decrypting " + ShortFileName(openFileDialog_image.FileName) + "...");
                if (Decrypt(openFileDialog_image.FileName, textBox_password.Text))
                {
                    UpdateFeed("Decryption successful.");
                }
                else
                {
                    UpdateFeed("Decryption failed. Reason: " + errMsg);
                }
            }

            mode = MODE.OPEN;
            hasImage = hasZip = false;
            button_encrypt.Enabled = true;
            button_decrypt.Enabled = true;
        }

        private void Button_cancel_Click(object sender, EventArgs e)
        {
            mode = MODE.OPEN;
            hasImage = hasZip = false;
            button_encrypt.Enabled = true;
            button_decrypt.Enabled = true;
            button_selectImage.Enabled = false;
            button_selectZip.Enabled = false;
            button_execute.Enabled = false;
            button_cancel.Enabled = false;
        }

        private void TextBox_password_DoubleClick(object sender, EventArgs e)
        {
            textBox_password.UseSystemPasswordChar = !textBox_password.UseSystemPasswordChar;
        }

        private void SetInfoText(string text)
        {
            textBox_info.Text = text;
        }

        private void UpdateFeed(string text)
        {
            textBox_feed.AppendText(Environment.NewLine + text);
        }

        // returns a file's name when given a full path, if possible
        private string ShortFileName(string longFileName)
        {
            try
            {
                return longFileName.Substring(longFileName.LastIndexOf("\\") + 1);
            }
            catch (ArgumentOutOfRangeException)
            {
                return longFileName;
            }
        }

        private bool IsPasswordValid(string s)
        {
            Regex regex = new Regex("\\A[0-9A-Za-z]+\\z");
            return regex.IsMatch(s);
        }

        private bool Decrypt(string imgName, string password)
        {
            long pixScan = 0, byteScan = -4, fileSize = 0;
            int color;
            byte[] pairBuffer = new byte[6];
            int population = 0;
            int datum;
            byte[] fileBuffer = null;
            Bitmap img;

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
                    return false;
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("The file\r\n" + imgName + "\r\nwas not found.", "File not found");
                    errMsg = "file not found";
                    return false;
                }
                catch (IOException)
                {
                    MessageBox.Show("An I/O error occurred while opening the file.", "Unexpected I/O error");
                    errMsg = "unexpected I/O error";
                    return false;
                }
                catch (System.Security.SecurityException)
                {
                    MessageBox.Show("You don't have permission to access the file:\r\n" + imgName, "Unauthorized access");
                    errMsg = "unauthorized access";
                    return false;
                }
                try
                {
                    img = new Bitmap(reader);
                }
                catch (ArgumentException)
                {
                    MessageBox.Show("The file\r\n" + imgName + "\r\ncould not be interpreted as a valid image.", "Invalid image");
                    errMsg = "invalid image file";
                    return false;
                }
                reader.Close();
                reader.Dispose();
            }

            // main data-processing loop
            while (byteScan < fileSize)
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

                    // determine whether byte is part of filesize header or part of file
                    if (byteScan < 0)
                    {
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
                        // part of header; continue to construct fileSize value
                        fileSize = fileSize << 8 | datum;
#pragma warning restore CS0675

                        // if filesize construction is complete, verify it and allocate buffer
                        if (byteScan == -1)
                        {
                            // ensure file fits per completed header
                            if (img.Height * img.Width * 3 / 4 < fileSize + 4)
                            {
                                errMsg = "corrupt file (bad header)";
                                return false;
                            }
                            else
                            {
                                fileBuffer = new byte[fileSize];
                            }
                        }
                    }
                    else
                    {
                        // add datum to buffer
                        fileBuffer[byteScan] = (byte)datum;
                    }

                    byteScan++;
                }
            }

            // Decrypt file using cipher, if there was a password
            if (!password.Equals(""))
            {
                Cipher cipher = new Cipher(password);
                for (int i = 0; i < fileBuffer.Length; i++)
                {
                    fileBuffer[i] ^= cipher.GetByte();
                }
            }

            // prompt user to save file
            if (saveFileDialog_zip.ShowDialog() == DialogResult.OK && saveFileDialog_zip.FileName.EndsWith(".zip"))
            {
                BinaryWriter writer;
                try
                {
                    writer = new BinaryWriter(File.Open(saveFileDialog_zip.FileName, FileMode.Create));
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
                    MessageBox.Show("The path\r\n" + saveFileDialog_zip.FileName + "\r\nis not a valid path. Please specify a valid path.", "Invalid path name");
                    errMsg = "invalid path name";
                    return false;
                }
                catch (IOException)
                {
                    MessageBox.Show("An I/O error occurred while using the file.", "Unexpected I/O error");
                    errMsg = "unexpected I/O error";
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("You don't have permission to access the file:\r\n" + saveFileDialog_zip.FileName, "Unauthorized access");
                    errMsg = "unauthorized access";
                    return false;
                }

                writer.Flush();
                writer.Close();
                writer.Dispose();
                return true;
            }
            else
            {
                MessageBox.Show("The file was not saved.", "Canceled operation");
                errMsg = "user cancellation";
                return false;
            }
        }

        private bool Encrypt(string imgName, string fileName, string password)
        {
            long pixScan = 0, byteScan = -4, fileSize;
            int pixX, pixY;
            int color, A, R, G, B;
            byte[] pairBuffer = new byte[6];
            int population = 0;
            int datum;
            Bitmap img;
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
                return false;
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("The file\r\n" + fileName + "\r\nwas not found.", "File not found");
                errMsg = "file not found";
                return false;
            }
            catch (IOException)
            {
                MessageBox.Show("An I/O error occurred while opening the file.", "Unexpected I/O error");
                errMsg = "unexpected I/O error";
                return false;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
            {
                MessageBox.Show("You don't have permission to access the file:\r\n" + fileName, "Unauthorized access");
                errMsg = "unauthorized access";
                return false;
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
                    return false;
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("The file\r\n" + imgName + "\r\nwas not found.", "File not found");
                    errMsg = "file not found";
                    return false;
                }
                catch (IOException)
                {
                    MessageBox.Show("An I/O error occurred while opening the file.", "Unexpected I/O error");
                    errMsg = "unexpected I/O error";
                    return false;
                }
                catch (System.Security.SecurityException)
                {
                    MessageBox.Show("You don't have permission to access the file:\r\n" + imgName, "Unauthorized access");
                    errMsg = "unauthorized access";
                    return false;
                }
                try
                {
                    img = new Bitmap(reader);
                }
                catch (ArgumentException)
                {
                    MessageBox.Show("The file\r\n" + imgName + "\r\ncould not be interpreted as a valid image.", "Invalid image");
                    errMsg = "invalid image file";
                    return false;
                }
                reader.Close();
                reader.Dispose();
            }

            // verify image is large enough to hold the file or quit
            if (img.Height * img.Width * 3 / 4 < fileSize + 4)
            {
                errMsg = "image too small";
                return false;
            }
            
            // Encrypt file using cipher, if there was a password
            if (!password.Equals(""))
            {
                Cipher cipher = new Cipher(password);
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
                            // read a byte from fileSize for the "header"
                            datum = (int)((fileSize >> ((int)byteScan * -8 - 8)) & 255);
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
            if (saveFileDialog_image.ShowDialog() == DialogResult.OK && saveFileDialog_image.FileName.EndsWith(".png"))
            {
                FileStream writer;
                try
                {
                    writer = new FileStream(saveFileDialog_image.FileName, FileMode.Create);
                }
                catch (Exception ex) when
                    (ex is ArgumentException ||
                    ex is NotSupportedException ||
                    ex is ArgumentNullException ||
                    ex is DirectoryNotFoundException ||
                    ex is PathTooLongException)
                {
                    // path is null, empty, or invalid due to length, drive, or characters
                    MessageBox.Show("The path\r\n" + saveFileDialog_image.FileName + "\r\nis not a valid path. Please specify a valid path.", "Invalid path name");
                    errMsg = "invalid path name";
                    return false;
                }
                catch (IOException)
                {
                    MessageBox.Show("An I/O error occurred while using the file.", "Unexpected I/O error");
                    errMsg = "unexpected I/O error";
                    return false;
                }
                catch (System.Security.SecurityException)
                {
                    MessageBox.Show("You don't have permission to access the file:\r\n" + saveFileDialog_image.FileName, "Unauthorized access");
                    errMsg = "unauthorized access";
                    return false;
                }

                img.Save(writer, System.Drawing.Imaging.ImageFormat.Png);
                writer.Close();
                writer.Dispose();
                return true;
            }
            else
            {
                MessageBox.Show("The file was not saved.", "Canceled operation");
                errMsg = "user cancellation";
                return false;
            }
        }
    }
}