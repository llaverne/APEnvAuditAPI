using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using System.IO;
using System.Collections;
using System.Net;
using System.Configuration;
using Newtonsoft.Json;

namespace APEnvAuditAPI.Controllers
{
    public class ServicesController : Controller
    {
        /* Usage:
         * Return ALL unique & lowercase Service Names as Json: http://localhost:56414/Services/
         * 
         * 
         * 
         */

        // Settings:
        private List<string> lstServiceList = new List<string>(); // Services list container
        private string strEnlistmentPath = ConfigurationManager.AppSettings["strEnlistmentPath"];
        private string strAppURL = ConfigurationManager.AppSettings["strAppURL"];

        public ActionResult Index(string strSearchTerm = null) // Done. Return Json; ALL stripped & unique Service Names matching SearchTerm:
        {// http://localhost:56414/Services/
            // Settings:
            string strGetDCsURL = strAppURL + @"DataCenters";
            List<string> lstDCFolderListToCheck = new List<string>(); // DC list container

            // Service list is made from list of directories found in a DCs directory:
            if (Directory.Exists(strEnlistmentPath))
            {
                // Get DCs list from DCs API:
                string objJSON = "";
                using (WebClient webClient = new WebClient()) // USING WebClient to destroy object after use.
                {
                    webClient.UseDefaultCredentials = true;
                    objJSON = webClient.DownloadString(strGetDCsURL);
                    //lstAllDCs = json.ToArray();
                }

                // JSON references:
                // http://www.newtonsoft.com/json/help/html/SerializingJSON.htm (using now)

                lstDCFolderListToCheck = JsonConvert.DeserializeObject<List<string>>(objJSON); // Convert resultant JSON to List<>

                // Iterate through each DC folder to get it's services:
                foreach (string strDCFolderWereChecking in lstDCFolderListToCheck)
                {
                    // Get entire folder/services list for this DC: D:\Enlistments\APGold\autopilotservice\Bn1\[XXX]
                    List<string> lstThisDCsServiceList = Directory.GetDirectories(strEnlistmentPath + strDCFolderWereChecking).ToList(); // Adds FOLDER names to list
                    
                    foreach (string strFolderNameAndPath in lstThisDCsServiceList)
                    {
                        string strServiceName = Path.GetFileName(strFolderNameAndPath); // strip path chars

                        // Format service name then add or exclude each service to resultant list:
                        switch (strServiceName.ToLower()) // Exclude below directories from list:
                        {
                            case "autopilot": // Exclude
                                break;
                            case "autopilotsecurity": // Exclude
                                break;
                            case "shared": // Exclude
                                break;
                            default: // ADD... after removing all extra chars from service name:
                                lstServiceList.Add( funNormalizeServiceName(strServiceName) );
                                break;
                        }
                    }
                }

                // OK, now de-dup & sort the normalized service list:
                lstServiceList = lstServiceList.Distinct().ToList();

            }
            else
            {
                // ERROR: path not valid!
                lstServiceList.Add("ERROR: Invalid Enlistment Path or unreachable.");
            }

            if (strSearchTerm != null)
            {
                // Build a list of matching Services: ToDo add regex or wildcards here
                var lstMatches = lstServiceList.FindAll(s => s.StartsWith(strSearchTerm.ToLower()));

                var varJson = funReturnJson(lstMatches); // Return JSON data
                return varJson;
            }
            
            // Return List:
            var varJson2 = funReturnJson(lstServiceList); // Return JSON data
            return varJson2;

        }

        [HttpPost]
        public ActionResult Search(string strSearchTerm) // Form POST entry point. Return MATCHING Services list
        {
            // Settings:
            string strGetServicesURL = strAppURL + @"Services?strSearchTerm=" + strSearchTerm;
            List<string> lstAllServicesList = new List<string>(); // Services list container
            List<string> lstServicesToSelect = new List<string>(); // Services list container

            // Get entire list of services from Services API
            string objJSON = "";
            using (WebClient webClient = new WebClient()) // USING WebClient to destroy object after use.
            {
                webClient.UseDefaultCredentials = true;
                objJSON = webClient.DownloadString(strGetServicesURL);
                //lstAllDCs = json.ToArray();
            }

            // JSON references:
            // http://www.newtonsoft.com/json/help/html/SerializingJSON.htm (using now)

            lstAllServicesList = JsonConvert.DeserializeObject<List<string>>(objJSON); // Convert resultant JSON to List<>

            // Return the resultant list for selection to SearchView
            return View(lstAllServicesList);
            
        }

        // Functions:
        private IList<SelectListItem> funGetALLServices()
        {
            // Get ServiceName list from Services API to populate drop-down:
            string objJSON = "";
            //string strGetServicesURL = strAppURL + @"Services" + @"&strSearchTerm=" + HttpUtility.UrlEncode(strSearchTerm); DOESNT WORK YET
            string strGetServicesURL = strAppURL + @"Services";

            List<string> lstServices = new List<string>(); // Service list container

            using (WebClient webClient = new WebClient()) // (USING WebClient to destroy object after use.)
            {
                webClient.UseDefaultCredentials = true;
                objJSON = webClient.DownloadString(strGetServicesURL); // Queries: http://localhost:56414/Services/
            }

            // JSON references:
            // http://www.newtonsoft.com/json/help/html/SerializingJSON.htm (using now)

            lstServices = JsonConvert.DeserializeObject<List<string>>(objJSON); // Convert resultant JSON to List<>
            lstServices.Sort();

            List<SelectListItem> lstServiceNamesSorted = new List<SelectListItem>();

            foreach (string strServiceName in lstServices)
            {
                // Exclude any non-desired ServiceNames here:
                lstServiceNamesSorted.Add(new SelectListItem { Text = strServiceName, Value = strServiceName });
            }

            return lstServiceNamesSorted;
        }
        public string funNormalizeServiceName(string strEntireServiceName)
        {   // Remove training chars & lowercase all chars:
            string strServiceNameStripped = "";
            if (strEntireServiceName.IndexOf("-") > -1) // to avoid exception when "-" is not present.
            {
                strServiceNameStripped = strEntireServiceName.Substring(0, strEntireServiceName.IndexOf("-")); // Remove everything after first "-"
            }
            else
            {
                strServiceNameStripped = strEntireServiceName;
            }
            strServiceNameStripped = strServiceNameStripped.Trim(); // Finally, remove any leading/trailing spaces
            return strServiceNameStripped.ToLower();
        } // Done. 

        public string funUppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public JsonResult funReturnJson(List<string> lstToReturn)
        {
            var jsonResult = Json(lstToReturn); // Required to return JSON data
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet; // Required to return JSON data
            return jsonResult;
        } // Done. 
    }
}