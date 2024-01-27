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
using System.Dynamic;

namespace APEnvAuditAPI.Controllers
{
    public class EnvironmentsController : Controller
    {
        /* Usage:
         * Return ALL unique Environment Names as Json: http://localhost:56414/Environments/
         * 
         * 
         * PUSH verification
         */
        string strEnlistmentPath = ConfigurationManager.AppSettings["strEnlistmentPath"];
        string strAppURL = ConfigurationManager.AppSettings["strAppURL"];
        private List<string> lstEnvironmentList = new List<string>(); // Services list container

        // POST: Environments/Create...
        // GET: URI/Environments/
        public ActionResult Index()
        {
            lstEnvironmentList.Add("Prod");
            lstEnvironmentList.Add("Dev");
            lstEnvironmentList.Add("Test");
            lstEnvironmentList.Add("PPE");
            lstEnvironmentList.Add("INT");
            lstEnvironmentList.Add("Sandbox");
            // Return List:
            var varJson = funReturnJson(lstEnvironmentList); // Return JSON data
            return varJson;
        } // Done. Return ALL environment names as Json
        
        [HttpPost]
        public ActionResult ListEnvironments(FormCollection collection) // Done. Return ALL Environments for a Service
        {
            string strGetDCsURL = strAppURL + @"DataCenters";
            // Next Views Data Model components:
            dynamic dynViewModel = new ExpandoObject(); // object to pass to View.
            List<Models.ServiceModel.objService> lstSelectedServicesAndEnvironments = new List<Models.ServiceModel.objService>();
            
            try // iterate through checked Services & get the Environments where the Service(s) is/are deployed:
            {
                // Get DCs list from DCs API:
                string objJSON = "";
                List<string> lstDCFolderListToCheck = new List<string>(); // DC list container

                using (WebClient webClient = new WebClient()) // USING WebClient to destroy object after use.
                {
                    webClient.UseDefaultCredentials = true;
                    objJSON = webClient.DownloadString(strGetDCsURL);
                }

                lstDCFolderListToCheck = JsonConvert.DeserializeObject<List<string>>(objJSON); // Convert resultant JSON to List<>. JSON reference: http://www.newtonsoft.com/json/help/html/SerializingJSON.htm

                if (Directory.Exists(strEnlistmentPath))
                {
                    foreach (string strSelectedServiceName in collection)
                    {
                        Models.ServiceModel.objService objService = new Models.ServiceModel.objService { strServiceName = strSelectedServiceName };
                        List<Models.ServiceModel.objEnvironment> lstEnvironmentList = new List<Models.ServiceModel.objEnvironment>(); // Services list container
                        // Check all DCs to see if this service is deployed:
                        // Iterate through each DC folder to get it's services:
                        foreach (string strDCFolderWereChecking in lstDCFolderListToCheck)
                        {
                            // Get entire folder/services list for this DC, where a dash is present: D:\Enlistments\APGold\autopilotservice\Bn1\[XXX]
                            List<string> lstThisDCsServiceList = Directory.GetDirectories(strEnlistmentPath + strDCFolderWereChecking, "*-*").ToList(); // Adds FOLDER names to list

                            foreach (string strFolderNameAndPath in lstThisDCsServiceList)
                            {
                                string strServiceNameUnderTest = Path.GetFileName(strFolderNameAndPath); // strip path chars

                                if (funNormalizeServiceName(strServiceNameUnderTest) == strSelectedServiceName) // It's one of our Service folders, so now get the Env:
                                {
                                    //lstEnvironmentList.Add(funGetEnvFromServiceName(strServiceNameUnderTest));
                                    Models.ServiceModel.objEnvironment objEnvironment = new Models.ServiceModel.objEnvironment { strEnvironmentName = funGetEnvFromServiceName(strServiceNameUnderTest) }; // Make an Environment object
                                    //objService.lstEnvironments.Add(objEnvironment); // add Environment object to our list in objService
                                    lstEnvironmentList.Add(objEnvironment);
                                }
                            }
                        }
                        // de-dupe & sort env list:
                        var lstEnvironmentListDeDuped = lstEnvironmentList.GroupBy( x => x.strEnvironmentName ).Select( g => g.First()).ToList();
                        objService.lstEnvironments.AddRange(lstEnvironmentListDeDuped); // add Environment list to our objService

                        lstSelectedServicesAndEnvironments.Add(objService); // now add objService to our final list of objects to POST
                    }
                    
                }
                else
                {
                    // ERROR: path not valid!
                    return View("ERROR: Invalid Enlistment Path or unreachable.");
                }

                dynViewModel.lstSelectedServicesAndEnvironments = lstSelectedServicesAndEnvironments;
                // Return List:
                return View(dynViewModel);

            }
            catch (Exception ex)
            {
                //clsService objService = new clsService { strServiceName = ex.Message };
                //lstSelectedServicesAndEnvironments.Add(objService);
                return View("~/Views/Shared/ErrorView.cshtml", ex);
            }
        }

        // Functions:
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
        public string funGetEnvFromServiceName(string strEntireServiceName)
        {   // ToDo: ServerPool-APDevCCP (service?) name doesn't always include env as last segment...an issue to deal with?
            // Are there 2 dashes: (sometimes not, like with ServerPool-APDevCCP)
            int count = 0;
            foreach (char c in strEntireServiceName)
                if (c == '-') count++;

            string strEnvironment = "";
            if (count > 1)
            {
                int intFirstDash = strEntireServiceName.IndexOf("-");
                int intSecondDash = strEntireServiceName.IndexOf("-", intFirstDash + 1);
                strEnvironment = strEntireServiceName.Substring(intFirstDash + 1, (intSecondDash - intFirstDash) - 1).ToLower(); // Get text between "-"'s
            }
            else
            {
                int intFirstDash = strEntireServiceName.IndexOf("-") + 1;
                int intRestToGet = strEntireServiceName.Length - intFirstDash;
                strEnvironment = strEntireServiceName.Substring(intFirstDash, intRestToGet).ToLower(); // Get text after first "-"
            }

            strEnvironment = strEnvironment.Trim(); // Finally, remove any leading/trailing spaces
            return strEnvironment.ToLower();
        }
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
        public JsonResult funReturnJson(List<string> lstToReturn)
        {
            var jsonResult = Json(lstToReturn); // Required to return JSON data
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet; // Required to return JSON data
            return jsonResult;
        } // Done. 


    }
}


