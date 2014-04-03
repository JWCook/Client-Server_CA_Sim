--------------------------------------------------------------------------------
0. Summary
--------------------------------------------------------------------------------

This is a cellular automata simulator written in C#, for the purposes of simulating and visualizing arbitrary CAs using dynamically compiled code. Written as part of an undergrad project.

--------------------------------------------------------------------------------
1. Program Features
--------------------------------------------------------------------------------

- Computation is multi-threaded, optimized, and offloaded to a WCF service that can be run on a separate server
- Dynamically compile delta functions using user-specified C# code
- Import/export delta functions (samples provided)
- Import/export cell grids (samples provided)
- Import/export CA state color schemes (samples provided)
- Editable/scrollable/zoomable cell grid
- Adjustable simulation speed

--------------------------------------------------------------------------------
2. Server
--------------------------------------------------------------------------------

Computation for the CA simulation is handled by a separate service (CAService) hosted by CAServer. By default, this is configured to run on localhost, TCP port 3333 (TODO: config/properties files). CAServer.exe must be run first in order for the client to function.

--------------------------------------------------------------------------------
3. Display
--------------------------------------------------------------------------------

The client program interface consists of the following:

- A scrollable cell grid to display the current contents of the CA simulation
- Simulation controls to run, stop, and clear the simulation
- Zoom controls to zoom in and out of the cell grid
- A status bar displaying the current generation, mouse coordinates on the grid, and any non-critical messages
- Menu items containing additional functionality discussed below

--------------------------------------------------------------------------------
4. Client: Loading, Saving, Creating, and Compiling a CA
--------------------------------------------------------------------------------

Before a simulation can be run, a cellular automata must be defined. To do this, click on Load/Edit CA from the CA menu. From here, the following information can be entered:

**Delta function code (required):**  
This is the code used to compute the delta function for an individual cell. It must be valid C# code, and all code entered must be within the function declaration provided:  

```C#
public void delta(Simulation sim, CPoint center) {  
	// User code goes here  
}
```

Where **sim** is a reference to the current simulation, and **center** is the cell to apply the delta function to. The simulation's grid is accessed via **sim.GetCellState()** and **sim.setCellState()**. **sim.getNumNeighbors()** may also be used as a shortcut to find the number of nearby neighbors of a given state. This is useful for CAs in which only the number of neighboring cells of a given state is relevant, and not their arrangement. See Samples directory for delta function examples.

**Number of states (required):**  
The number of possible states in this CA. it must match the code specified for the delta function.

**Default state (optional):**  
This is the default state that cells are initialized to when a new grid is created. It must be a valid state for the CA being specified. This defaults to state 0 if not specified.

**Neighborhood set (optional):**  
The neighborhood set for the CA. Points can be added and removed with the Add Point and Remove Point controls. A neighborhood set does not have to be specified, but if it is not, the simulation performance will be drastically lower.

**CA file to load (optional):**  
Alternatively, the user may load a CA from a file previously saves from this program, allowing its parameters to be edited.

Once the required information is entered, compile the CA using the Compile button. If there were any compiler errors, they will be displayed in the lower window labeled Compilation Results.

Once a CA has been successfully compiled, click Back to return to the main screen.

To save the CA, select Save CA from the CA menu.

--------------------------------------------------------------------------------
5. Loading, Saving, and Modifying the Grid
--------------------------------------------------------------------------------

The cell grid can be modified by left-clicking on a cell to cycle through the possible states in order, or right-clicking on a cell to clear the cell to the default state. The coordinate display in the status bar at the bottom of the screen shows where the mouse pointer is currently located on the grid.

The grid can be resized by selecting Resize Grid from the Grid menu.

The grid can be saved to a file by selecting Save Grid from the Grid menu.

A previously saved grid can be loaded by selecting Load Grid from the Grid menu.

--------------------------------------------------------------------------------
6. Loading, Saving, and Generating Color Schemes
--------------------------------------------------------------------------------

A color scheme is automatically generated to display different colors for different states of a CA. To re-generate these colors, select Generate Color Scheme from the View menu.

The current color scheme can be saved to a file by selecting Save Color Scheme from the View menu.

A user-specified color scheme can be loaded by selecting Load Color Scheme from the View menu. This must be an XML file containing elements in the following format:

```XML
<ArrayOfString>
<string>...</string>
...  
</ArrayOfString>
```

With each color specified by a 6-digit hexadecimal RGB value enclosed by <string> tags. The first entry will be used for state 0, the second entry will be used for state 1, and so on.

The file loaded must have at least as many colors as the number of states of the currently loaded CA. If a CA is later loaded that has more states than a user-specified color scheme can support, the program will generate a new color scheme.

--------------------------------------------------------------------------------
7. Adjusting Simulation Speed
--------------------------------------------------------------------------------

The display and running speed of the simulation can be adjusted by selecting Framerate Options from the View menu.

By default, frame skipping is enabled. This will allow the simulation to run as fast as possible, and update the display at a constant refresh rate, specified by the Frames Per Second input box.

If frame skipping is not enabled, each frame will be displayed as it is updated on the client at a (maximum) rate specified by the Frames Per Second input box. This significantly decreases the speed of the simulation.

--------------------------------------------------------------------------------
8. Running a Simulation
--------------------------------------------------------------------------------

Once a CA has been loaded and any other optional adjustments have been made, the simulation can be run.

To step through a single generation of the simulation, select Step from the Simulation controls, or from the File menu.

To continuously run the simulation according to the framerate options selected, select Run from the Simulation controls, or from the File menu.

Note that there is some delay in the server-client communication, and once the simulation is stopped, the display may briefly continue to update as any remaining information is retrieved from the server.

While a simulation is running, the display may be zoomed and scrolled as needed. All modifications to the grid, CA, framerate settings, and color scheme are disabled until the simulation is stopped.


--------------------------------------------------------------------------------
9. Included Samples
--------------------------------------------------------------------------------

For testing and demonstration purposes, several sample files are included in the Samples directory.

**CAs:**

- Game of Life
- Wireworld
- Langton's Ant LR (default, 2-color)
- Langton's Ant RLR (3-color)
- Langton's Ant LLRR (4-color)  
- Langton's Ant RLLR (4-color)  

**Color Schemes:**

- Wireworld  
- Langton's Ant & variations  

**Grids:**

- Game of Life - Acorn (single & multiple)
- Game of Life - Puffer Train
- Game of Life - Glider Gun
- Game of Life - Switch Engine (3 variations)
- Wireworld - Binary Counter
- Langton's Ant LR - Highway
- Langton's Ant RLR - Chaotic
- Langton's Ant RLLR - Maze
- Langton's Ant LLRR - Symmetric Brain

--------------------------------------------------------------------------------
10. Known Issues
--------------------------------------------------------------------------------
- Occasional grid synchronization issues between display and simulation. Such issues are corrected upon the next frame update.
- There may be miscellaneous communication exceptions thrown by WCF-related issues (difficult to reproduce).
- The dynamic compilation system is completely unsafe and easy to exploit. If the code entered for a delta function contains runtime errors, this may cause any number of problems with the simulation.
