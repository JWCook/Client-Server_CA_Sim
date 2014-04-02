using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Web;

namespace CASimService {
    public class CAService : ICAService {
        public string foo() {
            return "Success!!!!";
        }
    }
}