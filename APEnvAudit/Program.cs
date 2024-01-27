using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
//started working
namespace APEnvAudit
{
    class Program
    {
        static void Main(string[] args)
        {
            Arguments CommandLine = new Arguments(args);
            if (CommandLine["help"] != null)
            {
                Usage();
            }
            else if (CommandLine["get"] != null)
            {
                switch (CommandLine["get"])
                {
                    case "SLB":
                        Console.WriteLine("Give path of the env.ini file");
                        break;
                    case "Firewall":
                        Console.WriteLine("Firewall_Inbound");
                        break;
                    case "Admin":
                        Console.WriteLine("SecurityGroupAccess");
                        break;
                    default:
                        Usage();
                        break;
                }
            }
            else
            {
                Usage();
            }
            try
            {
                string path = Directory.GetCurrentDirectory();
                string searchPattern = "*";
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.WriteLine("Enter a selection");
                string findvalue = Console.ReadLine();

                // get dir & all subs list
                DirectoryInfo[] cDirs = new DirectoryInfo(path).GetDirectories(searchPattern,SearchOption.AllDirectories);

                // Define search term from selection:
                string strINIFile = "\\environment.ini"; // default ini file
                switch (findvalue)
                {
                    case "1":
                        findvalue = "Firewall_Inbound";
                        break;
                    case "2":
                        findvalue = "MaintenanceDelayTime";
                        break;
                    case "3":
                        findvalue = "SecurityGroupAccess";
                        break;
                    case "4":
                        findvalue = "ServiceManager";
                        break;
                    case "5":
                        findvalue = "ExternalSecurityGroupMembership";
                        break;
                    case "6":
                        findvalue = "[APLB";
                        break;
                    case "7":
                        findvalue = "[Snat";
                        break;
                    case "8":
                        findvalue = "LogMonitor.ApSmartAgent";
                        break;
                    case "9":
                        findvalue = "[ICM]";
                        break;
                    case "10":
                        findvalue = "[DataFolders]";
                        strINIFile = "\\deployment.ini"; // Where to look
                        break;
                    default:
                        Console.WriteLine("Unrecognized selection. Exiting.");
                        Environment.Exit(0);
                        break;
                 }

                using (StreamWriter sw = new StreamWriter(path+"\\envnames.txt"))
                {
                    foreach (DirectoryInfo dir in cDirs)
                    {
                        sw.WriteLine(dir.FullName);
                    }
                }

                // Read and show each line from the file.
                string filePath = "";
                using (StreamReader sr = new StreamReader(path + "\\envnames.txt"))
                {
                    while ((filePath = sr.ReadLine()) != null)
                    {
                        filePath = filePath + strINIFile;
                        //filePath = @"H:\src\APGold\autopilotservice\test\" + filePath+"\\environment.ini";
                        ReadFile(filePath, findvalue);
                    }
                }
                File.Delete(path + "\\envnames.txt");
                Console.ResetColor();
                Console.WriteLine("Find the Audit report at {0}", path + "\\APenvAudit.txt");
            }
            catch (Exception e)
            {
                Console.WriteLine("Try to get app information failed with the follow exception {0}", e.ToString());
            }
        }

        private static void Usage()
        {
            Console.WriteLine("------------------------------Command Line Usage--------------------------------------------------");
            Console.WriteLine(" Selection     Search Value                    Result");
            Console.WriteLine("     1         Firewall_Inbound                Audit all the AP env for firewall settings");
            Console.WriteLine("     2         MaintenanceDelayTime            Audit all the AP env for MaintenanceDelayTime settings");
            Console.WriteLine("     3         SecurityGroupAccess             Audit all the AP env for SecurityGroupAccess");
            Console.WriteLine("     4         ServiceManager                  Audit all the AP env for Graceful Shutdown");
            Console.WriteLine("     5         ExternalSecurityGroupMembership Audit all the AP env for AP Managed SG");
            Console.WriteLine("     6         [APLB                           Audit all the AP env for AP SLB");
            Console.WriteLine("     7         [Snat                           Audit all the AP env for SNAT configuration");
            Console.WriteLine("     8         LogMonitor.ApSmartAgent         Audit all the AP env for SMART Agent configuration");
            Console.WriteLine("     9         [ICM]                           Audit all the AP env for SNAT configuration");
            Console.WriteLine("     10        [DataFolders]                   Audit all the AP env for AP secret store usage in deployment.ini");
            Console.WriteLine();
        }

        public static void ReadFile(string filePath, string findvalue)
        {
            StringBuilder mystring = new StringBuilder();

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    while (!reader.EndOfStream)
                    {
                        string strInfo = reader.ReadLine();

                        if (strInfo.Contains(findvalue))
                        {
                            mystring.Append(filePath);
                            mystring.AppendLine();

                            mystring.Append(strInfo);
                            mystring.AppendLine();

                            bool readingdone = false;
                            do
                            {
                                string nextline = reader.ReadLine();
                                if (!nextline.Contains("["))
                                {
                                    mystring.Append(nextline);
                                    mystring.AppendLine();
                                }
                                else
                                {
                                    readingdone = true;
                                }
                            } while (!readingdone);
                        }
                    }
                    writeFile(mystring.ToString());

                }
            }
            catch(Exception e)
            {
                writeFile("Error:####"+e.Message.ToString());
                writeFile(" ");
            }
        }

        public static void writeFile(string str)
        {
            string path = Directory.GetCurrentDirectory();
            path = path + "\\APenvAudit.txt";
            using (StreamWriter writer = File.AppendText(path))
            //using (StreamWriter writer = File.AppendText(@"c:\users\ajohri\Desktop\output.txt")) 
            {
                writer.WriteLine(str);
            }
        }

    }

}

