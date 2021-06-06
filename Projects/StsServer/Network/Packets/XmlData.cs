// Copyright (c) Arctium.

using System.Text;
using System.Xml;

namespace StsServer.Network.Packets
{
    class XmlData
    {
        XmlWriter xmlWriter;
        StringBuilder builder;

        public XmlData()
        {
            var xmlWriterSettings = new XmlWriterSettings();

            xmlWriterSettings.NewLineChars = "\n";
            xmlWriterSettings.Indent = false;
            xmlWriterSettings.OmitXmlDeclaration = true;

            builder = new StringBuilder();
            xmlWriter = XmlWriter.Create(builder, xmlWriterSettings);

            xmlWriter.WriteStartDocument();
        }

        public void WriteElementRoot(string name)
        {
            xmlWriter.WriteStartElement(name);
            xmlWriter.WriteRaw("\n");
        }

        public void WriteElement(string name, string data, bool newLine = true)
        {
            xmlWriter.WriteElementString(name, data);

            if (newLine)
                xmlWriter.WriteRaw("\n");
        }

        public void WriteAttribute(string attribute, string data)
        {
            xmlWriter.WriteAttributeString(attribute, data);
        }

        public void WriteCustom(string data)
        {
            xmlWriter.WriteRaw(data);
        }

        public override string ToString()
        {
            //xmlWriter.WriteEndElement();
            //xmlWriter.WriteEndDocument();

            xmlWriter.Flush();
            xmlWriter.Close();

            return builder.ToString();
        }
    }
}
