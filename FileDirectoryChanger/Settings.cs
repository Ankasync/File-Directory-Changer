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
            // Tu�a bas�ld���nda �al��acak kodlar
            if (e.KeyCode == key)
            {
                try
                {
                    DirectoryInfo folder = new DirectoryInfo(SourceFolder);
                    var files = folder.GetFiles();

                    // Dosyalar� son de�i�tirilme tarihine g�re s�rala
                    var sequentialFiles = files.OrderByDescending(f => f.LastWriteTime);

                    // En son de�i�tirilen dosyay� al
                    var lastFile = sequentialFiles.FirstOrDefault();
                    if (lastFile == null)
                    {
                        MessageBox.Show("Kaynak klas�rde dosya bulunamad�.");
                        return;
                    }

                    string DestinationFolderPath = Path.Combine(DestinationFolder, lastFile.Name);

                    // Dosya zaten var m� kontrol et
                    if (File.Exists(DestinationFolderPath))
                    {
                        DialogResult result = MessageBox.Show(
                            "Hedef klas�rde ayn� isimde bir dosya var. �st�ne yazmak ister misiniz? " +
                            "�st�ne yazmak i�in Yes, yeni isim i�in No, iptal etmek i�in Cancel'a t�klay�n.",
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
                        // E�er Yes se�ene�i se�ilirse, devam edip mevcut dosya �zerine yaz�l�r
                    }

                    File.Copy(lastFile.FullName, DestinationFolderPath, true);

                    // Dosya boyutlar�n� kar��la�t�r
                    if (new FileInfo(DestinationFolderPath).Length == lastFile.Length)
                    {
                        MessageBox.Show("Dosya ba�ar�yla ta��nd�.");
                    }
                    else
                    {
                        MessageBox.Show("Bir sorun olu�tu. L�tfen dosyalar� kontrol edin.");
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

                // Girdi do�rulamas�
                if (KeyNode == null || SourceFolderNode == null || DestinationFolderNode == null)
                {
                    throw new Exception("Yap�land�rma dosyas�nda eksik bilgiler var.");
                }

                SourceFolder = SourceFolderNode.InnerText;
                DestinationFolder = DestinationFolderNode.InnerText;

                // Enum.TryParse kullan�m� ve girdi kontrol�
                if (!Enum.TryParse(KeyNode.InnerText, out key))
                {
                    throw new Exception("Ge�ersiz anahtar de�eri.");
                }

                KeyboardListener.HookedKeys.Add(key);
                KeyboardListener.KeyUp += new KeyEventHandler(handleKeyPress);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yap�land�rma okunurken hata olu�tu: {ex.Message}");
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
                    MessageBox.Show("G�rev zamanlay�c� kaydedildi.");
                }
            }
            catch (Exception exe)
            {
                MessageBox.Show($"G�rev zamanlay�c� kaydedilirken hata olu�tu: {exe.Message}");
            }

            if (!File.Exists(Application.StartupPath + @"\Config.xml"))
            {
                MessageBox.Show("Anahtar ve dizinleri se�in", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show("Bo� b�rak�lamaz.");
                return;
            }

            try
            {
                if (!Enum.TryParse(comboBox1.Text, out Keys selectedKey))
                {
                    MessageBox.Show("Ge�ersiz anahtar se�imi.");
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
