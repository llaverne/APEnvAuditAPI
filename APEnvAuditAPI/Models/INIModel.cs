using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APEnvAuditAPI.Models
{
    public class INIModel
    {
        public class objService
        {
            // Properties:
            public string strServiceName { get; set; }
            public virtual List<objEnvironment> lstEnvironments { get; set; } = new List<objEnvironment>(); // makes & instantiates the list
        }
        public List<objService> lstSelectedServices { get; set; } // Object to hold list of selected services by user

        public class objEnvironment
        {
            // Properties:
            public string strEnvironmentName { get; set; }
            public virtual List<objINIFile> lstINIFiles { get; set; } = new List<objINIFile>(); // makes & instantiates the list
        }
        public List<objEnvironment> lstEnvironments { get; set; } // Object to hold list of selected services by user
        public class objINIFile
        {
            public string strINIFileName { get; set; }
            public string strINIFilePath { get; set; }
        }

        public List<objINIFile> lstINIFiles { get; set; } // Object to hold list of selected services by user
    }
    
}