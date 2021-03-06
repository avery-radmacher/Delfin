﻿using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
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
        static readonly string VERSION = "0.8";

        // flags
        MODE mode;
        bool hasImage, hasZip;

        Thread cryptionThread; // used to manage encryption and decryption
        CancellationTokenSource cancellationTokenSource;

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
        private Button button_Settings;

        #region info texts
        private static readonly string cancelDecryptionInfo = "Cancel the decryption.";
        private static readonly string cancelEncryptionInfo = "Cancel the encryption.";
        private static readonly string cancellationInfo = "To abort the running operation, click cancel.";
        private static readonly string decryptionInfo = "Extract the compressed files from an image.";
        private static readonly string encryptionInfo = "Encrypt a compressed file into an image.";
        private static readonly string feedInfo = "A description of recent actions and events will appear in the feed.";
        private static readonly string mainDecryptInfo = "Choose an image to decrypt and specify a password for access. Leave password blank if there is no password. When you are ready, click run to select a save destination.";
        private static readonly string mainEncryptInfo = "Choose an image and a .zip file to encrypt in the image, optionally specifying a password for extra security. When you are ready, click run to select a save destination.";
        private static readonly string mainWelcomeInfo = "To continue, select an option from the left.\r\n\r\nMouse over an option to learn more.";
        private static readonly string passwordInfo = "Passwords are used to decrypt or encrypt with extra security. The password can contain these characters:\r\nLetters: a-z, A-Z\r\nDigits: 0-9\r\nSymbols: !@#$%^&*()-_=+[{]}\\|;:'\",<.>/?\r\n\r\nDouble-click to reveal the password.";
        private static readonly string runDecryptionInfo = "Decrypt the file from the image.";
        private static readonly string runEncryptionInfo = "Encrypt the file into the image.";
        private static readonly string selectImageDecryptInfo = "Select the image from which to extract a file. Clicking again allows you to re-select an image file.";
        private static readonly string selectImageEncryptInfo = "Select the image into which a file will be encrypted. Clicking again allows you to re-select an image file.";
        private static readonly string selectZipEncryptInfo = "Select a .zip file to encrypt within an image. Clicking again allows you to re-select a file.";
        private static readonly string settingsInfo = "Click to view and edit settings.";
        private static readonly string startupInfo = $"Delfin {VERSION}\r\nWelcome to Delfin for Windows.";
        #endregion

        public Form_main()
        {
            InitializeComponent();
            SetInfoText(mainWelcomeInfo);
            textBox_feed.Text = startupInfo;
            InitializeStateAndButtons();
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
            this.button_Settings = new System.Windows.Forms.Button();
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
            this.textBox_info.BackColor = System.Drawing.SystemColors.Window;
            resources.ApplyResources(this.textBox_info, "textBox_info");
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
            this.textBox_feed.BackColor = System.Drawing.SystemColors.Window;
            resources.ApplyResources(this.textBox_feed, "textBox_feed");
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
            this.pictureBox_delfin.DoubleClick += new System.EventHandler(this.PictureBox_delfin_DoubleClick);
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
            // button_Settings
            // 
            resources.ApplyResources(this.button_Settings, "button_Settings");
            this.button_Settings.Name = "button_Settings";
            this.button_Settings.UseVisualStyleBackColor = true;
            this.button_Settings.Click += new System.EventHandler(this.Button_Settings_Click);
            this.button_Settings.MouseEnter += new System.EventHandler(this.Button_Settings_MouseEnter);
            this.button_Settings.MouseLeave += new System.EventHandler(this.MouseIdle);
            // 
            // Form_main
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.button_Settings);
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

        #region event handlers
        private void Button_encrypt_MouseEnter(object sender, EventArgs e)
        {
            SetInfoText(encryptionInfo);
            openFileDialog_image.Filter = "All files|*.*|PNG image|*.png|JPG image|*.jpg|JPEG image|*.jpeg|TIFF image|*.tiff|Bitmap|*.bmp";
        }

        private void Button_decrypt_MouseEnter(object sender, EventArgs e)
        {
            SetInfoText(decryptionInfo);
            openFileDialog_image.Filter = "PNG image|*.png";
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

        private void Button_Settings_MouseEnter(object sender, EventArgs e)
        {
            SetInfoText(settingsInfo);
        }

        private void TextBox_password_MouseEnter(object sender, EventArgs e)
        {
            SetInfoText(passwordInfo);
        }

        private void TextBox_feed_MouseEnter(object sender, EventArgs e)
        {
            SetInfoText(feedInfo);
        }

        // hides password and calls MouseIdle
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
            else if(cryptionThread != null)
            {
                SetInfoText(cancellationInfo);
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
                // make sure a valid file was selected
                // (multiple image formats can be encrypted; only .png can be decrypted)
                if ((mode == MODE.ENCRYPT &&
                        (openFileDialog_image.FileName.EndsWith(".png") ||
                        openFileDialog_image.FileName.EndsWith(".jpg") ||
                        openFileDialog_image.FileName.EndsWith(".jpeg") ||
                        openFileDialog_image.FileName.EndsWith(".tiff") ||
                        openFileDialog_image.FileName.EndsWith(".bmp"))) ||
                    (mode == MODE.DECRYPT &&
                        openFileDialog_image.FileName.EndsWith(".png")))
                {
                    hasImage = true;
                    if (mode == MODE.DECRYPT || (mode == MODE.ENCRYPT && hasZip))
                    {
                        button_execute.Enabled = true;
                    }
                    UpdateFeed($"Image selected: {openFileDialog_image.FileName.ShortFileName()}");
                }
                else
                {
                    hasImage = false;
                    button_execute.Enabled = false;
                    MessageBox.Show("You must select a valid image file format.", "Invalid file");
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
                    UpdateFeed($"Zip file selected: {openFileDialog_zip.FileName.ShortFileName()}");
                }
                else
                {
                    hasZip = false;
                    button_execute.Enabled = false;
                    MessageBox.Show("You must select a .zip file", "Invalid file");
                }
            }
        }

        // runs the encryption or decryption on a background thread
        private void Button_execute_Click(object sender, EventArgs e)
        {
            if (textBox_password.Text.Length != 0 && !textBox_password.Text.IsValidPassword())
            {
                MessageBox.Show("The password field contains an invalid password. Please enter a valid password or leave the password field blank.", "Invalid password");
                return;
            }

            button_encrypt.Enabled = false;
            button_decrypt.Enabled = false;
            button_selectImage.Enabled = false;
            button_selectZip.Enabled = false;
            button_execute.Enabled = false;

            cancellationTokenSource = new();
            
            if (mode == MODE.ENCRYPT)
            {
                cryptionThread = new Thread(() => Encrypt(openFileDialog_image.FileName, openFileDialog_zip.FileName, textBox_password.Text, cancellationTokenSource.Token));
                cryptionThread.SetApartmentState(ApartmentState.STA);
                cryptionThread.Start();
            }
            else if (mode == MODE.DECRYPT)
            {
                cryptionThread = new Thread(() => Decrypt(openFileDialog_image.FileName, textBox_password.Text, cancellationTokenSource.Token));
                cryptionThread.SetApartmentState(ApartmentState.STA);
                cryptionThread.Start();
            }
        }

        // resets the form and kills any background threads
        private void Button_cancel_Click(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();

            InitializeStateAndButtons();
        }

        private void Button_Settings_Click(object sender, EventArgs e)
        {
            MessageBox.Show("In the future, this button will do something.", "Settings button");
            UpdateFeed("The user is curious.");
        }

        // reveals/hides password
        private void TextBox_password_DoubleClick(object sender, EventArgs e)
        {
            textBox_password.UseSystemPasswordChar = !textBox_password.UseSystemPasswordChar;
        }

        private void PictureBox_delfin_DoubleClick(object sender, EventArgs e)
        {
            if (cryptionThread == null)
            {
                return;
            }

            if (textBox_password.Text.ToLower().Contains("marino"))
            {
                if (textBox_password.Text.ToLower().Contains("dan"))
                {
                    UpdateFeed("You're a big guy.");
                    pictureBox_delfin.Image = Image.FromFile("ETD2.png");
                }
                else
                {
                    UpdateFeed("You paid homage to Marino.");
                }
            }
            else
            {
                UpdateFeed("You paid homage to Dan.");
            }
        }
        #endregion

        private void SetInfoText(string text)
        {
            textBox_info.Text = text;
        }

        // threadsafe call to add text to the feedbox
        private void UpdateFeed(string text)
        {
            if(textBox_feed.InvokeRequired)
            {
                textBox_feed.Invoke(new Action<string>(UpdateFeed), text);
                return;
            }

            textBox_feed.AppendText(Environment.NewLine + text);
        }

        // threadsafe display of message box with string and caption
        private void ShowMessage(string text, string caption)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, string>(ShowMessage), text, caption);
                return;
            }

            MessageBox.Show(text, caption);
        }

        private void Decrypt(string imgName, string password, CancellationToken token)
        {
            UpdateFeed($"Decrypting {openFileDialog_image.FileName.ShortFileName()}...");

            string saveFilename;
            if (saveFileDialog_zip.ShowDialog() == DialogResult.OK)
            {
                saveFilename = saveFileDialog_zip.FileName;
            }
            else
            {
                ProcessResult(false, "Cancelled operation", "Saving the result was cancelled.", MODE.DECRYPT);
                return;
            }

            Cryptor.Decrypt(imgName, password, saveFilename, (result) => ProcessResult(result, MODE.DECRYPT), token);
        }

        private void Encrypt(string imgName, string filename, string password, CancellationToken token)
        {
            UpdateFeed($"Encrypting {openFileDialog_zip.FileName.ShortFileName()} into {openFileDialog_image.FileName.ShortFileName()}...");

            string saveFilename;
            if (saveFileDialog_image.ShowDialog() == DialogResult.OK)
            {
                saveFilename = saveFileDialog_image.FileName;
            }
            else
            {
                ProcessResult(false, "Cancelled operation", "Saving the result was cancelled.", MODE.ENCRYPT);
                return;
            }

            Cryptor.Encrypt(imgName, filename, password, saveFilename, (result) => ProcessResult(result, MODE.ENCRYPT), token);
        }

        private void ProcessResult(bool success, string errMsg, string errDescription, MODE mode)
        {
            string modeString = mode == MODE.DECRYPT ? "Decryption" : "Encryption";
            if (success)
            {
                UpdateFeed($"{modeString} successful.");
                ShowMessage($"{modeString} successful.", "Success");
            }
            else
            {
                UpdateFeed($"{modeString} failed. Reason: {errDescription}");
                ShowMessage($"{modeString} failed. Reason: {errDescription}", $"{errMsg}");
            }

            InitializeStateAndButtons();
        }

        private void ProcessResult(CryptionResult result, MODE mode) => ProcessResult(result.Success, result.ErrMsg, result.ErrDescription, mode);

        // threadsafe call to enter primary state, enabling/disabling certain buttons and setting flags
        private void InitializeStateAndButtons()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(InitializeStateAndButtons));
                return;
            }

            mode = MODE.OPEN;
            hasImage = hasZip = false;
            button_encrypt.Enabled = true;
            button_decrypt.Enabled = true;
            button_selectImage.Enabled = false;
            button_selectZip.Enabled = false;
            button_execute.Enabled = false;
            button_cancel.Enabled = false;
            SetInfoText(mainWelcomeInfo);
            cryptionThread = null;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }

    public static class Helpers
    {
        private static readonly Regex passwordRegex = new(@"\A[0-9A-Za-z!@#$%^&*()\-_=+[{\]}\\|;:'"",<.>/?]+\z");

        // returns a file's name when given a full path, if possible
        public static string ShortFileName(this string longFileName)
        {
            try
            {
                return longFileName[(longFileName.LastIndexOf("\\") + 1)..];
            }
            catch (ArgumentOutOfRangeException)
            {
                return longFileName;
            }
        }

        // check whether password contains only valid characters
        public static bool IsValidPassword(this string password)
        {
            return passwordRegex.IsMatch(password);
        }
    }
}
