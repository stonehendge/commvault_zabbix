using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Diagnostics;
using System.Net;
using System.Xml;
using System.IO;
using Newtonsoft.Json;

namespace commvault_zabbix
{
    public class cvapi_zabbix
    {
        public string server { get; set; } = "";
        public string status { get; set; } = "";
        public string totalNumOfFiles { get; set; } = "";
        public string jobId { get; set; } = "";
        public string jobSubmitErrorCode { get; set; } = "";
        public string sizeOfMediaOnDisk { get; set; } = "";
        public string lastUpdateTime { get; set; } = "";
        public string percentSavings { get; set; } = "";
        public string pendingReason { get; set; } = "";
        public Int64 jobElapsedTime { get; set; } = 0;
        public string jobStartTime { get; set; } = "";
        public string backupLevelName { get; set; } = "";
        public string pendingReasonErrorCode { get; set; } = "";
        public string appTypeName { get; set; } = "";
        public string percentComplete { get; set; } = "";
    }

    public class cvapi_server
    {
        public string hostname { get; set; } = "";
        public string clientid { get; set; } = "";

    }

    public class cvapi_server_subclient
    {
        public string hostname { get; set; } = "";
        public string clientid { get; set; } = "";
        public string subclient { get; set; } = "";
    }

    public class cv_subclients
    {
        public List<string> lst_subclients { get; set; } = new List<string>();

    }

    class cvapi
    {
        String service = config_param.url; //"http://<server>:81/SearchSvc/CVWebService.svc/";
        string user = config_param.user;
        string pwd = config_param.pwd;
        string domain = config_param.domain;
        string client_group = config_param.client_group;
        string filter_subclient_app = config_param.filter_subclient_app;

        Dictionary<int, cv_subclients> cli_subcli_arr = new Dictionary<int, cv_subclients>();

        
        //Get all subclients for specified clientid
        
        public cv_subclients GetSubClients(int cid)
        {
            string token = GetSessionToken(domain, user, pwd);

            cv_subclients subclient = new cv_subclients();

            string clientPropService = service + "/Subclient?clientId=" + cid;

            HttpWebResponse ClientResp = SendRequest(clientPropService, "GET", token, null);
            if (ClientResp.StatusCode == HttpStatusCode.OK)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ClientResp.GetResponseStream());

                var subclients = xmlDoc.SelectNodes("/App_GetSubClientPropertiesResponse/subClientProperties/*");
                if (subclients != null)
                {
                    foreach (XmlNode cl in subclients)
                    {
                        if (cl.Name == "subClientEntity") //appName="SQL Server"
                        {

                            if (filter_subclient_app.Contains(cl.Attributes["appName"].Value))
                            {
                                subclient.lst_subclients.Add(cl.Attributes["subclientName"].Value.ToUpper());

                            }

                        }

                    }

                }
            }
            return subclient;
        }

        //Get all clients
        public List<cvapi_server> CVGetClients()
        {
            string token = GetSessionToken(domain, user, pwd);
            string json = "";

            List<cvapi_server> svrs = new List<cvapi_server>();

            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("Login Failed");
                Console.WriteLine("Login Failed");
            }
            else
            {
                Debug.WriteLine("Login Successful");

                //Login successful.	
             
                string clientPropService = service + "/ClientGroup/" + client_group;

                HttpWebResponse ClientResp = SendRequest(clientPropService, "GET", token, null);
                if (ClientResp.StatusCode == HttpStatusCode.OK)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(ClientResp.GetResponseStream());
                    //Parse response to get client name, host name and client description
                    //Debug.WriteLine(string.Format("Client properties response: ", xmlDoc.InnerXml));
                    Debug.WriteLine(string.Format("Job properties response: ", xmlDoc.InnerXml));

                    var clients = xmlDoc.SelectNodes("/App_PerformClientGroupResp/clientGroupDetail/*");
                    if (clients != null)
                    {
                       
                        foreach (XmlNode cl in clients)
                        {
                            if (cl.Name == "associatedClients")
                            {
                                cvapi_server obj = new cvapi_server
                                {
                                    hostname = cl.Attributes["hostName"].Value.ToUpper(), //clientName
                                    clientid = cl.Attributes["clientId"].Value
                                };

                                svrs.Add(obj);
                            }

                        }
                    }

                }

            }

            return svrs;
        }

        //processing discovery rule for zabbix
        public string CVGetClientsForDiscovery()
        {
            string json;
            cvapi api = new cvapi();
            api.CVGetClients();

            List<cvapi_server> clients = api.CVGetClients();

            List<cvapi_server_subclient> srv_subcl_cid = new List<cvapi_server_subclient>();

            foreach (cvapi_server client_id in clients)
            {
                cv_subclients lst_subclients = api.GetSubClients(Convert.ToInt32(client_id.clientid));
                foreach (string sub_client in lst_subclients.lst_subclients)
                {
                    cvapi_server_subclient new_item = new cvapi_server_subclient
                    {
                        hostname = client_id.hostname,
                        clientid = client_id.clientid,
                        subclient = sub_client
                    };
                    srv_subcl_cid.Add(new_item);
                }

            }

            json = JsonConvert.SerializeObject(srv_subcl_cid, Newtonsoft.Json.Formatting.Indented);

            json = json.Replace("\"hostname\"", "\"{#HOSTNAME_CMVLT}\"");
            json = json.Replace("\"clientid\"", "\"{#CLIENTID_CMVLT}\"");
            json = json.Replace("\"subclient\"", "\"{#SUBCLIENT_CMVLT}\"");

            return json;
        }

        //Get job results information for lastday backups
        public string GetJobs()
        {
            //1. Login
            string token = GetSessionToken(domain, user, pwd);
            string json = "";

            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("Login Failed");
            }
            else
            {
                Debug.WriteLine("Login Successful");


                cvapi api = new cvapi();
                api.CVGetClients();

                List<cvapi_server> clients = api.CVGetClients();
                List<cvapi_zabbix> lst_cavapi_zbx = new List<cvapi_zabbix>();

                foreach (cvapi_server client_ in clients)
                {

                    string clientPropService = service + "Job?clientId=" + client_.clientid + "&completedJobLookupTime=86400&jobCategory=Finished&jobFilter=backup";

                    HttpWebResponse ClientResp = SendRequest(clientPropService, "GET", token, null);
                    if (ClientResp.StatusCode == HttpStatusCode.OK)
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(ClientResp.GetResponseStream());


                        var job_summary = xmlDoc.SelectNodes("/JobManager_JobListResponse/jobs/*");
                        if (job_summary != null)
                        {
                            foreach (XmlNode job in job_summary)
                            {

                                cvapi_zabbix obj = new cvapi_zabbix
                                {
                                    server = client_.hostname,
                                    status = job.Attributes["status"].Value,
                                    totalNumOfFiles = job.Attributes["totalNumOfFiles"].Value,
                                    jobId = job.Attributes["jobId"].Value,
                                    jobSubmitErrorCode = job.Attributes["jobSubmitErrorCode"].Value,
                                    sizeOfMediaOnDisk = job.Attributes["sizeOfMediaOnDisk"].Value,
                                    lastUpdateTime = job.Attributes["lastUpdateTime"].Value,
                                    percentSavings = job.Attributes["percentSavings"].Value,
                                    pendingReason = job.Attributes["pendingReason"].Value,
                                    jobElapsedTime = Convert.ToInt64(job.Attributes["jobElapsedTime"].Value),
                                    jobStartTime = job.Attributes["jobStartTime"].Value,
                                    backupLevelName = job.Attributes["backupLevelName"].Value,
                                    pendingReasonErrorCode = job.Attributes["pendingReasonErrorCode"].Value,
                                    appTypeName = job.Attributes["appTypeName"].Value,
                                    percentComplete = job.Attributes["percentComplete"].Value
                                };

                                lst_cavapi_zbx.Add(obj);
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine(string.Format("Get Client properties request Failed. Status Code: {0}, Status Description: {1}", ClientResp.StatusCode, ClientResp.StatusDescription));
                    }

                }

                json = JsonConvert.SerializeObject(lst_cavapi_zbx, Newtonsoft.Json.Formatting.Indented);

            }

            return json;
        }

        //Get job results information for lastday backups for specified clientId
        public string GetJobsForClient(string cid, string subclient)
        {
            //1. Login
            string token = GetSessionToken(domain, user, pwd);
            string json = "";

            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("Login Failed");
            }
            else
            {
                Debug.WriteLine("Login Successful");

                List<cvapi_zabbix> lst_cavapi_zbx = new List<cvapi_zabbix>();

                string clientPropService = service + "Job?clientId=" + cid + "&completedJobLookupTime=86400&jobCategory=Finished&jobFilter=backup";
                //Job?clientId=2
                HttpWebResponse ClientResp = SendRequest(clientPropService, "GET", token, null);
                if (ClientResp.StatusCode == HttpStatusCode.OK)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(ClientResp.GetResponseStream());


                    var job_summary = xmlDoc.SelectNodes("/JobManager_JobListResponse/jobs/*");
                    if (job_summary != null)
                    {
                        //foreach (XmlAttribute job in job_summary)
                        foreach (XmlNode job in job_summary)
                        {

                            if (subclient.ToUpper() == job.Attributes["subclientName"].Value.ToUpper())
                            {
                                cvapi_zabbix obj = new cvapi_zabbix
                                {

                                    status = job.Attributes["status"].Value,
                                    totalNumOfFiles = job.Attributes["totalNumOfFiles"].Value,
                                    jobId = job.Attributes["jobId"].Value,
                                    jobSubmitErrorCode = job.Attributes["jobSubmitErrorCode"].Value,
                                    sizeOfMediaOnDisk = job.Attributes["sizeOfMediaOnDisk"].Value,
                                    lastUpdateTime = job.Attributes["lastUpdateTime"].Value,
                                    percentSavings = job.Attributes["percentSavings"].Value,
                                    pendingReason = job.Attributes["pendingReason"].Value,
                                    jobElapsedTime = Convert.ToInt64(job.Attributes["jobElapsedTime"].Value),
                                    jobStartTime = job.Attributes["jobStartTime"].Value,
                                    backupLevelName = job.Attributes["backupLevelName"].Value,
                                    pendingReasonErrorCode = job.Attributes["pendingReasonErrorCode"].Value,
                                    appTypeName = job.Attributes["appTypeName"].Value,
                                    percentComplete = job.Attributes["percentComplete"].Value
                                };

                                lst_cavapi_zbx.Add(obj);
                            }

                        }

                    }

                }
                else
                {
                    Debug.WriteLine(string.Format("Get Client properties request Failed. Status Code: {0}, Status Description: {1}", ClientResp.StatusCode, ClientResp.StatusDescription));
                }

                json = JsonConvert.SerializeObject(lst_cavapi_zbx, Newtonsoft.Json.Formatting.Indented);

            }

            return json;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
        private string GetSessionToken(string domain, string userName, string password)
        {
            string token = string.Empty;
            string loginService = service + "Login";
            byte[] pwd = System.Text.Encoding.UTF8.GetBytes(password);
            String encodedPassword = Convert.ToBase64String(pwd, 0, pwd.Length, Base64FormattingOptions.None);
            string loginReq = string.Format("<DM2ContentIndexing_CheckCredentialReq mode=\"Webconsole\" domain=\"{2}\" username=\"{0}\" password=\"{1}\" />", userName, encodedPassword, domain);
            HttpWebResponse resp = SendRequest(loginService, "POST", null, loginReq);
            //Check response code and check if the response has an attribute "token" set
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(resp.GetResponseStream());
                token = xmlDoc.SelectSingleNode("/DM2ContentIndexing_CheckCredentialResp/@token").Value;
            }
            else
            {
                Debug.WriteLine(string.Format("Login Failed. Status Code: {0}, Status Description: {1}", resp.StatusCode, resp.StatusDescription));
            }
            return token;
        }

        private HttpWebResponse SendRequest(string serviceURL, string httpMethod, string token, string requestBody)
        {
            WebRequest req = WebRequest.Create(serviceURL);
            req.Method = httpMethod;
            req.ContentType = @"application/xml; charset=utf-8";
            //build headers with the received token
            if (!string.IsNullOrEmpty(token))
                req.Headers.Add("Authtoken", token);
            if (!string.IsNullOrEmpty(requestBody))
                WriteRequest(req, requestBody);
            return req.GetResponse() as HttpWebResponse;
        }

        private void WriteRequest(WebRequest req, string input)
        {
            req.ContentLength = Encoding.UTF8.GetByteCount(input);
            using (Stream stream = req.GetRequestStream())
            {
                stream.Write(Encoding.UTF8.GetBytes(input), 0, Encoding.UTF8.GetByteCount(input));
            }
        }
    }
}
