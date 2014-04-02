using CASimClient.CAServiceReference;
using CASimService;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Util;

namespace CASimClient {
    /// <summary>
    /// Main window interaction logic and main client application responsible
    /// for handling communiation with the server. When a simulation is started,
    /// the server will run continuously until stopped and add updates to a 
    /// queue on the server, divided into "frames" of the simulation. While the
    /// simulation is running, the work is split up into the following threads:
    /// 
    /// -retrieveUpdatesThread retrieves any available updates from the server
    ///  and adds them to the toUpdate queue. Upon starting it signals the
    ///  server to begin simulation, and upon ending it signals the server to
    ///  end simulation, and pulls any remaining updates from the server.
    /// 
    /// -updateGridThread pulls updates from the toUpdate queue and updates
    /// the bitmap used by the client to display the grid. Refreshing the UI
    /// takes a (proportionally) significant amount of time, and is not handled
    /// by this thread unless frame skipping is disabled (e.g., if the user
    /// wants every single frame to be displayed sequentially). Disabling frame
    /// skipping will generally decrease performance.
    /// 
    /// -updateUiThread refreshes the UI according to a user-adjustable refresh
    /// rate, if frame skipping is enabled. By default this rate is ~30 frames
    /// per second, or one refresh every 33 milliseconds. If the simulation runs
    /// faster than this rate, some frames will be skipped.
    /// </summary>
    public partial class CAClientWindow : Window {
        private bool skipFrames = true;             // Determines if frame skipping is allowed
        private uint defaultState;                  // Default state to initialize cells to
        private int generation;                     // Current generation
        private int gridSizeX, gridSizeY;           // Size of grid (in cells)
        private int[] colorList;                    // Colors used to display cell states
        private CA ca;                              // Current CA being used
        private GridBitmap gridBMP;                 // Bitmap used to display the grid
        private CAServiceClient simClient;          // Client used to connect to the simulation server
        private ConcurrentQueue<Cell[]> toUpdate;   // Collection of cells to update the display with

        #region Threading variables
        private const int defaultRefreshRate = 100; // Default period of updateUI (33 ms ~= 30 FPS)
        private const int maxUpdateRate = 33;       // Max period of updateGridThread refresh (in ms)
        private const int speedupThresh = 2;        // updateGridThread will speed up if toUpdate reaches this size
        private const int simWaitThresh = 100;      // runSimulationThread will wait if toUpdate reaches this size
        private int refreshRate = 100;              // User-adjustable of updateUI (33 ms ~= 30 FPS)
        private int updateRate;                     // Auto-adjustable period of updateGridThread refresh (in ms)
        private ManualResetEventSlim stopEvent;     // Used to signal a running simulation run thread to stop
        private Thread stepSimulationThread;        // Thread responsible for running one simulation step
        private Thread retrieveUpdatesThread;       // Thread responsible for fetching updates from the simulation server
        private Thread updateGridThread;            // Thread responsible for updating the grid using toUpdate
        private Thread updateUiThread;              // Thread responsible for refreshing the UI (if skipping frames)
        #endregion


        public CAClientWindow() {
            InitializeComponent();
            defaultState = 0;
            generation = 0;
            toUpdate = new ConcurrentQueue<Cell[]>();

            // Configure a client to connect to the simulation server
            simClient = new CAServiceClient();
            foreach (OperationDescription op in simClient.Endpoint.Contract.Operations) {
                DataContractSerializerOperationBehavior dataContractBehavior = (DataContractSerializerOperationBehavior)op.Behaviors.Find<DataContractSerializerOperationBehavior>();
                if (dataContractBehavior != null) dataContractBehavior.MaxItemsInObjectGraph = Int32.MaxValue;
            }

            // Attempt to open a connection to the server
            try {simClient.Open(); }
            catch (CommunicationException e) {
                MessageBox.Show("Error: could not connect to CA Server", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
            }

            // Initialize grid display
            RenderOptions.SetBitmapScalingMode(gridImage, BitmapScalingMode.NearestNeighbor);
            gridSizeX = gridSizeY = 500;
            getDefaultColors(16);
            gridBMP = new GridBitmap(gridSizeX, gridSizeY, defaultState, colorList);
            gridImage.Source = gridBMP.Source;

            // Start grid-updating threads
            updateRate = maxUpdateRate;
            updateGridThread = new Thread(new ThreadStart(gridUpdater));
            updateUiThread = new Thread(new ThreadStart(UIUpdater));
            updateGridThread.Start();
            updateUiThread.Start();

            // Load a test CA
           /*ca = CASerializer.Deserialize("CA - Game of Life.xml");
            if (simClient.initCA(ca)) {
                stepButton.IsEnabled = true;
                runButton.IsEnabled = true;
                saveCAMenuItem.IsEnabled = true;
            }*/
        }


        #region Window interaction
        /// <summary>
        /// When a cell is left-clicked, increment its state, updating both the 
        /// simulation and the display. For a right-click, reset the cell to
        /// the default state.
        /// </summary>
        private void clickCell(object sender, MouseButtonEventArgs e) {
            // Find the cell clicked
            CPoint cell = getCellClicked(e);
            uint state = 0;

            // For a left click, increment the state
            if (e.ChangedButton == MouseButton.Left) {
                state = simClient.getCellState(cell) + 1;
                if (state >= simClient.getNumStates()) state = 0;
            }

            // For a right click, reset to the default state
            else if (e.ChangedButton == MouseButton.Right) state = defaultState;

            // Update the simulation grid and the grid display
            simClient.setCellState(cell, state);
            simClient.updateGrid(false);
            lock (gridBMP) {
                gridBMP.setPixel(new Cell(cell, state));
                gridBMP.refreshBMP();
            }
            statusTextBlock.Text = "Cell at (" + cell.X + "," + cell.Y + ") changed to state " + state;
        }


        /// <summary>
        /// Clear the contents of the grid
        /// </summary>
        private void clear(object sender, RoutedEventArgs e) {
            initDisplay(gridSizeX, gridSizeY);
            simClient.initGridBlank(gridSizeX, gridSizeY);
            generation = 0;
            generationTextBlock.Text = "Generation: 0";
            statusTextBlock.Text = "Grid cleared";
        }


        /// <summary>
        /// Display a dialog to adjust the refresh rate of the UI updater,
        /// or to enable/disable frame skipping
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editFrameRate(object sender, RoutedEventArgs e) {
            EditFramerateDialog editFramerateDialog = new EditFramerateDialog(refreshRate, skipFrames);
            if (editFramerateDialog.ShowDialog() == true) {
                refreshRate = editFramerateDialog.RefreshRate;
                bool result = editFramerateDialog.SkipFrames;
                if (skipFrames == true && result == false) disableFrameSkipping();
                else if (skipFrames == false && result == true) enableFrameSkipping();
            }
        }


        /// <summary>
        /// Exit the program
        /// </summary>
        private void exit(object sender, EventArgs e) {
            // End display updater threads
            updateGridThread.Abort();
            updateUiThread.Abort();
            updateGridThread.Join();
            updateUiThread.Join();

            // End the simulation, if it is running
            if (retrieveUpdatesThread != null && retrieveUpdatesThread.IsAlive) {
                stopEvent.Set();
                retrieveUpdatesThread.Join();
            }

            // Close connection to CA server and exit
            if (simClient.State == CommunicationState.Opened) simClient.Close();
            Application.Current.Shutdown();
        }


        /// <summary>
        /// Open a window to load CA parapeters and, if successful, use it in
        /// the current simulation
        /// </summary>
        private void loadCA(object sender, RoutedEventArgs e) {
            // Open CA loader window
            CALoaderWindow caLoaderWindow = new CALoaderWindow(ca);
            caLoaderWindow.Owner = this;
            caLoaderWindow.ShowDialog();

            // If a CA was successfully compiled, use it in the current simulation
            if (caLoaderWindow.CompiledCA != null) {
                // Send the uncompiled CA and let the server recompile (can't serialize an assembly)
                ca = caLoaderWindow.CompiledCA.copyCA();
                simClient.initCA(ca);

                // If not enough colors are defined for the new CA, generate more
                if (simClient.getNumStates() > colorList.Length) getDefaultColors(simClient.getNumStates());

                // Update default state (will not be immediately applied to grid unless it needs to be cleared)
                defaultState = ca.DefaultState;

                // If an incompatible CA has previously been run, clear the grid
                bool clearedGrid = false;
                if (clearedGrid = (simClient.getNumStates() > caLoaderWindow.CompiledCA.NumStates))
                    initDisplay(gridSizeX, gridSizeY);

                // Update controls
                enableMenuItems();   
                generation = 0;
                generationTextBlock.Text = "Generation: 0";
                statusTextBlock.Text = "New cellular automata loaded";
                if (clearedGrid) statusTextBlock.Text += " (grid cleared)";
            }
        }


        /// <summary>
        /// Open a dialog to load a color scheme from a file
        /// </summary>
        private void loadColors(object sender, RoutedEventArgs e) {
            OpenFileDialog openColorsDialog = new OpenFileDialog();
            openColorsDialog.Filter = "Color Scheme File (.xml)|*.xml|All Files|*.*";

            // Open selected file and process results
            if (openColorsDialog.ShowDialog() == true) {
                int[] colors = ColorSerializer.Deserialize(openColorsDialog.FileName);

                // If there aren't enough colors, ignore the file and display an error message
                if (colors != null && colors.Length < ca.NumStates) {
                    string msg = "Error: Only " + colors.Length + " colors defined in this file.";
                    msg +=  " At least " + ca.NumStates + " colors are required for the current CA.";
                    MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // If the file was valid, load the new color scheme
                else if (colors != null) {
                    colorList = colors;
                    lock (gridBMP) gridBMP.ColorList = colors;
                    statusTextBlock.Text = "New color scheme loaded (" + colors.Length + " colors)";

                    // If it has been modified, redraw it; otherwise just clear it
                    if (generation > 0) {
                        uint[][] grid = simClient.getGrid();
                        lock (gridBMP) {
                            gridBMP.setGrid(grid);
                            gridBMP.refreshBMP();
                        }
                    }
                    else clear(null, null);
                }

                // If the file was invalid, ignore it and display an error message
                else MessageBox.Show("Error: invalid color scheme file", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        /// <summary>
        /// Open a dialog to load a saved grid from a file
        /// </summary>
        private void loadGrid(object sender, RoutedEventArgs e) {
            OpenFileDialog openGridDialog = new OpenFileDialog();
            openGridDialog.Filter = "Grid File (.xml)|*.xml|All Files|*.*";

            // Open selected file and process results
            if (openGridDialog.ShowDialog() == true) {
                uint[][] grid = GridSerializer.Deserialize(openGridDialog.FileName);

                // If the file was valid, load the new grid
                if (grid != null) {
                    setGrid(grid);
                    statusTextBlock.Text = "New grid loaded (" + grid.Length + "x" + grid[0].Length + ")";
                }

                // Otherwise, ignore it and display an error message
                else MessageBox.Show("Error: invalid grid file", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        /// <summary>
        /// Re-generate a set of colors big enough for the current CA
        /// </summary>
        private void reGenerateColors(object sender, RoutedEventArgs e) {
            getDefaultColors(simClient.getNumStates());
        }


        /// <summary>
        /// Open a dialog to resize the grid. Any modifications to the grid will
        /// be preserved, within the bounds of the new grid size.
        /// </summary>
        private void resize(object sender, RoutedEventArgs e) {
            GridResizeDialog resizeGridDialog = new GridResizeDialog();
            if (resizeGridDialog.ShowDialog() == true) {
                int newX = resizeGridDialog.X;
                int newY = resizeGridDialog.Y;

                // Create a new grid, copying over old contents where possible
                uint[][] oldGrid = simClient.getGrid();
                uint[][] grid = new uint[newX][];
                int xLimit = (newX <= gridSizeY) ? newX : gridSizeX;
                int yLimit = (newY <= gridSizeY) ? newY : gridSizeY;
                for (int i = 0; i < xLimit; i++) {
                    grid[i] = new uint[newY];
                    // Copy over old contents of this column, if possible
                    for (int j = 0; j < yLimit; j++) grid[i][j] = oldGrid[i][j];
                    // Fill in any remaining cells in this column with default state
                    for (int j = yLimit; j < newY; j++) grid[i][j] = 0;
                }
                // Fill in any remaining columns with default state
                for (int i = xLimit; i < newX; i++) {
                    grid[i] = new uint[newY];
                    for (int j = 0; j < newX; j++) grid[i][j] = 0;
                }

                // Reinitialize grid
                setGrid(grid);
                statusTextBlock.Text = "Grid resized to " + newX + "x" + newY;
            }
        }


        /// <summary>
        /// Either start or start the simulation, depending on whether it is
        /// already running or not, and update controls appropriately
        /// </summary>
        private void runStop(object sender, RoutedEventArgs e) {
            // Start the simulation thread if it is currently stopped
            if (retrieveUpdatesThread == null || !retrieveUpdatesThread.IsAlive) {
                coordinatesTextBlock.Text = "Cell editing disabled";
                statusTextBlock.Text = "Simulation running";
                runButton.Content = "Stop";
                runMenuItem.Header = "Stop";
                disableMenuItems();
                updateRate = 0;
                stopEvent = new ManualResetEventSlim(false);
                retrieveUpdatesThread = new Thread(new ThreadStart(updateRetriever));
                retrieveUpdatesThread.Start();
            }

            // Signal simulation thread to stop if it is currently started
            else {
                stopEvent.Set();
                retrieveUpdatesThread.Join();
                stopEvent.Dispose();
                updateRate = maxUpdateRate;
                enableMenuItems();
                coordinatesTextBlock.Text = "Coordinates: (0,0)";
                statusTextBlock.Text = "Simulation stopped";
                runButton.Content = "Run";
                runMenuItem.Header = "Run";
            }
        }


        /// <summary>
        /// Save the current CA to a file
        /// (menu option will not be activated until a CA is successfully compiled)
        /// </summary>
        private void saveCA(object sender, RoutedEventArgs e) {
            saveFile("Cellular Automata");
        }


        /// <summary>
        /// Save the current color configuration to a file
        /// </summary>
        private void saveColors(object sender, RoutedEventArgs e) {
            saveFile("Color Scheme");
        }


        /// <summary>
        /// Save the current grid to a file
        /// </summary>
        private void saveGrid(object sender, RoutedEventArgs e) {
            saveFile("Grid");
        }


        /// <summary>
        /// Step through one iteration of the simulation
        /// </summary>
        private void step(object sender, RoutedEventArgs e) {
            // Wait for a currently executing stepSimulationThread to finish
            if (stepSimulationThread != null) stepSimulationThread.Join();

            // Spawn a new thread to step through the simulation once
            stepButton.IsEnabled = false;
            stepSimulationThread = new Thread(new ThreadStart(simulationStepRunner));
            stepSimulationThread.Start();
        }


        /// <summary>
        /// Update the coordinates in the statusbar upon mouseover of a cell
        /// </summary>
        private void updateCoords(object sender, MouseEventArgs e) {
            CPoint cell = getCellClicked(e);
            coordinatesTextBlock.Text = "Coordinates: (" + cell.X + "," + cell.Y + ")";
        }


        /// <summary>
        /// Zoom out
        /// </summary>
        private void zoomLarger(object sender, RoutedEventArgs e) {
            zoomSlider.Value += zoomSlider.LargeChange;
        }


        /// <summary>
        /// Zoom in
        /// </summary>
        private void zoomSmaller(object sender, RoutedEventArgs e) {
            zoomSlider.Value -= zoomSlider.LargeChange;
        }
        #endregion


        #region Miscellaneous helpers
        /// <summary>
        /// Disable menu items while simulation is running
        /// </summary>
        private void disableMenuItems() {
            gridImage.IsEnabled = false;
            stepButton.IsEnabled = false;
            clearButton.IsEnabled = false;
            runMenuItem.IsEnabled = false;
            stepMenuItem.IsEnabled = false;
            clearMenuItem.IsEnabled = false;
            resizeMenuItem.IsEnabled = false;
            loadCAMenuItem.IsEnabled = false;
            saveCAMenuItem.IsEnabled = false;
            loadGridMenuItem.IsEnabled = false;
            saveGridMenuItem.IsEnabled = false;
            genColorMenuItem.IsEnabled = false;
            loadColorMenuItem.IsEnabled = false;
            saveColorMenuItem.IsEnabled = false;
            framerateOptionsMenuItem.IsEnabled = false;
        }


        /// <summary>
        /// Re-enable menu items when simulation is done running
        /// </summary>
        private void enableMenuItems() {
            runButton.IsEnabled = true;
            gridImage.IsEnabled = true;
            stepButton.IsEnabled = true;
            clearButton.IsEnabled = true;
            runMenuItem.IsEnabled = true;
            stepMenuItem.IsEnabled = true;
            clearMenuItem.IsEnabled = true;
            resizeMenuItem.IsEnabled = true;
            loadCAMenuItem.IsEnabled = true;
            saveCAMenuItem.IsEnabled = true;
            loadGridMenuItem.IsEnabled = true;
            saveGridMenuItem.IsEnabled = true;
            genColorMenuItem.IsEnabled = true;
            loadColorMenuItem.IsEnabled = true;
            saveColorMenuItem.IsEnabled = true;
            framerateOptionsMenuItem.IsEnabled = true;
        }


        /// <summary>
        /// Calculate the cell corresponding to the location clicked
        /// </summary>
        /// <param name="e">The mouse button event containing the location clicked</param>
        /// <returns>A Point representing the cell clicked</returns>
        private CPoint getCellClicked(MouseEventArgs e) {
            // Find factors to adjust for scaling and height/width ratio
            double cellRatioX = gridImage.Width / gridSizeX;
            double cellRatioY = gridImage.Height / gridSizeY;
            double adjustX = (gridSizeX >= gridSizeY) ? 1 : gridSizeY / gridSizeX;
            double adjustY = (gridSizeY >= gridSizeX) ? 1 : gridSizeX / gridSizeY;

            // Get relative location clicked and adjust
            System.Windows.Point clickPos = e.GetPosition(gridImage);
            int cellX = System.Convert.ToInt32(Math.Ceiling((clickPos.X / cellRatioX)*adjustX)) - 1;
            int cellY = (gridSizeY+1) - System.Convert.ToInt32(Math.Ceiling((clickPos.Y / cellRatioY)*adjustY)) - 1;
            return new CPoint(cellX, cellY);
        }


        /// <summary>
        /// Generate a list of colors to use. The colors are chosen to be
        /// evenly spaced in the color spectrum.
        /// </summary>
        private void getDefaultColors(int numColors) {
            // Find the number of possibilities for each primary color (numColors^(1/3))
            int numColorVals = Convert.ToInt32(Math.Ceiling(Math.Pow(numColors-1, 1.0/3.0)));

            // Find the possible values for each primary color
            int increment = 255 / numColorVals;
            int[] colorVals = new int[numColorVals];
            for (int i = 0; i < numColorVals; i++) colorVals[i] = i*increment;

            // Generate a color list
            colorList = new int[numColors];
            colorList[0] = 0xFFFFFF;
            int r = 0, g = 0, b = 0;
            for (int i = 1; i < numColors; i++) {
                // Add a new color to the list
                colorList[i] = (getRGB(colorVals[r], colorVals[g], colorVals[b]));

                // Choose new RGB values
                r++;
                if (r >= numColorVals) { r = 0; g++; }
                if (g >= numColorVals) { g = 0; b++; }
                if (b >= numColorVals) { b = 0; }       // Should never happen
            }
        }


        /// <summary>
        /// Given 3 separate color values, return a single RGB value
        /// </summary>
        /// <param name="r">Red value (0-255)</param>
        /// <param name="g">Green value (0-255)</param>
        /// <param name="b">Blue value (0-255)</param>
        /// <returns>An RGB value with the specified color values</returns>
        private int getRGB(int r, int g, int b) {
            return (r << 16) | (g << 8) | (b << 0);
        }


        /// <summary>
        /// Initialize the grid display to a new bitmap of the specified size
        /// </summary>
        /// <param name="x">The width of the new grid</param>
        /// <param name="y">The height of the new grid</param>
        private void initDisplay(int x, int y) {
            lock (gridBMP) {
                gridBMP = new GridBitmap(x, y, defaultState, colorList);
                gridImage.Source = gridBMP.Source;
                gridBMP.refreshBMP();
            }
        }


        /// <summary>
        /// Save the specified information to a file
        /// </summary>
        /// <param name="type">The type of information to save; can be either
        /// Cellular Automata, Color Scheme, or Grid</param>
        private void saveFile(string type) {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = type + " File (.xml)|*.xml|All Files|*.*";
            if (saveDialog.ShowDialog() == true) {
                if (type == "Cellular Automata") CASerializer.Serialize(saveDialog.FileName, ca);
                else if (type == "Color Scheme") ColorSerializer.Serialize(saveDialog.FileName, colorList);
                else if (type == "Grid") GridSerializer.Serialize(saveDialog.FileName, simClient.getGrid());
                statusTextBlock.Text = type + " saved to " + saveDialog.SafeFileName;
            }
        }


        /// <summary>
        /// Use the specified grid instead of the current one
        /// </summary>
        /// <param name="grid">The new grid to use</param>
        private void setGrid(uint[][] grid) {
            gridSizeX = grid.Length;
            gridSizeY = grid[0].Length;
            simClient.initGrid(grid);
            initDisplay(gridSizeX, gridSizeY);
            lock (gridBMP) {
                gridBMP.setGrid(grid);
                gridBMP.refreshBMP();
            }
            generation = 0;
            generationTextBlock.Text = "Generation: 0";
        }


        /// <summary>
        /// Update the UI, refreshing the grid display and updating the
        /// generation shown in the status bar
        /// </summary>
        private void updateUI() {
            generationTextBlock.Text = "Generation: " + generation;
            gridBMP.refreshBMP();
        }
        #endregion


        #region Threads
        /// <summary>
        /// Disable frame skipping; this makes updateGridThread now responsible
        /// for updating the UI while a simulation is running. This will update
        /// the UI whenever the grid is updated.
        /// </summary>
        private void disableFrameSkipping() {
            skipFrames = false;
            if (updateUiThread != null) updateUiThread.Abort();
        }


        /// <summary>
        /// Enable frame skipping; this makes updateUiThread now responsible
        /// for updating the UI while a simulation is running. This will update
        /// the UI every [refreshRate] milliseconds.
        /// </summary>
        private void enableFrameSkipping() {
            skipFrames = true;
            updateUiThread = new Thread(new ThreadStart(UIUpdater));
            updateUiThread.Start();
        }


        /// <summary>
        /// Used by stepSimulationThread to step through one iteration of the
        /// simulation
        /// </summary>
        private void simulationStepRunner() {
            simClient.step();
            Cell[] updated = simClient.getUpdated();
            if (updated != null) toUpdate.Enqueue(updated);
            stepButton.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => stepButton.IsEnabled = true));
        }


        /// <summary>
        /// Used by runSimulationThread to run the simulation on the server
        /// until stopped, and retrieve any available updates from the server
        /// </summary>
        private void updateRetriever() {
            // Signal the server to run the simulation
            simClient.run(!skipFrames);
            Cell[] updated;

            // Listen for any availalbe updates from the server
            while (!stopEvent.Wait(0)) {
                if ((updated = simClient.getUpdated()) != null) toUpdate.Enqueue(updated);

                // Wait briefly to let displayThread catch up, if needed
                if (skipFrames && toUpdate.Count > simWaitThresh) Thread.Sleep(maxUpdateRate);
                else if (!skipFrames && toUpdate.Count >= 5) Thread.Sleep(refreshRate*2);
            }

            // Signal the server to stop the simulation and retrieve any remaining updates
            simClient.stop();
            while ((updated = simClient.getUpdated()) != null) toUpdate.Enqueue(updated);
        }


        /// <summary>
        /// Loop used by drawingThread to continuously update the grid
        /// whenever new frames are retrieved from the server. If frame
        /// skipping is disabled, the display is updated here as well;
        /// otherwise it is handled by updateUiThread.
        /// </summary>
        private void gridUpdater() {
            Cell[] updated;
            while (true) {
                if (toUpdate.TryDequeue(out updated)) {
                    // Update grid bitmap with new frame
                    gridBMP.setPixels(updated);
                    generation++;

                    // If frame skipping is not allowed, refresh the UI immediately
                    if (!skipFrames) gridImage.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(updateUI));                    
                    
                    // Increase refresh rate if the toUpdate queue has exceeded speedupThresh
                    else if (updateRate > 0 && toUpdate.Count > speedupThresh) updateRate--;
                }
                
                // Wait briefly until next refresh (dependent on frame skipping)
                if (!skipFrames) Thread.Sleep(refreshRate);
                else {
                    Thread.Sleep(updateRate);
                    // Decrease refresh rate if the queue is still empty
                    if (skipFrames && updateRate <= maxUpdateRate && toUpdate.Count == 0) updateRate++;
                }
            }
        }


        /// <summary>
        /// Loop used by updateUiThread to update the UI every
        /// [refreshRate] milliseconds
        /// </summary>
        private void UIUpdater() {
            while (true) {
                gridImage.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(updateUI));
                Thread.Sleep(refreshRate);
            }
        }
        #endregion
    }
}
