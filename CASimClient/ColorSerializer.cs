using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Util {
    /// <summary>
    /// A class used to read and write a color scheme to and from an XML
    /// configuration file.
    /// </summary>
    class ColorSerializer {        
        
        /// <summary>
        /// Serialize a color scheme, writing it to a configuration file in XML format
        /// </summary>
        /// <param name="filename">The name of the file to read from</param>
        /// <param name="colorList">The color scheme to serialize</param>
        public static void Serialize(string filename, int[] colorList) {
            // Convert color list to an array of hex strings
            string[] hexColorList = new string[colorList.Length];
            for (int i = 0; i < colorList.Length; i++) hexColorList[i] = intToHex(colorList[i]);

            // Write the strings to a file
            XmlSerializer serializer = new XmlSerializer(typeof(string[]));
            XmlTextWriter writer = new XmlTextWriter(filename, new UnicodeEncoding());
            writer.Formatting = Formatting.Indented;
            serializer.Serialize(writer, hexColorList);
            writer.Flush();
            writer.Close();
        }


        /// <summary>
        /// Deserialize a color scheme, reading from a configuration file in XML format
        /// </summary>
        /// <param name="filename">The name of the file to read from</param>
        /// <returns>A color scheme read from the file, if the file is valid.
        /// Otherwise, null is returned.</returns>
        public static int[] Deserialize(string filename) {
            // Read an array of hex strings from a file
            XmlSerializer deserializer = new XmlSerializer(typeof(string[]));
            XmlTextReader reader = new XmlTextReader(filename);
            string[] hexColorList = null; int[] colorList = null;
            try { hexColorList = (string[])deserializer.Deserialize(reader); }
            catch (InvalidOperationException e) { }

            // Convert the hex strings back to integers
            if (hexColorList != null) {
                colorList = new int[hexColorList.Length];
                for (int i = 0; i < hexColorList.Length; i++) colorList[i] = hexToInt(hexColorList[i]);
            }
            
            reader.Close();
            return colorList;
        }


        /// <summary>
        /// Convert a string containing a hexadecimal value to an int
        /// </summary>
        /// <param name="hexStr">A string containing a hexadecimal value</param>
        /// <returns>An integer with the equivalent value</returns>
        public static int hexToInt(string hexStr) {
            return int.Parse(hexStr, NumberStyles.HexNumber);
        }


        /// <summary>
        /// Convert an int to a hexadecimal string
        /// </summary>
        /// <param name="x">The integer to convert</param>
        /// <returns>A string containing that equivalent hexadecimal value</returns>
        public static string intToHex(int x) {
            return x.ToString("X");
        }
    }
}
