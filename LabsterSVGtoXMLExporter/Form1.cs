using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Xml;
using System;

namespace LabsterSVGtoXMLExporter
{
    class ItemSvdData
    {
        public string GUIScreenHeight { get; private set; }
        public string GUIScreenWidth { get; private set; }
        public List<XMLPageData> ListPageData = new List<XMLPageData>();

        public ItemSvdData(XmlNode node)
        {
            GUIScreenHeight = node.Attributes["height"].Value;
            GUIScreenWidth = node.Attributes["width"].Value;

            var xmllist = node.ChildNodes;
            int _pageCount = 0;
            foreach (XmlNode xmlNode in xmllist)
            {

                if (xmlNode.Name.Contains("g"))
                {

                    var listXMLData = new List<XMLData>();
                    foreach (XmlNode nod in xmlNode.ChildNodes)
                    {

                        if (nod.Name == "rect")
                        {
                            var width = nod.Attributes["width"].Value;
                            var height = nod.Attributes["height"].Value;
                            var x = calculatedPos(nod.Attributes["x"].Value, GUIScreenWidth);
                            var y = calculatedPos(nod.Attributes["y"].Value, GUIScreenHeight);
                            var color = FormatColor(nod.Attributes["style"].Value);
                            var item = new XMLImageData(x, y, width, height, color);
                            listXMLData.Add(item);
                        }

                        if (nod.Name == "text")
                        {
                            var x = calculatedPos(nod.Attributes["x"].Value, GUIScreenWidth);
                            var y = calculatedPos(nod.Attributes["y"].Value, GUIScreenHeight);
                            var text = nod.FirstChild.InnerText;
                            var fontSize = CalculateFontFormat(nod.Attributes["style"].Value, "font-size");
                            var fontFamily = CalculateFontFormat(nod.Attributes["style"].Value, "font-family");
                            listXMLData.Add(new XMLTextData(x, y, fontSize, fontFamily, text));
                        }


                    }
                    var page = new XMLPageData(_pageCount.ToString(), listXMLData);
                    ListPageData.Add(page);
                    _pageCount++;
                }
            }


        }

        private string calculatedPos(string pos, string dim)
        {
            string retVal;
            var valPos = GetDouble(pos, 0);
            var valDim = GetDouble(dim, 0);

            var calculated = valPos - (valDim / 2);
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            retVal = calculated.ToString(nfi);

            return retVal;
        }

        private string FormatColor(string val)
        {
            string retVal = "";

            var splits = val.Split(';');
            string[] colorStrings = new string[4];
            foreach (string s in splits)
            {

                if (s.Contains("opacity"))
                {
                    var index = s.IndexOf(':') + 1;
                    var sub = s.Substring(index);
                    float x = float.Parse(sub, NumberStyles.Any, CultureInfo.InvariantCulture);
                    float value = x * 255;
                    int opacity = (int)value;
                    colorStrings[3] = opacity.ToString();
                    break;
                }
                if (s.Contains("fill"))
                {
                    var index = s.IndexOf(':') + 1;
                    var sub = s.Substring(index);
                    Color color = ColorTranslator.FromHtml(sub);
                    colorStrings[0] = color.R.ToString();
                    colorStrings[1] = color.G.ToString();
                    colorStrings[2] = color.B.ToString();

                    continue;
                }

            }

            retVal = string.Join(",", colorStrings);

            return retVal;
        }

        private double GetDouble(string value, double defaultValue)
        {
            double result;

            double.TryParse(value, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out result);

            return result;
        }

        private string CalculateFontFormat(string val, string format)
        {
            string retVal = "";

            var value = val.Split(';');

            foreach (string v in value)
            {
                if (v.Contains(format))
                {

                    if (format.Equals("font-size"))
                    {
                        var index = v.IndexOf(':') + 1;
                        var vSub = v.Substring(index).Remove(2, 2);
                        retVal = vSub;
                        break;
                    }
                    else if (format.Equals("font-family"))
                    {
                        var index = v.IndexOf(':') + 1;
                        var vSub = v.Substring(index).Remove(2, 2);
                        retVal = vSub;
                        break;
                    }

                }

            }

            return retVal;
        }
    }

    class XMLData
    { }


    class XMLPageData : XMLData
    {
        public string Id;
        public List<XMLData> ListData;

        public XMLPageData(string _Id, List<XMLData> data)
        {
            Id = _Id;
            ListData = data;
        }
    }

    class XMLTextData : XMLData
    {
        public string x;
        public string y;
        public string fontSize;
        public string fontFamily;
        public string text;

        public XMLTextData(string _x, string _y, string _fontSize, string _fontFamily, string _text)
        {
            x = _x;
            y = _y;
            fontSize = _fontSize;
            fontFamily = _fontFamily;
            text = _text;
        }
    }

    class XMLImageData : XMLData
    {
        public string x;
        public string y;
        public string width;
        public string height;
        public string color;

        public XMLImageData(string _x, string _y, string _width, string _height, string _color)
        {
            x = _x;
            y = _y;
            width = _width;
            height = _height;
            color = _color;
        }
    }

    public partial class Form1 : Form
    {
        private ItemSvdData _fileContent;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.button1.Click += Button1_Click;
            this.button2.Click += Button2_Click;
            this.textBox1.KeyUp += TextBox1_KeyUp;
        }

        private void TextBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.A)
            {
                ((TextBox)sender).SelectAll();
            }
        }

        private void Button1_Click(object sender, System.EventArgs e)
        {
            Console.WriteLine("button click to upload svg clicked");
            OpenSVGFile();
        }

        private void Button2_Click(object sender, System.EventArgs e)
        {
            Console.WriteLine("button to parse labster xml clicked");
            ConvertsSVGToLabsterXML();
        }

        private void OpenSVGFile()
        {
            ResetValue();
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "*svg files (.svg)|*.svg";
                openFileDialog.RestoreDirectory = false;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Console.WriteLine(openFileDialog.FileName);
                    var filestream = openFileDialog.OpenFile();
                    using (StreamReader reader = new StreamReader(filestream))
                    {
                        string file;
                        file = reader.ReadToEnd();

                        if (file != null)
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(file);
                            _fileContent = new ItemSvdData(doc.DocumentElement);
                        }

                    }

                }
            }



        }

        private void ConvertsSVGToLabsterXML()
        {
            //XmlDocument doc = new XmlDocument();
            //doc.LoadXml(_fileContent);

            if (_fileContent == null) return;

            XmlWriterSettings setting = new XmlWriterSettings();
            setting.CloseOutput = false;
            setting.Encoding = System.Text.Encoding.UTF8;

            MemoryStream stream = new MemoryStream();
            using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings()))
            {
                writer.WriteStartDocument();

                //GUISCREEN
                writer.WriteStartElement("GUIScreen");
                writer.WriteAttributeString("Id", "");
                writer.WriteAttributeString("Name", "");
                writer.WriteAttributeString("AtlasPath", "MachineUIAtlas");
                writer.WriteAttributeString("FontPath", "montserratregular");
                writer.WriteAttributeString("Depth", "-1");
                writer.WriteAttributeString("Align", "Center");
                writer.WriteAttributeString("Resolution", FormatVector2(_fileContent.GUIScreenWidth, _fileContent.GUIScreenHeight));

                foreach (XMLData page in _fileContent.ListPageData)
                {
                    var pageVar = (XMLPageData)page;
                    writer.WriteStartElement("GUIPage");
                    writer.WriteAttributeString("Id", "Page_" + pageVar.Id);

                    int x = 0;
                    foreach (XMLData pageData in pageVar.ListData)
                    {
                        var dataImage = pageData as XMLImageData;
                        if (dataImage != null)
                        {
                            writer.WriteStartElement("GUIImage");
                            writer.WriteAttributeString("Id", "Image_" + x.ToString());
                            writer.WriteAttributeString("Size", FormatVector2(dataImage.width, dataImage.height));
                            writer.WriteAttributeString("Position", FormatVector2(dataImage.x, dataImage.y));
                            writer.WriteAttributeString("Color", dataImage.color);
                            writer.WriteAttributeString("Depth", "1");
                            writer.WriteAttributeString("Align", "TopLeft");
                            writer.WriteAttributeString("ImageSource", "block_white");
                            writer.WriteEndElement();
                            x++;
                        }

                        var dataText = pageData as XMLTextData;
                        if (dataText != null)
                        {
                            writer.WriteStartElement("GUILabel");
                            writer.WriteAttributeString("Id", "Label_" + x.ToString());
                            writer.WriteAttributeString("Size", FormatVector2("500", dataText.fontSize));
                            writer.WriteAttributeString("Position", FormatVector2( (float.Parse(dataText.x) - float.Parse(dataText.fontSize)).ToString(), dataText.y));
                            writer.WriteAttributeString("Depth", "2");
                            writer.WriteAttributeString("Align", "Left");
                            writer.WriteAttributeString("Text", dataText.text);
                            writer.WriteAttributeString("TextSize", dataText.fontSize);
                            writer.WriteAttributeString("Color", "Black");
                            writer.WriteEndElement();
                            x++;
                        }
                    }

                    writer.WriteEndElement();
                }


                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
                stream.Position = 0;

            }
            StreamReader reader = new StreamReader(stream);
            PrintToTextBox(reader.ReadToEnd());
            stream.Dispose();
        }

        private string FormatVector2(string x, string y)
        {
            return x + ',' + y;
        }

        private void PrintToTextBox(string val)
        {
            textBox1.Text = val;
        }

        private void ResetValue()
        {
            _fileContent = null;
            textBox1.Text = "";
        }
    }
}
