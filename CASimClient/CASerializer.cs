using CASimService;
using System;
using System.Text;
using System.Xml;
using System.Xml.Serialization;


namespace Util {
    /// <summary>
    /// A class used to read and write a CA and its paramters to and from an XML
    /// configuration file.
    /// </summary>
    class CASerializer {
        /// <summary>
        /// Serialize a CA, writing it to a configuration file in XML format
        /// </summary>
        /// <param name="filename">The name of the file to read from</param>
        /// <param name="ca">The CA to serialize</param>
        public static void Serialize(string filename, CA ca) {
            XmlSerializer serializer = new XmlSerializer(typeof(CA));
            XmlTextWriter writer = new XmlTextWriter(filename, new UnicodeEncoding());
            writer.Formatting = Formatting.Indented;
            serializer.Serialize(writer, ca);
            writer.Flush();
            writer.Close();
        }


        /// <summary>
        /// Deserialize a CA, reading from a configuration file in XML format
        /// </summary>
        /// <param name="filename">The name of the file to read from</param>
        /// <returns>A CA representing the data read from the file, if the file
        /// is valid. Otherwise, null is returned.</returns>
        public static CA Deserialize(string filename) {
            XmlSerializer deserializer = new XmlSerializer(typeof(CA));
            XmlTextReader reader = new XmlTextReader(filename);
            CA ca = null;
            try { ca = (CA)deserializer.Deserialize(reader); }
            catch (InvalidOperationException e) { }
            if (ca != null) ca.reverseNeighborhood();

            reader.Close();
            return ca;
        }
    }
}
