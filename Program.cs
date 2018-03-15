using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Tooling.Connector;
using System.Net;
using System.Configuration;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Globalization;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.Numerics;

namespace CRMTool
{
    class Program
    {
        static class Globals
        {
            //Project Variables
            public static string PROJDIR = Environment.CurrentDirectory;
            public static string INDIR = PROJDIR + "\\In\\";
            public static string ERRDIR = PROJDIR + "\\Error\\";
            public static string OUTDIR = PROJDIR + "\\Done\\";
            public static string USERNAME;
            public static string PASSWORD;
            public static string COMP_URL;
            public static string ORG_NAME;

            //CRM Connection Variables
            public static CrmServiceClient conn;
            public static IOrganizationService crmService;
            public static void CrmConnect()
            {
                conn = new CrmServiceClient(new NetworkCredential("ad\\"+USERNAME, PASSWORD), AuthenticationType.IFD, COMP_URL, "443", ORG_NAME, true, true, null);
                if (conn.IsReady)
                {
                    Console.WriteLine("Logged In");
                }
                else
                {
                    Console.WriteLine("Failed to log in");
                }
                crmService = conn.OrganizationServiceProxy;
            }

            //File Specific Variables
            public static string ENTITY;
            public static string ACTION;
            public static List<string> SUPPORTEDACTIONS = new List<string>(){ "create", "delete", "update" };
            public static List<string> types = new List<string>();
            public static List<string> fields = new List<string>();
            public static List<Dictionary<string, string>> entitylist = new List<Dictionary<string, string>>();
            public static EntityMetadata CurrentEntityMetadata;
            public static Dictionary<string, AttributeTypeCode> legend;

        }
        
        static void FolderSetup()
        {
            System.IO.Directory.CreateDirectory(Globals.PROJDIR + "\\In");
            System.IO.Directory.CreateDirectory(Globals.PROJDIR + "\\Done");
            System.IO.Directory.CreateDirectory(Globals.PROJDIR + "\\Error");
        }

        static void getUserData()
        {
            if(File.Exists("settings.txt"))
            {
                StreamReader infile = new StreamReader("settings.txt");
                Globals.USERNAME = infile.ReadLine();
                Globals.PASSWORD = infile.ReadLine();
                Globals.COMP_URL = infile.ReadLine();
                Globals.ORG_NAME = infile.ReadLine();
                infile.Close();
            }
            else
            {
                StreamWriter settingsfile = new StreamWriter("Settings.txt");
                Console.WriteLine("Project Directory: " + Globals.PROJDIR);
                Console.WriteLine("No Settings file found. Setting Up Now.");
                Console.Write("Username: ");
                Globals.USERNAME = Console.ReadLine();
                Console.Write("Password: ");
                Globals.PASSWORD = Console.ReadLine();
                Console.Write("URL: ");
                Globals.COMP_URL = Console.ReadLine();
                Console.Write("Organization Name: ");
                Globals.ORG_NAME = Console.ReadLine();
                settingsfile.WriteLine(Globals.USERNAME);
                settingsfile.WriteLine(Globals.PASSWORD);
                settingsfile.WriteLine(Globals.COMP_URL);
                settingsfile.WriteLine(Globals.ORG_NAME);
                settingsfile.Close();
            }
        }

        static void ProcessFile(string filename)
        {

            //Open Input File
            StreamReader infile = new StreamReader(filename);

            //Read First line and define Global Action and Entity
            Globals.types = infile.ReadLine().Split(',').ToList();
            Globals.ENTITY = Globals.types[0].ToLower();
            Globals.ACTION = Globals.types[1].ToLower();

            //Entity Metadata request variables
            RetrieveEntityRequest req = new RetrieveEntityRequest { EntityFilters = EntityFilters.All, LogicalName = Globals.ENTITY };
            RetrieveEntityResponse res = new RetrieveEntityResponse();

            //Check for Entity
            try
            {
                res = (RetrieveEntityResponse)Globals.crmService.Execute(req);
                Globals.CurrentEntityMetadata = res.EntityMetadata;
                Globals.legend = getAttributes();
            }
            catch
            {
                infile.Close();
                throw new System.ArgumentException("Entity " + Globals.ENTITY + " not found. Check Spelling and capatilazation");
            }

            //Check Action
            if(!Globals.SUPPORTEDACTIONS.Contains(Globals.ACTION))
            {
                infile.Close();
                throw new ActionNotSupportedException("\"" + Globals.ACTION + "\" is not a supported");
            }

            //Read second line and convert fields to List
            Globals.fields = infile.ReadLine().Split(',').ToList();

            //Check for matching attribute fields
            foreach (string field in Globals.fields)
            {
                if(!Globals.legend.ContainsKey(field))
                {
                    infile.Close();
                    throw new Exception("\"" + Globals.ENTITY + "\" does not have an attribute " + " \"" + field + "\" associated with it, please check file.");
                }
            }

            //Read through the rest of the input file collecting each record in a list
            while (!infile.EndOfStream)
            {
                List<string> record = infile.ReadLine().Split(',').ToList();
                Dictionary<string, string> entityattr = new Dictionary<string, string>();
                for (int x = 0; x < record.Count; x++)
                {
                    if (record[x] != "")
                    {
                        entityattr.Add(Globals.fields[x], record[x]);
                    }
                }
                Globals.entitylist.Add(entityattr);
                entityattr = null;
            }
  
            //Proces each record into CRM
            foreach (Dictionary<string, string> entry in Globals.entitylist)
            {
                try
                {
                    ProcessEntry(entry);
                }
                catch
                {
                    Console.WriteLine("There was a problem with the record, Please check Error file for line.");
                    File.AppendAllText(Globals.ERRDIR + "ErrorLines_" + Path.GetFileName(filename), string.Join(",",entry.Values));
                }
                
            }

            infile.Close();
        }

        static void ProcessEntry(Dictionary<string, string> entry)
        {
            Entity ent = new Entity(Globals.ENTITY);
            switch (Globals.ACTION)
            {
                case "create":
                    try
                    {
                        Console.WriteLine("Creating " + Globals.ENTITY);
                        foreach (string key in entry.Keys)
                        {
                            Console.WriteLine(key + " : " + entry[key]);
                            ent = AddAttribute(ent, key, entry[key]);
                        }
                        //CREATE CRM ENTITY
                        Guid newEntity = Globals.crmService.Create(ent);
                        Console.WriteLine("Created " + Globals.ENTITY + ": " + newEntity.ToString());
                    }
                    catch(Exception e)
                    {
                        throw e;
                    }
                    break;

                case "update":
                    try
                    {
                        Console.WriteLine("Updating " + Globals.ENTITY);
                        ColumnSet attributes = new ColumnSet(new string[] { "firstname", "lastname" });
                        ent = Globals.crmService.Retrieve(ent.LogicalName, new Guid(entry["GUID"]), attributes);
                        foreach (string key in entry.Keys)
                        {
                            Console.WriteLine(key + " : " + entry[key]);
                            ent = AddAttribute(ent, key, entry[key]);
                        }
                        ent.Attributes.Remove("GUID");
                        //UPDATE CRM ENTITY
                        Globals.crmService.Update(ent);
                    }
                    catch(Exception e)
                    {
                        throw e;
                    }
                    break;

                case "delete":
                    try
                    {
                        Console.WriteLine("Deleting " + Globals.ENTITY + " : " + entry["GUID"]);
                        //Delete CRM Entity
                        Globals.crmService.Delete(Globals.ENTITY, new Guid(entry["GUID"]));
                    }
                    catch(Exception e)
                    {
                        throw e;
                    }
                    break;
            }
        }

        static Entity AddAttribute(Entity Entity, string attribute, string value)
        {
            Entity tempEnt = Entity;
            AttributeTypeCode datatype = Globals.legend[attribute];
            switch (datatype)
                {
                    case AttributeTypeCode.BigInt:
                        tempEnt[attribute] = BigInteger.Parse(value);
                        break;
                    case AttributeTypeCode.Boolean:
                        tempEnt[attribute] = Boolean.Parse(value);
                        break;
                    case AttributeTypeCode.DateTime:
                        tempEnt[attribute] = Convert.ToDateTime(value);
                        break;
                    case AttributeTypeCode.Decimal:
                        tempEnt[attribute] = Decimal.Parse(value);
                        break;
                    case AttributeTypeCode.Double:
                        tempEnt[attribute] = Double.Parse(value);
                        break;
                    case AttributeTypeCode.Integer:
                        tempEnt[attribute] = int.Parse(value);
                        break;
                    case AttributeTypeCode.Picklist:
                        Dictionary<string, int?> options = GetoptionsetText(attribute);
                        tempEnt[attribute] = new OptionSetValue((int)options[value]);
                        break;
                    case AttributeTypeCode.String:
                        tempEnt[attribute] = value;
                        break;
                    case AttributeTypeCode.Uniqueidentifier:
                        tempEnt[attribute] = new Guid(value);
                        break;
                    default:
                        break;
                }
            return tempEnt;
        }
        
        static Dictionary<string, AttributeTypeCode> getAttributes()
        {
            Dictionary<string, AttributeTypeCode> temp = new Dictionary<string, AttributeTypeCode>();
            foreach(AttributeMetadata attribute in Globals.CurrentEntityMetadata.Attributes)
            {
                temp[attribute.LogicalName] = (AttributeTypeCode)attribute.AttributeType;
            }
            return temp;
        }

        public static Dictionary<string,int?> GetoptionsetText(string attributeName)
        {
            Dictionary<string, int?> temp = new Dictionary<string, int?>();
            PicklistAttributeMetadata picklistMetadata = Globals.CurrentEntityMetadata.Attributes.FirstOrDefault(attribute => String.Equals(attribute.LogicalName, attributeName, StringComparison.OrdinalIgnoreCase)) as PicklistAttributeMetadata;
            OptionSetMetadata options = picklistMetadata.OptionSet;
            IList<OptionMetadata> OptionsList = options.Options.ToList();
            foreach(var option in OptionsList)
            {
                temp.Add(option.Label.UserLocalizedLabel.Label, option.Value);
                //Console.WriteLine(option.Label.UserLocalizedLabel.Label + " : " + option.Value);
            }
            //Console.WriteLine("");
            return temp;
            //string optionsetLabel = (OptionsList.First()).Label.UserLocalizedLabel.Label;
        }
        
        static void Main(string[] args)
        {
            getUserData();
            FolderSetup();
            IEnumerable<string> files = Directory.EnumerateFiles(Globals.PROJDIR + "\\In");
            if (files.Any())
            {
                Globals.CrmConnect();
                foreach (string filedir in files)
                {
                    try
                    {
                        ProcessFile(filedir);
                        File.Move(filedir, Globals.PROJDIR + "\\Done\\" + Path.GetFileName(filedir));
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                        File.Move(filedir, Globals.PROJDIR + "\\Error\\" + Path.GetFileName(filedir));
                    }
                }
            }
            else
            {
                Console.Write("No Files Found!");
            }
        }

    }
}