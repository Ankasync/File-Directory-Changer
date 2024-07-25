using GlobalHookExample;
using Microsoft.Win32.TaskScheduler;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace FileDirectoryChanger
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string SourceFolder = "";
        string DestinationFolder = "";
        Keys key;
        globalKeyboardHook KeyboardListener = new globalKeyboardHook();

        bool closeState = false;
        bool showState = false;

        void handleKeyPress(object sender, KeyEventArgs e)
        {
            // Tuþa basýldýðýnda çalýþacak kodlar
            if (e.KeyCode == key)
            {
                try
                {
                    DirectoryInfo folder = new DirectoryInfo(SourceFolder);
                    var files = folder.GetFiles();

                    // Dosyalarý son deðiþtirilme tarihine göre sýrala
                    var sequentialFiles = files.OrderByDescending(f => f.LastWriteTime);

                    // En son deðiþtirilen dosyayý al
                    var lastFile = sequentialFiles.FirstOrDefault();
                    if (lastFile == null)
                    {
                        MessageBox.Show("Kaynak klasörde dosya bulunamadý.");
                        return;
                    }

                    string DestinationFolderPath = Path.Combine(DestinationFolder, lastFile.Name);

                    // Dosya zaten var mý kontrol et
                    if (File.Exists(DestinationFolderPath))
                    {
                        DialogResult result = MessageBox.Show(
                            "Hedef klasörde ayný isimde bir dosya var. Üstüne yazmak ister misiniz? " +
                            "Üstüne yazmak için Yes, yeni isim için No, iptal etmek için Cancel'a týklayýn.",
                            "Dosya Zaten Mevcut",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Cancel)
                        {
                            return;
                        }
                        else if (result == DialogResult.No)
                        {
                            string newFileName = Path.GetFileNameWithoutExtension(lastFile.Name) +
                                                 "_Copy" +
                                                 Path.GetExtension(lastFile.Name);
                            DestinationFolderPath = Path.Combine(DestinationFolder, newFileName);
                        }
                        // Eðer Yes seçeneði seçilirse, devam edip mevcut dosya üzerine yazýlýr
                    }

                    File.Copy(lastFile.FullName, DestinationFolderPath, true);

                    // Dosya boyutlarýný karþýlaþtýr
                    if (new FileInfo(DestinationFolderPath).Length == lastFile.Length)
                    {
                        MessageBox.Show("Dosya baþarýyla taþýndý.");
                    }
                    else
                    {
                        MessageBox.Show("Bir sorun oluþtu. Lütfen dosyalarý kontrol edin.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hata: {ex.Message}");
                }
            }

            e.Handled = false;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (!showState)
            {
                this.Hide();
            }
            else
            {
                showState = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!closeState) e.Cancel = true;
            this.Hide();
        }

        private void ShowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void HideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            closeState = true;
            Application.Exit();
        }

        public void ConfigureHook()
        {
            KeyboardListener = new globalKeyboardHook();
            XmlDocument Read = new XmlDocument();
            try
            {
                Read.Load(Application.StartupPath + @"\Config.xml");
                XmlNode KeyNode = Read.GetElementsByTagName("Key")[0];
                XmlNode SourceFolderNode = Read.GetElementsByTagName("SourceFolder")[0];
                XmlNode DestinationFolderNode = Read.GetElementsByTagName("DestinationFolder")[0];

                // Girdi doðrulamasý
                if (KeyNode == null || SourceFolderNode == null || DestinationFolderNode == null)
                {
                    throw new Exception("Yapýlandýrma dosyasýnda eksik bilgiler var.");
                }

                SourceFolder = SourceFolderNode.InnerText;
                DestinationFolder = DestinationFolderNode.InnerText;

                // Enum.TryParse kullanýmý ve girdi kontrolü
                if (!Enum.TryParse(KeyNode.InnerText, out key))
                {
                    throw new Exception("Geçersiz anahtar deðeri.");
                }

                KeyboardListener.HookedKeys.Add(key);
                KeyboardListener.KeyUp += new KeyEventHandler(handleKeyPress);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yapýlandýrma okunurken hata oluþtu: {ex.Message}");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            TaskService ts = new TaskService();
            TaskDefinition td = ts.NewTask();
            td.RegistrationInfo.Description = "FileDirectoryChanger Service";
            td.Principal.RunLevel = TaskRunLevel.Highest;
            td.Triggers.AddNew(TaskTriggerType.Logon);
            td.Actions.Add(new ExecAction(Application.StartupPath + @"\FileDirectoryChanger.exe", Application.StartupPath + @"\FileDirectoryChanger.txt", null));

            try
            {
                if (!ts.RootFolder.AllTasks.Any(t => t.Name == "FileDirectoryChanger Task"))
                {
                    ts.RootFolder.RegisterTaskDefinition(@"FileDirectoryChanger Task", td);
                    MessageBox.Show("Görev zamanlayýcý kaydedildi.");
                }
            }
            catch (Exception exe)
            {
                MessageBox.Show($"Görev zamanlayýcý kaydedilirken hata oluþtu: {exe.Message}");
            }

            if (!File.Exists(Application.StartupPath + @"\Config.xml"))
            {
                MessageBox.Show("Anahtar ve dizinleri seçin", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                showState = true;
                this.Show();
            }
            else
            {
                ConfigureHook();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(DestinationFoldertxt.Text) || comboBox1.SelectedItem == null)
            {
                MessageBox.Show("Boþ býrakýlamaz.");
                return;
            }

            try
            {
                if (!Enum.TryParse(comboBox1.Text, out Keys selectedKey))
                {
                    MessageBox.Show("Geçersiz anahtar seçimi.");
                    return;
                }

                XmlDocument doc = new XmlDocument();
                XmlElement Settings = doc.CreateElement("Settings");
                doc.AppendChild(Settings);
                XmlElement SourceFolder = doc.CreateElement("SourceFolder");
                Settings.AppendChild(SourceFolder);
                XmlElement DestinationFolder = doc.CreateElement("DestinationFolder");
                Settings.AppendChild(DestinationFolder);

                XmlElement Key = doc.CreateElement("Key");
                Settings.AppendChild(Key);

                Key.InnerText = selectedKey.ToString();
                DestinationFolder.InnerText = this.DestinationFoldertxt.Text;
                SourceFolder.InnerText = this.SourceFoldertxt.Text;
                doc.Save(Application.StartupPath + @"\Config.xml");

                ConfigureHook();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}");
            }
        }

        private void textBox2_DoubleClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    DestinationFoldertxt.Text = dialog.SelectedPath + @"\";
                }
            }
        }

        private void textBox1_DoubleClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    SourceFoldertxt.Text = dialog.SelectedPath + @"\";
                }
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }
    }
}
