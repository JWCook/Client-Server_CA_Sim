using Microsoft.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using System.Reflection;

namespace CASimService {

    /// <summary>
    /// A class representing a CA and its parameters. A CA is used by first initializing it either from a file
    /// (using CASerializer) or from the constructor.
    /// 
    /// Before being used, the delta function for the CA must be compiled with compileDelta().
    /// 
    /// The CA may then be used by calling delta() on a cell. delta() also requires a reference to the Simulation that
    /// calls it in order to access the Simulation's grid.
    /// 
    /// The delta function must be in the following format:
	///    public void delta(Simulation sim, CPoint center) {
	///       //(code goes here)
	///    }
    /// Where sim is a reference to the current simulation, and center is the cell to apply the delta function to.
    /// 
    /// The simulation's grid is accessed via sim.GetCellState() and sim.setCellState(). sim.getNumNeighbors() may also
    /// be used as a shortcut for CAs in which only the number of neighboring cells of a given state is relevant.
    /// </summary>
    [Serializable]
    public class CA {
        #region Serialized variables
        private int numStates;             // Number of possible states
        private uint defaultState;         // Default state that cells are initialized to
        private CPoint[] neighborhood;     // Neighborhood set, if available
        private string deltaStr;           // String representing the delta function

        public int NumStates {
            get { return numStates; }
            set { numStates = value; }
        }
        public uint DefaultState {
            get { return defaultState; }
            set { defaultState = value; }
        }
        public CPoint[] Neighborhood {
            get { return neighborhood; }
            set { neighborhood = value; }
        }
        public string DeltaStr {
            get { return deltaStr; }
            set { deltaStr = value; }
        }
        #endregion

        #region Nonserialized variables
        private CPoint[] rNeighborhood;     // Reversed neighborhood set, if available
        private object deltaWrapper;        // An object containing a compiled delta function
        MethodInfo deltaFunction;           // MethodInfo handle for the compiled delta function
        public CPoint[] RNeighborhood { get { return rNeighborhood; } }
        #endregion


        /// <summary>
        /// Initialize a CA with default values (required for serialization)
        /// </summary>
        public CA() {
            numStates = 2;
            defaultState = 0;
            neighborhood = null;
            deltaStr = null;
        }


        /// <summary>
        /// Initialize a CA with the specified values. compileDelta() must be run before the CA is usable.
        /// </summary>
        /// <param name="n">The number of states of this CA</param>
        /// <param name="d">The default state of this CA. Must be between 0 and
        /// n (will be adjusted otherwise).</param>
        /// <param name="neighbors">Array of points representing the neighborhood set of this CA, if applicable</param>
        /// <param name="delta">A string representing the delta function to be compiled</param>
        public CA(int n, uint d, CPoint[] neighbors, string delta) {
            numStates = n;
            defaultState = (d < 0) ? 0 : ((d >= n) ? (uint)n-1 : d);
            neighborhood = neighbors;
            deltaStr = delta;
            reverseNeighborhood();
        }


        /// <summary>
        /// Return an uncompiled copy of this CA. This is used when sending a CA over a network, since a compiled
        /// assmebly cannot be serialized.
        /// </summary>
        /// <returns>An uncompiled copy of this CA</returns>
        public CA copyCA() {
            return new CA(numStates, defaultState, neighborhood, deltaStr);
        }


        /// <summary>
        /// Compile this CA's delta function
        /// </summary>
        public string compileDelta() {
            // Put delta function code in a wrapper class and namespace
            string fullDeltaStr = "using CASimService; namespace Delta { public class DeltaWrapper {" + deltaStr + "}}";

            // Attempt to compile the code supplied by deltaStr
            CSharpCodeProvider csCompiler = new CSharpCodeProvider();
            CompilerParameters compilerParams = new CompilerParameters(new String[] { "CASimService.dll" });
            compilerParams.GenerateExecutable = false;
            compilerParams.GenerateInMemory = true;
            CompilerResults results = csCompiler.CompileAssemblyFromSource(compilerParams, new string[1] { fullDeltaStr });

            // If unsuccessful, display any compiler errors
            if (results.Errors.HasErrors) {
                StringBuilder errors = new StringBuilder("compiler Errors:\n\n");
                foreach (CompilerError error in results.Errors) {
                    errors.AppendFormat("Line {0},{1}\t: {2}\n", error.Line, error.Column, error.ErrorText);
                }
                return (errors.ToString());
            }

            // If successful, store the compiled assembly's method info
            else {
                Assembly a = results.CompiledAssembly;
                deltaWrapper = a.CreateInstance("Delta.DeltaWrapper");
                Type deltaType = deltaWrapper.GetType();
                deltaFunction = deltaType.GetMethod("delta");
                return ("Delta function successfully compiled");
            }
        }


        /// <summary>
        /// Call the compiled delta function, if available
        /// </summary>
        /// <param name="sim">The simulation for which the function is being called</param>
        /// <param name="center">The center cell to call the function on</param>
        public void delta(Simulation sim, CPoint center) {
            if (deltaFunction != null) deltaFunction.Invoke(deltaWrapper, new Object[2] { sim, center });
        }


        /// <summary>
        /// Reverse the current neighborhood set, flipping it vertically and horizontally about the center. This can be
        /// done by simply reversing the order of the points as well as the sign on each coordinates.
        /// </summary>
        public void reverseNeighborhood() {
            if (neighborhood == null) return;
            rNeighborhood = new CPoint[neighborhood.Length];
            for (int i = 0; i < rNeighborhood.Length; i++) {
                rNeighborhood[i] = new CPoint(neighborhood[(neighborhood.Length - i) - 1].X * (0-1),
                                        neighborhood[(neighborhood.Length - i) - 1].Y * (0-1));
            }
        }
    }
}
