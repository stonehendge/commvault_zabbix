# commvault_zabbix
get information from Commvault jobs to zabbix

Example of usage commvault api to get backup history information.
Zabbix template creates item for backup job status, executed in last 1 day.


example of contents of config.xml file. Must be placed in directory with main executable.
Password can be encrypted with Encrypt App

```xml
<login>
<user>zbx_cmvlt</user>
<pwd>hash_pwd</pwd>
<domain>domain</domain>
<url>http://<serverCMVLT>:81/SearchSvc/CVWebService.svc/</url>
<client_group>9</client_group>
<filter_subclient_app>"SQL Server"</filter_subclient_app>
</login>
```
