using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Web;

namespace CASimServer {
    public class CAHost {
        static void Main(string[] args) {
            Uri baseAddress = new Uri("http://localhost:8000/CAService");
            ServiceHost selfHost = new ServiceHost(typeof(CAServer), baseAddress);
            try {
                selfHost.AddServiceEndpoint(typeof(ICAServer), new WSHttpBinding(), "CAService");
                ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                smb.HttpGetEnabled = true;
                selfHost.Description.Behaviors.Add(smb);
                selfHost.Open();

                Console.ReadLine();
                Console.WriteLine("CA Simulator Server is running. Press Enter to exit.");
                selfHost.Close();
            }
            catch (CommunicationException ce) {
                Console.WriteLine(ce.Message);
                selfHost.Abort();
            }
        }
    }
}