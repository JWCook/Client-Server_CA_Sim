using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Runtime.Serialization;
using System.Web;
using CASimService;

namespace CASimServer {
    /// <summary>
    /// A console application used to host a Simulation. TCP is used, with a separate MEX endpoint to send metadata.
    /// Information is encoded in binary instead of XML. If this host is used with a non-WCF client, an HTTP binding
    /// with XML encoding should be used instead.
    /// </summary>
    public class CAServer {
        static void Main(string[] args) {
            // TODO: extract into a properties file
            string serviceName = "CAService";
            string baseAddress = "net.tcp://localhost:3333/";
            ServiceHost caHost = new ServiceHost(typeof(Simulation), new Uri(baseAddress + serviceName));

            try {
                // Configure the TCP binding
                NetTcpBinding tcpBinding = new NetTcpBinding();
                tcpBinding.MaxReceivedMessageSize = Int32.MaxValue;
                tcpBinding.MaxBufferPoolSize = Int32.MaxValue;
                tcpBinding.ReaderQuotas.MaxArrayLength = Int32.MaxValue;

                // Configure a binary message encoding binding element
                BinaryMessageEncodingBindingElement binaryBinding = new BinaryMessageEncodingBindingElement();
                binaryBinding.ReaderQuotas.MaxArrayLength = Int32.MaxValue;
                binaryBinding.ReaderQuotas.MaxNameTableCharCount = Int32.MaxValue;
                binaryBinding.ReaderQuotas.MaxStringContentLength = Int32.MaxValue;

                // Configure a MEX TCP binding to send metadata
                CustomBinding mexBinding = new CustomBinding(MetadataExchangeBindings.CreateMexTcpBinding());
                mexBinding.Elements.Insert(0, binaryBinding);
                mexBinding.Elements.Find<TcpTransportBindingElement>().MaxReceivedMessageSize = Int32.MaxValue;

                // Configure the host
                ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                caHost.Description.Behaviors.Add(smb);
                caHost.AddServiceEndpoint(typeof(IMetadataExchange), mexBinding, baseAddress + serviceName + "/mex");
                caHost.AddServiceEndpoint(typeof(ICAService), tcpBinding, baseAddress + serviceName);
                ServiceDebugBehavior debug = caHost.Description.Behaviors.Find<ServiceDebugBehavior>();
                if (debug == null) caHost.Description.Behaviors.Add(new ServiceDebugBehavior() { IncludeExceptionDetailInFaults = true });
                else if (!debug.IncludeExceptionDetailInFaults) debug.IncludeExceptionDetailInFaults = true;

                // Open the host and run until Enter is pressed
                caHost.Open();
                Console.WriteLine("CA Simulator Server is running. Press Enter to exit.");
                Console.ReadLine();
                caHost.Close();
            }
            catch (CommunicationException e) {
                Console.WriteLine(e.Message);
                caHost.Abort();
            }
            catch (InvalidOperationException e) {
                Console.WriteLine(e.Message);
                caHost.Abort();
            }
        }
    }
}