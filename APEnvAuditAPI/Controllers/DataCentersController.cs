using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using System.IO;
using System.Collections;
using System.Configuration;
using System.Net;
using Newtonsoft.Json;
using System.Dynamic;

namespace APEnvAuditAPI.Controllers
{
    public class DataCentersController : Controller
    {
        /* Usage:
         * Return ALL unique & lowercase Service Names as Json: http://localhost:56414/DataCenters/
         * 
         * 
         * 
         */

        // Settings:
        private List<string> lstDataCenterList = new List<string>(); // DC list container
        private List<string> lstServiceList = new List<string>(); // Services list container
        private string strEnlistmentPath = ConfigurationManager.AppSettings["strEnlistmentPath"];
        private string strAppURL = ConfigurationManager.AppSettings["strAppURL"];

        public ActionResult Index() // Done: Return ALL Data Center names 
        {


            // DC list is made from list of directories found in root directory:
            string strEnlistmentPath = ConfigurationManager.AppSettings["strEnlistmentPath"];
            if (Directory.Exists(strEnlistmentPath))
            {
                string[] aryAllDCs = Directory.GetDirectories(strEnlistmentPath);
                foreach (string strDCFolder in aryAllDCs)
                {
                    lstDataCenterList.Add(Path.GetFileName(strDCFolder)); // Adds FOLDER name to list
                }
            }
            else
            {
                // ERROR: path not valid!
                lstDataCenterList.Add("ERROR: Invalid Enlistment Path");
            }

            // Return List:
            //var jsonResult = Json(lstDataCenterList); // Required to return JSON data
            var jsonResult = Newtonsoft.Json.JsonConvert.SerializeObject(lstDataCenterList);
            //jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet; // Required to return JSON data
            return Content(jsonResult);
        }
        public JsonResult GetAServicesDataCenterList(string strServiceToLocate, string strEnvironmentToLocate) // Done. Return all DCs where a Service exists:
        {   /* Test URI: http://localhost:56414/DataCenters/GetAServicesDataCenterList?strServiceToLocate=MapsDirections&strEnvironmentToLocate=Prod
            Services named like: (but, not always...)
            [ServiceName]-[environment: INT,TEST,PROD,SANDBOX]-[datacenter: bn1,co2, etc.]-[other text: FRA,GRU,NYC,etc.]
            ServiceName appears to always have a "-", or nothing, after it.

            Examples:
            MapsDirections-BJ1
            MapsDirections-Prod-BJ1
            MapsDirections-Test-BJ1
            MapsDirections-Perf-CO4

            */

            // Get a list of ALL DCs from DCs API:
            string objJSON = "";
            string strGetDCsURL = strAppURL + @"DataCenters";
            List<string> lstMatchingDCsList = new List<string>(); // Matching DCs list container

            // Populate
            using (WebClient webClient = new WebClient()) // USING WebClient to destroy object after use.
            {
                webClient.UseDefaultCredentials = true;
                objJSON = webClient.DownloadString(strGetDCsURL);
                // JSON references:
                // http://stackoverflow.com/questions/2246694/how-to-convert-json-object-to-custom-c-sharp-object
                // http://www.newtonsoft.com/json/help/html/SerializingJSON.htm (using now)
            }

            List<string> lstDCFolderListToCheck = new List<string>(); // DC list container
            lstDCFolderListToCheck = JsonConvert.DeserializeObject<List<string>>(objJSON); // Convert resultant JSON to List<>

            // Iterate through each DC folder to locate our service:
            foreach (string strDCFolderWereChecking in lstDCFolderListToCheck)
            {
                // Get entire folder/services list for this DC: D:\Enlistments\APGold\autopilotservice\Bn1\[XXX]
                List<string> lstThisDCsServiceList = Directory.GetDirectories(strEnlistmentPath + strDCFolderWereChecking).ToList(); // Adds FOLDER names to list

                // If the service folder AND environment is present, then add this DC to the lstMatchingDCsList list:
                foreach (string strFolderNameAndPath in lstThisDCsServiceList)
                {
                    string strFolderName = Path.GetFileName(strFolderNameAndPath); // strip path chars
                    string strShortenedFolderName = funNormalizeServiceName(strFolderName); // Normalize to just the Service name
                    string strEnvironmentUnderTest = funGetServiceEnvironment(strFolderName); // extract the environment we want to find from the folder name

                    // Format folder/service name then add or exclude each service to results list:
                    if (strEnvironmentUnderTest.ToLower() == strEnvironmentToLocate.ToLower() && strShortenedFolderName.ToLower() == strServiceToLocate.ToLower()) // Service folder AND Environment match, so add this DC to the lstMatchingDCsList list:
                    { // Gotta match so add this DC to the returned list
                        lstMatchingDCsList.Add(funNormalizeServiceName(strFolderNameAndPath));
                    }
                }
            }

            // OK, now de-dup the normalized DC list:
            lstMatchingDCsList = lstMatchingDCsList.Distinct().ToList();

            // Return List:
            var varJson = funReturnJson(lstMatchingDCsList); // Return JSON data
            return varJson;
        }
        public JsonResult GetaDCsServiceList(string strDCToSearch) // Done. Return ALL unique service names within a Data Center
        {   // URI: http://localhost:56414/DataCenters/GetaDCsServiceList?strDCtoSearch=bn1
            // strDCsToSearch D:\Enlistments\APGold\autopilotservice\

            string strEnlistmentPath = ConfigurationManager.AppSettings["strEnlistmentPath"];
            string[] aryAllDCs = Directory.GetDirectories(strEnlistmentPath + @"\" + strDCToSearch);
            foreach (string strDCFolder in aryAllDCs)
            {
                lstServiceList.Add(Path.GetFileName(funNormalizeServiceName(strDCFolder)).ToLower()); // Adds FOLDER name to list
            }

            // OK, now de-dup & sort the normalized service list:
            lstServiceList = lstServiceList.Distinct().ToList();

            // Return List:
            var varJson = funReturnJson(lstServiceList); // Return JSON data
            return varJson;
        }

        // POST: DataCenters/ListDataCenters
        [HttpPost]
        public ActionResult ListDataCenters(FormCollection collection) // ToDo: return ALL DCs for a Service & Env
        {
            // Where I'm at:
            // Iterate form collection "aptestrajat/dev" 
            //

            // Recreate next Views Data Model components:
            dynamic dynViewModel = new ExpandoObject(); // Data Model/object "type" to pass to next View.
            List<Models.ServiceModel.objService> lstSelectedServicesEnvironmentDCList = new List<Models.ServiceModel.objService>(); // Enpty list of Service Objects we'll fill
            int intServiceCounter = -1; // Service counter for adding a list rows objects properties later.
            try // iterate through checked Service & Environments, recreate Model objects, & add Data Center list to each Environment object for next View:
            {
                
                string strServiceNameWeJustProcessed = "";

                foreach (string strSelectedServiceAndEnvironment in collection)
                {   // Get Form field values & split: EX: "aptestrajat/dev" [0],[1]
                    string[] aryFormValues = strSelectedServiceAndEnvironment.Split('/');
                    string strSelectedServiceName = aryFormValues[0];
                    string strSelectedEnvironmentName = aryFormValues[1];
                    Models.ServiceModel.objService objService = null; // "...= null;" required to get compiler to let us reference the object outside an IF block, where they are instantiated.
                    Models.ServiceModel.objEnvironment objEnvironment = null; // "...= null;" required to get compiler to let us reference the object outside an IF block, where they are instantiated.
                    List<Models.ServiceModel.objEnvironment> lstEnvironments = null; // "...= null;" required to get compiler to let us reference the object outside an IF block, where they are instantiated.    
                    string strServiceEnvironmentWeJustProcessed = "";

                    // Are we processing the First or Next selected Service:
                    if (strSelectedServiceName != strServiceNameWeJustProcessed)
                    {   // YES, so make a new Service object & Env. list for that object:
                        objService = new Models.ServiceModel.objService { strServiceName = strSelectedServiceName }; // Make the Service object
                        //lstEnvironments = new List<Models.ServiceModel.objEnvironment>(); // Temp Environment object list
                        strServiceNameWeJustProcessed = strSelectedServiceName;
                        lstSelectedServicesEnvironmentDCList.Add(objService);
                        intServiceCounter++;
                    }

                    // Create & add the ENVs & DCs lists:
                    // Get DCs list for this Service & ENV combo, from DataCenters API: (DataCenters/GetAServicesDataCenterList)
                    string objJSON = "";

                    using (WebClient webClient = new WebClient()) // USING WebClient to destroy object after use.
                    {
                        string strGetDCsURL = strAppURL + @"DataCenters/GetAServicesDataCenterList?strServiceToLocate=" + strSelectedServiceName + @"&strEnvironmentToLocate=" + strSelectedEnvironmentName;
                        webClient.UseDefaultCredentials = true;
                        objJSON = webClient.DownloadString(strGetDCsURL);
                        // http://localhost:56414/DataCenters/GetAServicesDataCenterList?strServiceToLocate=CloudSearch&strEnvironmentToLocate=prod
                        //lstAllDCs = json.ToArray();

                        // JSON references:
                        // http://www.newtonsoft.com/json/help/html/SerializingJSON.htm (using now)
                    }

                    List<string> lstThisEnvironmentsDCPath = new List<string>(); // Temp Data Centers Path container
                    lstThisEnvironmentsDCPath.AddRange(JsonConvert.DeserializeObject<List<string>>(objJSON)); // Convert resultant JSON to List<>
                                                                                                              // List row ex: "D:\\Enlistments\\APGold\\autopilotservice\\BJ1\\ApTemp" & "D:\\Enlistments\\APGold\\autopilotservice\\Ch1b\\ApTemp"
                    List<string> lstThisEnvironmentsDCNames = new List<string>(); // Temp Data Centers Names container
                    foreach (string strDCPath in lstThisEnvironmentsDCPath)
                    {
                        lstThisEnvironmentsDCNames.Add(funGetDCFromServicePath(strDCPath)); // Get just the DC Name from the full path.
                    }

                    // Make a new Environment object for it's DC list
                    objEnvironment = new Models.ServiceModel.objEnvironment { strEnvironmentName = strSelectedEnvironmentName }; // Make the new Env. object
                    
                    // Add the DC list to a new Environment object
                    objEnvironment.strDataCenterNames.AddRange(lstThisEnvironmentsDCNames);

                    // Add the Environment object to the Environment objects list
                    lstSelectedServicesEnvironmentDCList[intServiceCounter].lstEnvironments.Add(objEnvironment);

                    strServiceEnvironmentWeJustProcessed = strSelectedEnvironmentName; // Set the flag to bypass loop where object creation occurs.
                }

                // OK, now de-dup & sort the list:
                //lstSelectedServicesEnvironmentDCList = lstSelectedServicesEnvironmentDCList.Distinct().ToList();

                dynViewModel.lstSelectedServicesAndEnvironments = lstSelectedServicesEnvironmentDCList;
                // Return List:
                return View(dynViewModel);

            }
            catch (Exception ex)
            {
                Models.ServiceModel.objService objService = new Models.ServiceModel.objService { strServiceName = ex.Message }; // Make the Service object
                lstSelectedServicesEnvironmentDCList.Add(objService);

                return View(lstSelectedServicesEnvironmentDCList);
            }


        }

        // Functions:
        public JsonResult funReturnJson(List<string> lstToReturn)
        {
            var jsonResult = Json(lstToReturn); // Required to return JSON data
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet; // Required to return JSON data
            return jsonResult;
        } // Done. Convert passed param to JSon

        public string funNormalizeServiceName(string strEntireServiceName) // Done. Return just the Service Name portion of a complete passed service folder name
        {
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
            return strServiceNameStripped;
        }

        public string funGetServiceEnvironment(string strEntireServiceName) // Done. Return just the Environment Name portion of a complete passed Service folder Name
        {
            string strEnvironmentNameFound = "";
            if (strEntireServiceName.IndexOf("-") > -1) // to avoid exception when "-" is not present.
            {
                strEnvironmentNameFound = strEntireServiceName.Substring(strEntireServiceName.IndexOf("-") + 1); // Get everything after first "-"
                if (strEnvironmentNameFound.IndexOf("-") > -1) // to avoid exception when "-" is not present.
                {
                    strEnvironmentNameFound = strEnvironmentNameFound.Substring(0, strEnvironmentNameFound.IndexOf("-")); // Get everything before last "-"
                }
            }
            else // No Env in folder name, so:
            {
                strEnvironmentNameFound = "Undetermined"; // Couldn't get it from the folder name, shoud we try Env.ini file???
            }
            strEnvironmentNameFound = strEnvironmentNameFound.Trim(); // Finally, remove any leading/trailing spaces
            return strEnvironmentNameFound;
        }
        public string funGetDCFromServicePath(string strEntireServicePath)
        {   // EX: "D:\\Enlistments\\APGold\\autopilotservice\\BJ1\\ApTemp"
            string strDataCenterName = "";
            if (strEntireServicePath.IndexOf("autopilotservice") > -1) // to avoid exception when "\" is not present.
            {
                int intStartOfDCName = strEntireServicePath.IndexOf("autopilotservice") +17;
                int intEndOfDCName = strEntireServicePath.IndexOf(@"\", intStartOfDCName); // value,start
                strDataCenterName = strEntireServicePath.Substring(intStartOfDCName, (intEndOfDCName - intStartOfDCName)).ToLower(); // start, length. Get text between "-"'s
            }
            else
            {
                strDataCenterName = "Unknown";
            }
            strDataCenterName = strDataCenterName.Trim(); // Finally, remove any leading/trailing spaces
            return strDataCenterName.ToLower();
        }
    }
}