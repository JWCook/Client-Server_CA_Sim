using CASimService;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CASimClient {
    /// <summary>
    /// Draws a cell grid using a (semi-)optimized bitmap which redraws only the portions that change in between frames.
    /// After being initialized, cells may be drawn on the bitmap using:
    /// setPixel() for an individual cell
    /// setPixels() for a LinkedList or array of cells
    /// setGrid() for a full grid
    /// </summary>
    class GridBitmap {
        // Bring in some Win32 memory management junk to simplify bitmap operations
        #region Memory Management
        private const uint FILE_MAP_ALL_ACCESS = 0xF001F;
        private const uint PAGE_READWRITE = 0x04;
        private IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes,
            uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess,
            uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern void MoveMemory(IntPtr Destination, IntPtr Source, int Length);
        #endregion

        private int undefinedColor = 0x000000;
        private PixelFormat format = PixelFormats.Bgr32;
        private int gridSizeX, gridSizeY;
        private uint numPixels, numBytes;
        private InteropBitmap bmpSource;
        private IntPtr map;
        private int[] colorList;
        private unsafe int* vptr;

        public InteropBitmap Source {
            get { lock (bmpSource) { bmpSource.Invalidate(); return bmpSource; } }
        }
        public int[] ColorList {
            get { return ColorList; }
            set { colorList = value; }
        }


        /// <summary>
        /// Initialize a new GridBitmap with the given dimensions and color range.
        /// </summary>
        /// <param name="x">The width of the bitmap (in pixels)</param>
        /// <param name="y">The height of the bitmap (in pixels)</param>
        /// <param name="colors">An array containing RGB values indicating how states should be colored. Array size
        /// should correspond to the number of states; a cell in state i will be colored with the RGB value in colors[i].
        /// If a color for a given state is undefined, undefinedColor will be used.</param>
        public GridBitmap(int x, int y, uint defaultState, int[] colors) {
            gridSizeX = x;
            gridSizeY = y;
            colorList = colors;

            numPixels = (uint)(gridSizeX * gridSizeY);
            numBytes = numPixels * 4;
            int stride = (gridSizeX * format.BitsPerPixel + 7) / 8;

            // Make a memory mapping for the bitmap
            map = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, PAGE_READWRITE, 0, numBytes, null);

            // Initialize a blank bitmap (using the color specified for the default state)
            unsafe {
                vptr = (int*)MapViewOfFile(map, FILE_MAP_ALL_ACCESS, 0, 0, numBytes).ToPointer();
                Parallel.For(0, gridSizeY, j => {
                    int rowStart = j * gridSizeX;
                    for (int i = 0; i < gridSizeX; i++) vptr[rowStart+i] = colors[defaultState];
                });
            }
            
            // Create a bitmap source using the memory map
            bmpSource = (InteropBitmap)Imaging.CreateBitmapSourceFromMemorySection(map, gridSizeX, gridSizeY, format, stride, 0);
        }


        /// <summary>
        /// Refresh the bitmap and free previously allocated memory
        /// </summary>
        public void refreshBMP() {
            lock (bmpSource) bmpSource.Invalidate();
        }


        /// <summary>
        /// Set an individual pixel to a new color
        /// </summary>
        /// <param name="x">The Cell containing the coordinates and new state
        /// </param>
        public void setPixel(Cell cell) {
            lock (bmpSource) {
                unsafe {
                    int newColor = (cell.state >= colorList.Length) ? undefinedColor : colorList[cell.state];
                    int pixelIndex = cell.X+(((gridSizeY-1)-cell.Y)*gridSizeX);
                    if (pixelIndex < 0) pixelIndex = 0;
                    vptr[pixelIndex] = newColor;
                }
            }

            GC.Collect(1);
        }


        /// <summary>
        /// Update portions of the bitmap given a LinkedList of cells to be updated
        /// </summary>
        /// <param name="toUpdate">A list of newly updated cells and their new states</param>
        public void setPixels(LinkedList<Cell> toUpdate) {
            lock (bmpSource) {
                unsafe {
                    int newColor, pixelIndex;
                    foreach (Cell next in toUpdate) {
                        newColor = (next.state > colorList.Length) ? undefinedColor : colorList[next.state];
                        pixelIndex = next.X+(((gridSizeY-1)-next.Y)*gridSizeX);
                        vptr[pixelIndex] = newColor;
                    }
                    toUpdate.Clear();
                }
            }

            GC.Collect(1);
        }


        /// <summary>
        /// Update portions of the bitmap given an array of cells to be updated
        /// </summary>
        /// <param name="toUpdate">A list of newly updated cells and their new states</param>
        public void setPixels(Cell[] toUpdate) {
            lock (bmpSource) {
                unsafe {
                    int newColor, pixelIndex;
                    foreach (Cell next in toUpdate) {
                        newColor = (next.state > colorList.Length) ? undefinedColor : colorList[next.state];
                        pixelIndex = next.X+(((gridSizeY-1)-next.Y)*gridSizeX);
                        vptr[pixelIndex] = newColor;
                    }
                }
            }

            GC.Collect(1);
        }


        /// <summary>
        /// Set this bitmap to the contents of the specified grid. The grid must be the same size as the current bitmap.
        /// </summary>
        /// <param name="grid"></param>
        public void setGrid(uint[][] grid) {
            if (grid.Length != gridSizeX || grid[0].Length != gridSizeY) return;

            lock (bmpSource) {
                unsafe {
                    int newColor;
                    for (int j = 0; j < gridSizeY; j++) {
                        int rowStart = gridSizeX * ((gridSizeY - 1) - j);
                        for (int i = 0; i < grid.Length; i++) {
                            newColor = (grid[i][j] > colorList.Length) ? undefinedColor : colorList[grid[i][j]];
                            vptr[rowStart + i] = newColor;
                        }
                    }
                }
            }

            GC.Collect(1);
        }
    }
}
