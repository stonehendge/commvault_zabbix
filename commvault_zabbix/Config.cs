using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Reflection;

namespace commvault_zabbix
{
    public static class config_param
    {
        public static string user = "";
        public static string pwd = "";
        public static string url = "";
        public static string domain = "";
        public static string client_group = "";
        public static string filter_subclient_app = "";
    }
    public class Config
    {
        public Config()
        {
            var dir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            try
            {
                XDocument conf = XDocument.Load(dir + @"\config.xml");

                foreach (XElement xe in conf.Descendants("login"))
                {

                    if (xe.Element("user").Value != null)
                    {
                        config_param.user = xe.Element("user").Value;
                    }
                    else { continue; }

                    if (xe.Element("pwd").Value != null)
                    {
                        config_param.pwd = Decrypt(xe.Element("pwd").Value, "KeYPassPhrase");
                    }
                    else { continue; }
                    if (xe.Element("domain").Value != null)
                    {
                        config_param.domain = xe.Element("domain").Value;
                    }
                    else { continue; }
                    if (xe.Element("url").Value != null)
                    {
                        config_param.url = xe.Element("url").Value;
                    }
                    else { continue; }
                    if (xe.Element("client_group").Value != null)
                    {
                        config_param.client_group = xe.Element("client_group").Value;
                    }
                    else { continue; }
                    if (xe.Element("filter_subclient_app").Value != null)
                    {
                        config_param.filter_subclient_app = xe.Element("filter_subclient_app").Value;
                    }
                    else { continue; }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                System.Environment.Exit(1);
            }
        }


        public static string Decrypt(string encodedText, string key)
        {
            TripleDESCryptoServiceProvider desCryptoProvider = new TripleDESCryptoServiceProvider();
            MD5CryptoServiceProvider hashMD5Provider = new MD5CryptoServiceProvider();

            byte[] byteHash;
            byte[] byteBuff;

            byteHash = hashMD5Provider.ComputeHash(Encoding.UTF8.GetBytes(key));
            desCryptoProvider.Key = byteHash;
            desCryptoProvider.Mode = CipherMode.ECB; //CBC, CFB
            byteBuff = Convert.FromBase64String(encodedText);

            string plaintext = Encoding.UTF8.GetString(desCryptoProvider.CreateDecryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
            return plaintext;
        }
    }
}
