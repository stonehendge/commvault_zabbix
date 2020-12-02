using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace commvault_zabbix
{
    class Program
    {
        /*
         usage: cv_api.exe <client_id> <subclient>

         */
        static void Main(string[] args)
        {

            Config cfg = new Config();
            cvapi api = new cvapi();

            //api.GetJobsForClient("54", "default");

            int x = 0;

            if ((args.Length > 0) && (Int32.TryParse(args[0], out x)))
            {
                if (x > 0)
                {
                    Console.Write(api.GetJobsForClient(args[0], args[1]));
                }
            }
            else //discovery option
            {
                Console.Write(api.CVGetClientsForDiscovery());
            }



        }
    }
}
