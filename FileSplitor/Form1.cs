using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileSplitor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = this.openFileDialog1.FileName;
                FileInfo fi = new FileInfo(this.textBox1.Text);
                this.showSplitMessage("Size: " + Math.Round((double)fi.Length / (1024 * 1024), 2));
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.textBox1.Text))
                return;
            if (string.IsNullOrEmpty(this.textBox2.Text))
                return;

            ParameterizedThreadStart ts = new ParameterizedThreadStart(this.SplitFile);
            Thread thread = new Thread(ts);
            thread.IsBackground = true;
            thread.Start(new Tuple<string, string>(this.textBox1.Text, this.textBox2.Text));
        }

        private void showSplitMessage(string msg)
        {
            if (this.lblMessage.InvokeRequired)
            {
                this.Invoke(new Action<Label, string>(setText), this.lblMessage, msg);
            }
            else
            {
                this.setText(this.lblMessage, msg);
            }
        }

        private void showMergeMessage(string msg)
        {
            if (this.lblMerMsg.InvokeRequired)
            {
                this.Invoke(new Action<Label, string>(setText), this.lblMerMsg, msg);
            }
            else
            {
                this.setText(this.lblMerMsg, msg);
            }
        }

        private void setText(Label lbl, string msg)
        {
            lbl.Text = msg;
        }

        private void SplitFile(object obj)
        {
            Tuple<string, string> arg = (Tuple<string, string>)obj;
            string filePath = arg.Item1;
            long pieceSize = long.Parse(arg.Item2) * 1024 * 1024;
            var fs = File.Open(filePath, FileMode.Open);
            long fileLength = fs.Length;
            //FileInfo fi = new FileInfo(filePath);
            int fileCount = (int)Math.Ceiling((double)fileLength / pieceSize);
            int fileIdx = 0;
            while (fileIdx < fileCount)
            {
                string newFilePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + "-" + (fileIdx + 1) + Path.GetExtension(filePath));

                long startPos = fileIdx * pieceSize;
                long endPos = startPos + pieceSize;
                if (endPos > fileLength)
                {
                    endPos = fileLength;
                }
                int maxLength = 1024 * 1024;
                long pos = startPos;
                var ws = File.OpenWrite(newFilePath);

                while (pos < endPos)
                {
                    this.showSplitMessage("Processing file " + (fileIdx + 1) + " of " + fileCount + ", progress: " + Math.Round((double)(pos - startPos) / (endPos - startPos), 2));

                    int length = (int)(endPos - pos > maxLength ? maxLength : endPos - pos);
                    byte[] bytes = new byte[length];
                    fs.Position = pos;
                    fs.Read(bytes, 0, length);
                    ws.Position = pos - startPos;
                    ws.Write(bytes, 0, length);
                    ws.Flush();

                    pos += length;
                }

                ws.Close();
                fileIdx++;
            }

            fs.Close();
            this.showSplitMessage("Finished");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.textBox3.Text = this.folderBrowserDialog1.SelectedPath;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.textBox3.Text))
                return;

            ParameterizedThreadStart ts = new ParameterizedThreadStart(this.MergeFile);
            Thread thread = new Thread(ts);
            thread.IsBackground = true;
            thread.Start(this.textBox3.Text);
        }

        private void MergeFile(object obj)
        {
            string folderPath = obj.ToString();
            string[] files = Directory.GetFiles(folderPath);
            if (files.Length == 0)
                return;

            string firstFileName = files[0];
            string fn = Path.GetFileNameWithoutExtension(firstFileName);
            fn = fn.Substring(0, fn.LastIndexOf('-'));
            fn = fn + Path.GetExtension(firstFileName);
            fn = Path.Combine(Path.GetDirectoryName(firstFileName), fn);

            int fileCount = files.Length;
            int fileIdx = 0;
            var ws = File.OpenWrite(fn);
            long writePos = 0;

            while (fileIdx < fileCount)
            {
                string filePath = files[fileIdx];
                var fs = File.Open(filePath, FileMode.Open);
                long fileLength = fs.Length;
                int maxLength = 1024 * 1024;
                long pos = 0;
                while (pos < fileLength)
                {
                    this.showMergeMessage("Processing file " + (fileIdx + 1) + " of " + fileCount + ", progress: " + Math.Round((decimal)(pos) / fileLength, 2));

                    int length = (int)(fileLength - pos > maxLength ? maxLength : fileLength - pos);
                    byte[] bytes = new byte[length];
                    fs.Position = pos;
                    fs.Read(bytes, 0, length);
                    ws.Position = writePos;
                    ws.Write(bytes, 0, length);
                    ws.Flush();

                    pos += length;
                    writePos += length;
                }
                fs.Close();
                fileIdx++;
            }

            ws.Close();
            this.showMergeMessage("Finished");
        }
    }
}
