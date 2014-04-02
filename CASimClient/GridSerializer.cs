using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;


namespace Util {
    /// <summary>
    /// Reads and writes the complete state of a grid to and from an XML configuration file.
    /// </summary>
    public class GridSerializer {

        /// <summary>
        /// Serialize a grid, writing it to a configuration file in XML format
        /// </summary>
        /// <param name="filename">The name of the file to write to</param>
        /// <param name="grid">The grid to serialize</param>
        public static void Serialize(string filename, uint[][] grid) {
            XmlSerializer serializer = new XmlSerializer(typeof(int[][]));
            XmlTextWriter writer = new XmlTextWriter(filename, new UnicodeEncoding());
            writer.Formatting = Formatting.Indented;
            serializer.Serialize(writer, grid);
            writer.Flush();
            writer.Close();
        }


        /// <summary>
        /// Deserialize a grid, reading from a configuration file in XML
        /// format, and creating a 2-dimensional grid from it.
        ///
        /// The height of the grid will be the number of rows entered in the
        /// XML file. The width of the grid will be the length of the longest
        /// row in the XML file.
        ///
        /// Any values missing in the XML file (e.g., any rows containing
        /// fewer elements than the longest row) will be assigned the default
        /// state (0).
        /// </summary>
        /// <param name="filename">The name of the file to read from</param>
        /// <returns>A grid representing the data read from the file, if the file
        /// is valid. Otherwise, null is returned.</returns>
        public static uint[][] Deserialize(string filename) {
            XmlSerializer deserializer = new XmlSerializer(typeof(int[][]));
            XmlTextReader reader = new XmlTextReader(filename);
            uint[][] grid = null;

            try {
                uint[][] jaggedGrid = (uint[][])deserializer.Deserialize(reader);

                // Find length of longest row
                int width = 0;
                for (int i = 0; i < jaggedGrid.Length; i++)
                    if (jaggedGrid[i].Length > width) width = jaggedGrid[i].Length;

                // Resize grid to adjust for missing values
                grid = new uint[jaggedGrid.Length][];
                for (int i = 0; i < jaggedGrid.Length; i++) {
                    grid[i] = new uint[width];
                    for (int j = 0; j < jaggedGrid[i].Length; j++) grid[i][j] = jaggedGrid[i][j];
                    // Fill in any missing values with default state
                    for (int j = jaggedGrid[i].Length; j < width; j++) grid[i][j] = 0;
                }
            }
            catch (InvalidOperationException e) { }

            reader.Close();
            return grid;
        }
    }
}
