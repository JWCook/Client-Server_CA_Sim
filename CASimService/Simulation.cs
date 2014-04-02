using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace CASimService {
    /// <summary>
    /// Functionality needed for setting up and simulating a cellular automaton. The simulation, its grid, and its CA
    /// are initialized separately to allow a grid to be displayed before any parameters are entered.
    ///
    /// When a new simulation is created, the grid is initialized to a default size (500x500) with all cells in the
    /// default state (0). The grid may be re-initialized, if needed.
    ///
    /// Before using a simulation, initCA() must be called to load CA parameters. Before a CA is loaded, the grid is
    /// still accessible, but the simulation cannot be run.
    ///
    /// If the loaded CA specifies a neighborhood set, the simulation will attempt to optimize subsequent iterations by
    /// only checking cells within the reverse neighborhood set of changed cells (e.g., the simulation checks cells for
    /// which one or more cells in its neighborhood set has changed).
    ///
    /// If the loaded CA has a variable neighborhood set or does not specify a a neighborhood set, the delta function
    /// must be applied to every cell in the grid. Ugly.
    ///
    /// Once set up, the simulation is run by calls to step(), which simulates a single generation of the cellular automaton.                              
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple, MaxItemsInObjectGraph = Int32.MaxValue)]
    public class Simulation : ICAService {
        private const int defaultX = 500;        // Default width of grid (in cells)
        private const int defaultY = 500;        // Default height of grid (in cells)
        private int gridSizeX;                   // Width of grid (in cells)
        private int gridSizeY;                   // Height of grid (in cells)
        private int generation;                  // Current number of generations simulated
        private bool optimizing;                 // Indicates if optimizations can be made
        private uint[][] grid;                   // The 2D grid of cells
        private CA ca;                           // The CA used for this simulation
        private SortedSet<CPoint> toCheck;       // Cells to check next iteration
        private ConcurrentQueue<Cell> toUpdate;  // Cells to update this iteration
        private ConcurrentQueue<Cell[]> updated; // Cells updated since last checked by client

        #region Threading variables
        private const int defaultRefresh = 0;    // Default sim thread refresh period (0 = run as fast as possible)
        private const int slowdownThresh = 100;  // Sim thread will slow down if updated is >= this size
        private const int speedupThresh = 10;    // Sim thread will speed up if updated is <= this size
        private bool throttled;                  // Indicates if the simulation should't outproduce client
        private int refresh;                     // Refresh period for sim thread (in ms)
        private Thread runSimulationThread;      // Thread responsible for continuously running the simulation
        private ManualResetEventSlim stopEvent;  // Used to signal a running simulation run thread to stop
        #endregion


        public uint[][] getGrid() { lock (grid) { return grid; } }
        public int getGeneration() { return generation; }
        public int getNumStates() { lock (ca) { return ca.NumStates; } }


        /// <summary>
        /// Initialize a new simulation with a blank grid (with all cells in the default state) of the default size.
        /// </summary>
        public Simulation() {
            toCheck = new SortedSet<CPoint>(new PointComparer());
            toUpdate = new ConcurrentQueue<Cell>();
            updated = new ConcurrentQueue<Cell[]>();
            grid = new uint[1][];
            ca = new CA();
            initGridBlank(defaultX, defaultY);
            refresh = defaultRefresh;
            throttled = false;
        }
        

        /// <summary>
        /// Given coordinates relative to a specified center point, return the equivalent absolute coords on the grid.
        /// </summary>
        private CPoint getAbsoluteCoords(CPoint center, CPoint relative) {
            int absoluteX = wrap(center.X + relative.X, true);
            int absoluteY = wrap(center.Y + relative.Y, false);
            return new CPoint(absoluteX, absoluteY);
        }


        /// <summary>
        /// Get the state of the specified cell
        /// </summary>
        public uint getCellState(CPoint cell) {
            lock (grid) { return grid[wrap(cell.X, true)][wrap(cell.Y, false)]; }
        }


        /// <summary>
        /// Get the number of neighbors with the specified state that are neighboring the specified cell. To be used as
        /// a shortcut for CAs in which only the number of neighboring cells of a given state is relevant, and not their
        /// arrangement.
        /// </summary>
        public int getNumNeighbors(CPoint cell, int state) {
            if (ca == null) return 0;

            // Iterate through each neighbor and test if it is in the specified state
            int relativeX, relativeY, neighborX, neighborY, count = 0;
            for (int i = 0; i < ca.Neighborhood.Length; i++) {
                lock (ca) {
                    relativeX = ca.Neighborhood[i].X;
                    relativeY = ca.Neighborhood[i].Y;
                }
                if (!(relativeX == 0 && relativeY == 0)) {
                    neighborX = wrap(cell.X + relativeX, true);
                    neighborY = wrap(cell.Y + relativeY, false);
                    lock (grid) { if (grid[neighborX][neighborY] == state) count++; }
                }
            }

            return count;
        }


        /// <summary>
        /// Get an array of updated cells representing a single frame of the simulation, if available
        /// </summary>
        /// <returns>An array of updated cells, if available, or null otherwise
        /// </returns>
        public Cell[] getUpdated() {
            Cell[] frame = null;
            return (!updated.IsEmpty && updated.TryDequeue(out frame)) ? frame : null;
        }


        /// <summary>
        /// Load the specified CA into this simulation, and clear the grid if necessary
        /// </summary>
        /// <param name="newCA">A CA representing a CA and its parameters</param>
        /// <returns>True if the CA is valid, or false otherwise</returns>
        public bool initCA(CA newCA) {
            // If an incompatible CA has previously been run, clear the grid
            if (getNumStates() > newCA.NumStates) initGridBlank(gridSizeX, gridSizeY);

            // Load and compile the new CA
            string result;
            lock (ca) {
                ca = newCA;
                result = newCA.compileDelta();
                if (ca.Neighborhood != null) optimizing = true;
                else optimizing = false;
            }

            // Return true if the CA's delta function compiled successfully
            return (result == "Delta function successfully compiled");
        }


        /// <summary>
        /// Initialize a blank grid (with all cells in state 0) of the specified size
        /// </summary>
        public void initGridBlank(int x, int y) {
            generation = 0;
            gridSizeX = x;
            gridSizeY = y;
            uint defaultState = ca.DefaultState;

            // Set each cell to the default state
            lock (grid) {
                grid = new uint[gridSizeX][];
                Parallel.For(0, gridSizeX, i => {
                    grid[i] = new uint[gridSizeY];
                    for (int j = 0; j < gridSizeY; j++) grid[i][j] = defaultState;
                });
            }

            // Clear any pending updated cells
            updated = new ConcurrentQueue<Cell[]>();
        }


        /// <summary>
        /// Initialize a grid from a the specified array
        /// </summary>
        public void initGrid(uint[][] newGrid) {
            generation = 0;

            lock (grid) {
                grid = newGrid;
                gridSizeX = grid.Length;
                gridSizeY = grid[0].Length;
            }

            // Clear any pending updated cells
            updated = new ConcurrentQueue<Cell[]>();
        }


        /// <summary>
        /// Start the simulation thread
        /// </summary>
        /// <param name="t">Indicates if the simulation should try to only produce what the client can consume</param>
        public void run(bool t) {
            throttled = t;
            stopEvent = new ManualResetEventSlim(false);
            runSimulationThread = new Thread(new ThreadStart(runSimulation));
            runSimulationThread.Start();
        }



        /// <summary>
        /// Used by runSimulationThread to run the simulation until stopped
        /// </summary>
        private void runSimulation() {
            while (!stopEvent.Wait(0)) {
                step();

                // Adjust refresh rate and wait for client to catch up, if necessary
                if (!throttled) {
                    if (updated.Count >= slowdownThresh) refresh++;
                    else if (refresh > 0 && updated.Count <= speedupThresh) refresh--;
                }
                else {
                    if (updated.Count >= 5) refresh++;
                    else if (refresh > 0 && updated.Count <= 1) refresh--;
                }
                Thread.Sleep(refresh);
            }
        }


        /// <summary>
        /// Set the specified cell to be in the specified state in the next iteration; this adds the modification to a
        /// set of cells to modify at the end of this iteration.
        ///
        /// If a neighborhood set has been specified and optimization is possible, any cell modifications are caught and
        /// handled here. Cells within the reverse neighborhood set of the changed cell are added to the list of cells
        /// to check in the next iteration.
        /// </summary>
        public void setCellState(CPoint cell, uint state) {
            if (ca == null) return;
            CPoint modifiedPoint = new CPoint(wrap(cell.X, true), wrap(cell.Y, false));
            toUpdate.Enqueue(new Cell(modifiedPoint, state));
            foreach (CPoint p in ca.RNeighborhood) lock (toCheck) toCheck.Add(getAbsoluteCoords(modifiedPoint, p));
        }


        /// <summary>
        /// Step through one iteration of the simulation. This involves:
        /// -Applying the delta function to each cell that needs to be checked
        /// -Updating the list of cells to be checked in the next iteration (handled by any calls to setCellState())
        /// -Applying any changes to the grid at the end of the iteration.
        /// </summary>
        public void step() {
            if (ca == null) return;

            // If optimization is not possible (or first generation), check every cell
            if (!optimizing || generation == 0) {
                Parallel.For(0, gridSizeX, i => {
                    for (int j = 0; j < gridSizeY; j++) ca.delta(this, new CPoint(i, j));
                });
            }

            // If optimization is possible, only check cells as needed
            else {
                // Copy collection of cells to check and clear the collection
                CPoint[] checking;
                lock (toCheck) {
                    checking = toCheck.ToArray();
                    toCheck.Clear();
                }

                // Check each cell to be checked from the previous generation
                // (and update the collection of cells to be checked next generation)
                Parallel.For(0, checking.Length, i => { ca.delta(this, checking[i]); });
            }

            // Update the grid with any cells changed in this iteration
            updateGrid(true);
            generation++;
        }


        /// <summary>
        /// Signal simulation thread to stop
        /// </summary>
        public void stop() {
            stopEvent.Set();
            runSimulationThread.Join();
            stopEvent.Dispose();
            refresh = defaultRefresh;
        }


        /// <summary>
        /// Update the grid with any cells changed in this iteration, and add to the list of cells updated
        /// (to be checked by the client), if specified.
        /// </summary>
        /// <param name="addToUpdated">Specifies whether to add to the list of updated cells. This Should be false only 
        /// when updateGrid() is explicitly called by the client, and the client has already updated the appropriate
        /// cells.</param>
        public void updateGrid(bool addToUpdated) {
            // Add to list of updated cells, if specified
            if (addToUpdated && toUpdate.Count > 0) {
                Cell[] frame = toUpdate.ToArray();
                updated.Enqueue(frame);
            }

            // Update all cells changed in this iteration
            Cell next;
            lock (grid) {
                while (!toUpdate.IsEmpty) {
                    if (toUpdate.TryDequeue(out next)) grid[next.X][next.Y] = next.state;
                }
            }
        }


        /// <summary>
        /// Adjust an x or y value to be within the bounds of the grid, with wraparound. 
        /// Example: if wrap() is called on both coordinates of the point(502,-5) on a 500x500 grid, then the point is
        /// wrapped to (2,495).
        /// </summary>
        private int wrap(int n, Boolean isX) {
            while (n < 0) n += (isX) ? gridSizeX : gridSizeY;
            while (n >= ((isX) ? gridSizeX : gridSizeY)) n -= (isX) ? gridSizeX : gridSizeY;
            return n;
        }
    }
}
