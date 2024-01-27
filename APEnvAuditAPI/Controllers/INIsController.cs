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
using IniParser; // https://github.com/rickyah/ini-parser
using IniParser.Model;

namespace APEnvAuditAPI.Controllers
{
    public class INIsController : Controller
    {
        // WHERE I'M AT:
        // 

        // Global Params:
        private List<string> lstINIs = new List<string>(); // DC list container
        private string strEnlistmentPath = ConfigurationManager.AppSettings["strEnlistmentPath"];
        private string strAppURL = ConfigurationManager.AppSettings["strAppURL"];

        private List<string> lstDataCenterList = new List<string>(); // DC list container
        private List<string> lstServiceList = new List<string>(); // Services list container
        
        // VIEW Settings
        private string strDefaultCheckedINI = "Environment"; // Default checked INI file for drop down list building in View (next rev)

        public ActionResult Index(string strINIFilePath) // Done: Return ALL .INI content for a provided path:
        {
            // Recreate next Views Data Model components:
            dynamic dynViewModel = new ExpandoObject(); // Data Model/object "type" to pass to next View.
            IniParser.Model.IniData INIFileContents = new IniParser.Model.IniData(); // Enpty INI Object we'll fill

            // Get the content:
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(strINIFilePath);

            // Return List:
            var varJson = Json(data); // Required to return JSON data
            varJson.JsonRequestBehavior = JsonRequestBehavior.AllowGet; // Required to return JSON data
            return varJson;
        }

        public JsonResult GetINIListForaServiceDCandEnvironment(string strServiceName, string strDCToSearch, string strEnvironmentTarget) // Done. Return ALL INIs for a Service,DC, & Env.
        {   /* Example URI: http://localhost:56414/INIs/GetINIListForaServiceDCandEnvironment?strServiceName=CloudSearch&strDCtoSearch=bn1&strEnvironmentTarget=prod

            Where I'm at:
            using folder name to get env
            Testing... strServiceName=ControlsPlatform&strDCToSearch=bn1&strEnvironment=production

             Actual Paths:
             D:\Enlistments\APGold\autopilotservice\Bn1\ControlsPlatform-Prod-Bn1
             D:\Enlistments\APGold\autopilotservice\Bn1\ControlsPlatform - Test - Bn1
             D:\Enlistments\APGold\autopilotservice\Bn1\ControlsPlatform - PPE - Bn1
             D:\Enlistments\APGold\autopilotservice\bn1\ControlsPlatform2-PPE-Bn1
             
            */
            string strServiceNameStripped = funNormalizeServiceName(strServiceName); // Isolate "base" Service Name
                        
            string[] aryPossibleServiceFolders = Directory.GetDirectories(strEnlistmentPath + @"\" + strDCToSearch, strServiceNameStripped + "*"); // find ALL folders with service name (sometimes the service names are similar; "ControlsPlatform" vs "ControlsPlatform2")

            foreach (var strFolderNameAndPath in aryPossibleServiceFolders)
            {
                string strFolderName = Path.GetFileName(strFolderNameAndPath); // Result: "ControlsPlatform-PPE-Bn1" or "Acceptance- Test"
                string strFolderNameStripped = funNormalizeServiceName(strFolderName); // Result: "controlsplatform" or "acceptance"
                string strEnvironmentUnderTest = funGetEnvFromServiceName(strFolderName); // Result: "ppe"

                if (strFolderNameStripped == strServiceNameStripped) // Is it a service folder we want to look in? (sometimes the service names are similar; "ControlsPlatform" vs "ControlsPlatform2")
                {   // Yep! So, look for DESIRED environment by searching through each folders Environment.INI file for Type=[Environment] tag

                    if (strEnvironmentUnderTest == strEnvironmentTarget) // is it an Environment folder we want to look in?
                    { // Yep, so make file list to look for INIs:
                        string[] files = Directory.GetFiles(strFolderNameAndPath, "*.ini");

                        foreach (string strThisFounedINI in files)
                        { // YEP! so add it:
                            lstINIs.Add(strThisFounedINI); // Add this FOLDER name to list
                        }
                    }


                    //using (var reader = new StreamReader(strFolderNameAndPath + @"\environment.ini"))
                    //{
                    //    var hasResult = false;
                    //    while (!reader.EndOfStream)
                    //    {
                    //        var line = reader.ReadLine();
                    //        if (!hasResult)
                    //        {
                    //            if (line.StartsWith("Type="))
                    //            {
                    //                hasResult = true; // we've reached the flag, so stop reading file

                    //                // Get tag value for comparison:
                    //                string strThisINIsTagValue = line.Substring(5, 3);
                    //                /* Is it the one we want?

                    //                    Possible Environments:
                    //                    Production - prod
                    //                    Integration - int
                    //                    Development - dev, sandbox
                    //                    Test - test, ppe 

                    //                    from an INI file:
                    //                    ; Name              -  Type
                    //                    ; Description       -  This field specifies the intended use or purpose of
                    //                    ;                      the environment.
                    //                    ; Units             -  Text
                    //                    ; Values            -  Must be 1 of:
                    //                    ;                      - "Production" - production
                    //                    ;                      - "INT" - integration
                    //                    ;                      - "PPE" - pre-production
                    //                    ;                      - "Test" - used for testing
                    //                    ;                      - "Dev" - used for development
                    //                    ;                      - "Sandbox" - temporary dev/test environment
                    //                    Type=PPE
                    //                 */

                    //                switch (strThisINIsTagValue.ToLower())
                    //                {
                    //                    case "pro":
                    //                        strThisINIsEnvironment = "Production";
                    //                        break;
                    //                    case "int":
                    //                        strThisINIsEnvironment = "Integration";
                    //                        break;
                    //                    case "ppe":
                    //                        strThisINIsEnvironment = "Pre-production";
                    //                        break;
                    //                    case "tes":
                    //                        strThisINIsEnvironment = "Testing";
                    //                        break;
                    //                    case "dev":
                    //                        strThisINIsEnvironment = "Development";
                    //                        break;
                    //                    case "san":
                    //                        strThisINIsEnvironment = "Sandbox";
                    //                        break;
                    //                    default:
                    //                        strThisINIsEnvironment = "Unknown";
                    //                        break;
                    //                }
                    //            }
                    //        }
                    //    } // END while (!reader.EndOfStream)
                    //}
                }

                
            } // END foreach (var strFolderNameAndPath in aryPossibleServiceFolders)

            // Return List:
            var varJson = funReturnJson(lstINIs); // Return JSON data
            return varJson;
        } // END JsonResult GetINIListForaServiceDCandEnvironment()

        // POST: INIs/ListINIs
        [HttpPost]
        public ActionResult ListINIs(FormCollection collection) // Done: Return INI list w/path
        {
            // Receive SVC/ENV/DC List > Populate INI file list

            // Recreate next Views Data Model components:
            dynamic dynViewModel = new ExpandoObject(); // Data Model/object "type" to pass to next View.
            List<Models.INIModel.objService> lstSelectedServicesEnvironmentINIList = new List<Models.INIModel.objService>(); // Enpty list of Service Objects we'll fill
            //List<Models.INIModel.objINIFile> lstINIFiles = new List<Models.INIModel.objINIFile>(); // Enpty list of INI Objects we'll fill
            int intServiceCounter = -1; // Service counter for adding a list rows objects properties later.

            try // iterate through checked Service & Environments, recreate Model objects, & add Data Center list to each Environment object for next View:
            {
                string strServiceNameWeJustProcessed = "";
                foreach (string strSelectedServiceEnvironmentAndDCs in collection)
                {   // Get Form field values & split: EX: "acceptance/test/bn4c" [0],[1],[2]
                    string[] aryFormValues = strSelectedServiceEnvironmentAndDCs.Split('/');
                    string strSelectedServiceName = aryFormValues[0];
                    string strSelectedEnvironmentName = aryFormValues[1];
                    string strSelectedDataCenterName = aryFormValues[2];

                    Models.INIModel.objService objService = null; // "...= null;" required to get compiler to let us reference the object outside an IF block, where they are instantiated.
                    Models.INIModel.objEnvironment objEnvironment = null; // "...= null;" required to get compiler to let us reference the object outside an IF block, where they are instantiated.
                    List<Models.INIModel.objEnvironment> lstEnvironments = null; // "...= null;" required to get compiler to let us reference the object outside an IF block, where they are instantiated.    
                    string strEnvironmentNameWeJustProcessed = "";
                    

                    /************* POPULATE SERVICE OBJECT ****************/
                    // Are we processing the First or Next selected Service:
                    if (strSelectedServiceName != strServiceNameWeJustProcessed)
                    {   // YES, so make a new Service object & Env. list for that object:
                        objService = new Models.INIModel.objService { strServiceName = strSelectedServiceName }; // Make the Service object
                        //lstEnvironments = new List<Models.INIModel.objEnvironment>(); // Temp Environment object list
                        strServiceNameWeJustProcessed = strSelectedServiceName;
                        lstSelectedServicesEnvironmentINIList.Add(objService);
                        intServiceCounter++;
                    }

                    /************* POPULATE INIFILE OBJECT ****************/

                        Models.INIModel.objINIFile objINIFile = null; // "...= null;" required to get compiler to let us reference the object outside an IF block, where they are instantiated.
                                                                      // Create & populate the INIs list:
                    
                    // API: GetINIListForaServiceDCandEnvironment(string strServiceName, string strDCToSearch, string strEnvironmentTarget)
                    string objJSON = "";

                    using (WebClient webClient = new WebClient()) // USING WebClient to destroy object after use.
                    {
                        string strGetDCsURL = strAppURL + @"INIs/GetINIListForaServiceDCandEnvironment?strServiceName=" + strSelectedServiceName + @"&strDCToSearch=" + strSelectedDataCenterName + @"&strEnvironmentTarget=" + strSelectedEnvironmentName;
                        webClient.UseDefaultCredentials = true;
                        objJSON = webClient.DownloadString(strGetDCsURL);

                        // JSON references:
                        // http://www.newtonsoft.com/json/help/html/SerializingJSON.htm (using now)
                    }

                    List<string> lstThisServiceEnvironmentDCandINIPath = new List<string>(); // Temp container
                    lstThisServiceEnvironmentDCandINIPath.AddRange(JsonConvert.DeserializeObject<List<string>>(objJSON)); // Convert resultant JSON to List<>


                    // INI files found for this service/dc/env combo:
                    List<Models.INIModel.objINIFile> lstINIFiles = new List<Models.INIModel.objINIFile>(); // Temp INI list
                    foreach (string strINIPath in lstThisServiceEnvironmentDCandINIPath)
                    {
                        //lstINIFiles.Add(strINIFileName = Path.GetFileNameWithoutExtension(strINIPath)); // Get just the DC Name from the full path.

                        // Make a new INI object for it's list for the View page
                        objINIFile = new Models.INIModel.objINIFile { strINIFileName = Path.GetFileNameWithoutExtension(strINIPath),strINIFilePath = strINIPath };

                        // Add the INI to a list
                        //objINIFile. strDataCenterNames.AddRange(lstINIFileNamesOnly);

                        // Add the INI File object to the temp INI objects list
                        lstINIFiles.Add(objINIFile);
                    }

                    /************* POPULATE ENVIRONMENT OBJECT ****************/
                    // Are we processing the First or Next selected Environment:
                    if (strSelectedEnvironmentName != strEnvironmentNameWeJustProcessed)
                    {   // YES, so make a new ENV object:
                        // Make a new Environment object for it's INIFile list
                        objEnvironment = new Models.INIModel.objEnvironment { strEnvironmentName = strSelectedEnvironmentName, lstINIFiles = lstINIFiles }; // Make the new Env. object & add INI list
                        
                    }
                    // Add the INI list to the Env object:
                    //objEnvironment.lstINIFiles.AddRange(lstINIFiles);

                    // Add the Environment object to the Environment objects list of the current Service
                    lstSelectedServicesEnvironmentINIList[intServiceCounter].lstEnvironments.Add(objEnvironment);

                    strEnvironmentNameWeJustProcessed = strSelectedEnvironmentName; // Set the flag to bypass loop where object creation occurs.

                    strServiceNameWeJustProcessed = strSelectedServiceName; // Set the flag to bypass loop where object creation occurs.
                }

                // OK, now de-dup & sort the list:
                //lstSelectedServicesEnvironmentINIList = lstSelectedServicesEnvironmentINIList.Distinct().ToList();

                dynViewModel.lstServicesEnvironmentINIList = lstSelectedServicesEnvironmentINIList;
                // Return List:
                return View(dynViewModel);

            }
            catch (Exception ex)
            {
                Models.ServiceModel.objService objService = new Models.ServiceModel.objService { strServiceName = ex.Message }; // Make the Service object
                //lstINIFiles.Add(objService);

                return View(objService);
            }


        }

        public ActionResult DisplayINIContents(FormCollection collection) // ToDo: Return INI content object list 
        {
            // Receive SVC/ENV/INI List > Populate INI content objects list
            // Where I'm at:
            // 1. Using passed list from form (service/env/[INI file path]), get INI file contents (Section/Key/Value(s)) and show in next View
            // 2. Make ActionResult "EditINIContents", in this controller, to process those changes (if any)
            //
            // Example of 1 returned Form data row: "rewards/int/D:\\Enlistments\\APGold\\autopilotservice\\\\bn1\\Rewards-INT-Bn1\\Environment.ini"

            // Recreate next Views Data Model components:
            dynamic dynViewModel = new ExpandoObject(); // Data Model/object "type" to pass to next View.
            List<IniParser.Model.IniData> lstINIFileContents = new List<IniParser.Model.IniData>(); // Enpty list of Service Objects we'll fill
            //List<Models.INIModel.objINIFile> lstINIFiles = new List<Models.INIModel.objINIFile>(); // Enpty list of INI Objects we'll fill
            int intServiceCounter = -1; // Service counter for adding a list rows objects properties later.

            try // iterate through checked Service & Environments, recreate Model objects, & add Data Center list to each Environment object for next View:
            {
                string strServiceNameWeJustProcessed = "";
                foreach (string strSelectedServiceEnvironmentAndDCs in collection)
                {   // Get Form field values & split: EX: "acceptance/test/bn4c" [0],[1],[2]
                    string[] aryFormValues = strSelectedServiceEnvironmentAndDCs.Split('/');
                    string strSelectedServiceName = aryFormValues[0];
                    string strSelectedEnvironmentName = aryFormValues[1];
                    string strSelectedINIFilePath = aryFormValues[2];
                    
                    string strServiceEnvironmentWeJustProcessed = "";

                    /****************** uncomment below *********************/
                    ///************* POPULATE SERVICE OBJECT ****************/
                    //// Are we processing the First or Next selected Service:
                    //if (strSelectedServiceName != strServiceNameWeJustProcessed)
                    //{   // YES, so make a new Service object & Env. list for that object:
                    //    objService = new Models.INIModel.objService { strServiceName = strSelectedServiceName }; // Make the Service object
                    //    //lstEnvironments = new List<Models.INIModel.objEnvironment>(); // Temp Environment object list
                    //    strServiceNameWeJustProcessed = strSelectedServiceName;
                    //    lstSelectedServicesEnvironmentINIList.Add(objService);
                    //    intServiceCounter++;
                    //}

                    ///************* POPULATE INIFILE OBJECT ****************/
                    //Models.INIModel.objINIFile objINIFile = null; // "...= null;" required to get compiler to let us reference the object outside an IF block, where they are instantiated.
                    // Create & populate the INIs list:
                    // API: INIs(string strINIFilePath)
                    string objJSON = "";

                    using (WebClient webClient = new WebClient()) // USING WebClient to destroy object after use.
                    {
                        string strGetINIFile = strAppURL + @"INIs?strINIFilePath=" + strSelectedINIFilePath;
                        webClient.UseDefaultCredentials = true;
                        objJSON = webClient.DownloadString(strGetINIFile);

                        // JSON references:
                        // http://www.newtonsoft.com/json/help/html/SerializingJSON.htm (using now)
                    }

                    //List<string> lstThisServiceEnvironmentDCandINIPath = new List<string>(); // Temp container
                    //lstThisServiceEnvironmentDCandINIPath.AddRange(JsonConvert.DeserializeObject<List<string>>(objJSON)); // Convert resultant JSON to List<>


                    //// INI files found for this service/dc/env combo:
                    //List<Models.INIModel.objINIFile> lstINIFiles = new List<Models.INIModel.objINIFile>(); // Temp INI list
                    //foreach (string strINIPath in lstThisServiceEnvironmentDCandINIPath)
                    //{
                    //    //lstINIFiles.Add(strINIFileName = Path.GetFileNameWithoutExtension(strINIPath)); // Get just the DC Name from the full path.

                    //    // Make a new INI object for it's list for the View page
                    //    objINIFile = new Models.INIModel.objINIFile { strINIFileName = Path.GetFileNameWithoutExtension(strINIPath), strINIFilePath = strINIPath };

                    //    // Add the INI to a list
                    //    //objINIFile. strDataCenterNames.AddRange(lstINIFileNamesOnly);

                    //    // Add the INI File object to the temp INI objects list
                    //    lstINIFiles.Add(objINIFile);
                    //}

                    ///************* POPULATE ENVIRONMENT OBJECT ****************/
                    //// Make a new Environment object for it's INIFile list
                    //objEnvironment = new Models.INIModel.objEnvironment { strEnvironmentName = strSelectedEnvironmentName, lstINIFiles = lstINIFiles }; // Make the new Env. object & add INI list

                    //List<string> lstThisEnvironmentsDCPath = new List<string>(); // Temp Data Centers Path container
                    //lstThisEnvironmentsDCPath.AddRange(JsonConvert.DeserializeObject<List<string>>(objJSON)); // Convert resultant JSON to List<>
                    //                                                                                          // List row ex: "D:\\Enlistments\\APGold\\autopilotservice\\BJ1\\ApTemp" & "D:\\Enlistments\\APGold\\autopilotservice\\Ch1b\\ApTemp"
                    //List<string> lstThisEnvironmentsDCNames = new List<string>(); // Temp Data Centers Names container
                    //foreach (string strDCPath in lstThisEnvironmentsDCPath)
                    //{
                    //    lstThisEnvironmentsDCNames.Add(funGetDCFromServicePath(strDCPath)); // Get just the DC Name from the full path.
                    //}

                    //// Add the Environment object to the Environment objects list
                    //lstSelectedServicesEnvironmentINIList[intServiceCounter].lstEnvironments.Add(objEnvironment);

                    //strServiceEnvironmentWeJustProcessed = strSelectedEnvironmentName; // Set the flag to bypass loop where object creation occurs.
                    /****************** uncomment above *********************/
                }

                //dynViewModel.lstServicesEnvironmentINIList = lstSelectedServicesEnvironmentINIList;
                // Return List:
                return View(dynViewModel);

            }
            catch (Exception ex)
            {
                Models.ServiceModel.objService objService = new Models.ServiceModel.objService { strServiceName = ex.Message }; // Make the Service object
                //lstINIFiles.Add(objService);

                return View(objService);
            }
            return View();


        }
        // Functions:
        public string funNormalizeServiceName(string strEntireServiceName)
        {   // ToDo: Make list
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
        }

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

        public string funGetDCFromServicePath(string strEntireServicePath)
        {   // EX: "D:\\Enlistments\\APGold\\autopilotservice\\BJ1\\ApTemp"
            string strDataCenterName = "";
            if (strEntireServicePath.IndexOf("autopilotservice") > -1) // to avoid exception when "\" is not present.
            {
                int intStartOfDCName = strEntireServicePath.IndexOf("autopilotservice") + 17;
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

        public JsonResult funReturnJson(List<string> lstToReturn)
        {
            var jsonResult = Json(lstToReturn); // Required to return JSON data
            jsonResult.JsonRequestBehavior = JsonRequestBehavior.AllowGet; // Required to return JSON data
            return jsonResult;
        }

    }

    /* INI Info:
    A typical Section:

    [SearchGoldAccess]

    ; Name              - SGGroups
    ; Description       - The security groups which have access to the environment directory in
    ;                     SearchGold
    ; Units             - Text
    ; Default           - None
    ;######IMPORTANT!!! Please do NOT grant deploy permission in environment.ini. To get deployment permission for production environments please go to http://aka.ms/appermission for instruction. ######
    SGGroups=REDMOND\Sch-IndexBuild-Admins;REDMOND\Sch-Platform-All
    
    ; Name              - SGUsers
    ; Description       - The users which have access to the environment directory in SearchGold
    ; Units             - Text
    ; Default           - None
    ;######IMPORTANT!!! Please do NOT grant deploy permission in environment.ini. To get deployment permission for production environments please go to http://aka.ms/appermission for instruction. ######
    SGUsers=

     */
}
