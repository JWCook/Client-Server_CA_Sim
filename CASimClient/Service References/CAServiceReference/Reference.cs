﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CASimClient.CAServiceReference {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="CAServiceReference.ICAService", SessionMode=System.ServiceModel.SessionMode.Required)]
    public interface ICAService {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICAService/getCellState", ReplyAction="http://tempuri.org/ICAService/getCellStateResponse")]
        uint getCellState(CASimService.CPoint cell);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICAService/getNumNeighbors", ReplyAction="http://tempuri.org/ICAService/getNumNeighborsResponse")]
        int getNumNeighbors(CASimService.CPoint cell, int state);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICAService/getUpdated", ReplyAction="http://tempuri.org/ICAService/getUpdatedResponse")]
        CASimService.Cell[] getUpdated();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICAService/initCA", ReplyAction="http://tempuri.org/ICAService/initCAResponse")]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CASimService.CPoint))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CASimService.Cell[]))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CASimService.Cell))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(CASimService.CPoint[]))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(System.Reflection.MethodInfo))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(System.Reflection.MethodBase))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(System.Reflection.MemberInfo))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(uint[][]))]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(uint[]))]
        bool initCA(CASimService.CA newCA);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICAService/initGridBlank", ReplyAction="http://tempuri.org/ICAService/initGridBlankResponse")]
        void initGridBlank(int x, int y);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICAService/initGrid", ReplyAction="http://tempuri.org/ICAService/initGridResponse")]
        void initGrid(uint[][] newGrid);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICAService/run", ReplyAction="http://tempuri.org/ICAService/runResponse")]
        void run(bool throttled);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICAService/setCellState", ReplyAction="http://tempuri.org/ICAService/setCellStateResponse")]
        void setCellState(CASimService.CPoint cell, uint state);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICAService/step", ReplyAction="http://tempuri.org/ICAService/stepResponse")]
        void step();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICAService/stop", ReplyAction="http://tempuri.org/ICAService/stopResponse")]
        void stop();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICAService/updateGrid", ReplyAction="http://tempuri.org/ICAService/updateGridResponse")]
        void updateGrid(bool addToUpdated);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICAService/getNumStates", ReplyAction="http://tempuri.org/ICAService/getNumStatesResponse")]
        int getNumStates();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICAService/getGrid", ReplyAction="http://tempuri.org/ICAService/getGridResponse")]
        uint[][] getGrid();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICAService/getGeneration", ReplyAction="http://tempuri.org/ICAService/getGenerationResponse")]
        int getGeneration();
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ICAServiceChannel : CASimClient.CAServiceReference.ICAService, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class CAServiceClient : System.ServiceModel.ClientBase<CASimClient.CAServiceReference.ICAService>, CASimClient.CAServiceReference.ICAService {
        
        public CAServiceClient() {
        }
        
        public CAServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public CAServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public CAServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public CAServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public uint getCellState(CASimService.CPoint cell) {
            return base.Channel.getCellState(cell);
        }
        
        public int getNumNeighbors(CASimService.CPoint cell, int state) {
            return base.Channel.getNumNeighbors(cell, state);
        }
        
        public CASimService.Cell[] getUpdated() {
            return base.Channel.getUpdated();
        }
        
        public bool initCA(CASimService.CA newCA) {
            return base.Channel.initCA(newCA);
        }
        
        public void initGridBlank(int x, int y) {
            base.Channel.initGridBlank(x, y);
        }
        
        public void initGrid(uint[][] newGrid) {
            base.Channel.initGrid(newGrid);
        }
        
        public void run(bool throttled) {
            base.Channel.run(throttled);
        }
        
        public void setCellState(CASimService.CPoint cell, uint state) {
            base.Channel.setCellState(cell, state);
        }
        
        public void step() {
            base.Channel.step();
        }
        
        public void stop() {
            base.Channel.stop();
        }
        
        public void updateGrid(bool addToUpdated) {
            base.Channel.updateGrid(addToUpdated);
        }
        
        public int getNumStates() {
            return base.Channel.getNumStates();
        }
        
        public uint[][] getGrid() {
            return base.Channel.getGrid();
        }
        
        public int getGeneration() {
            return base.Channel.getGeneration();
        }
    }
}