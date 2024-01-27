using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Data.Entity;
using System.Web.Mvc;

/* WHERE I'M AT:
 * 
 * 
 */

namespace APEnvAuditAPI.Models
{
    public class ServiceModel
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
            public virtual List<string> strDataCenterNames { get; set; } = new List<string>(); // makes & instantiates the list
        }
        public List<objEnvironment> lstEnvironments { get; set; } // Object to hold list of selected services by user

        //public class objEnvironment
        //{
        //    // Properties:
        //    public string strEnvironmentName { get; set; }
        //    public virtual List<objDataCenter> lstDataCenters { get; set; } = new List<objDataCenter>(); // makes & instantiates the list
        //}

        //public class objDataCenter
        //{
        //    // Properties:
        //    public string strDataCenterName { get; set; }
        //}





    }

}