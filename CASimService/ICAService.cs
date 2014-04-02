using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace CASimService {
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface ICAService {
        [OperationContract]
        uint getCellState(CPoint cell);
        [OperationContract]
        int getNumNeighbors(CPoint cell, int state);
        [OperationContract]
        Cell[] getUpdated();
        [OperationContract]
        bool initCA(CA newCA);
        [OperationContract]
        void initGridBlank(int x, int y);
        [OperationContract]
        void initGrid(uint[][] newGrid);
        [OperationContract]
        void run(bool throttled);
        [OperationContract]
        void setCellState(CPoint cell, uint state);
        [OperationContract]
        void step();
        [OperationContract]
        void stop();
        [OperationContract]
        void updateGrid(bool addToUpdated);
        [OperationContract]
        int getNumStates();
        [OperationContract]
        uint[][] getGrid();
        [OperationContract]
        int getGeneration();
    }
}
