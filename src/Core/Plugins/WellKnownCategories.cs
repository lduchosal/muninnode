namespace MuninNode.Plugins;
// 'Well known categories' are defined by Munin.
// See https://guide.munin-monitoring.org/en/latest/reference/graph-category.html
public static class WellKnownCategories
{
    public const string OneSec = "1sec";
    public const string Antivirus = "antivirus"; // Antivirus tools
    public const string Appserver = "appserver"; // Application servers
    public const string Auth = "auth"; // Authentication servers and services
    public const string Backup = "backup"; // All measurements around backup creation
    public const string Chat = "chat"; // Messaging servers
    public const string Cloud = "cloud"; // Cloud providers and cloud components
    public const string Cms = "cms"; // Content Management Systems
    public const string Cpu = "cpu"; // CPU measurements
    public const string Db = "db"; // Database servers: MySQL, PosgreSQL, MongoDB, Oracle
    public const string Devel = "devel"; // (Software) Development Tools
    public const string Disk = "disk"; // Disk and other storage measurements: : used space, free inodes, activity, latency, throughput
    public const string Dns = "dns"; // Domain Name Server
    public const string Filetransfer = "filetransfer"; // Filetransfer tools and servers
    public const string Forum = "forum"; // Forum applications
    public const string Fs = "fs"; // (Network) Filesystem activities, includes also monitoring of distributed storage appliances
    public const string Fw = "fw"; // All measurements around network filtering
    public const string Games = "games"; // Game-Server
    public const string Htc = "htc"; // High-throughput computing
    public const string Loadbalancer = "loadbalancer"; // Load balancing and proxy servers..
    public const string Mail = "mail"; // Mail throughput, mail queues, etc.: Postfix, Exim, Sendmail. For monitoring a large mail system, it makes sense to override this with configuration on the Munin master, and make graph categories for the mail roles you provide. Mail Transfer Agent (postfix and exim), Mail Delivery Agent (filtering, sorting and storage), Mail Retrieval Agent (imap server).
    public const string Mailinglist = "mailinglist"; // Listserver
    public const string Memory = "memory"; // All kind of memory measurements. Note that info about memory caching servers is also placed here
    public const string Munin = "munin"; // Monitoring the monitoring.. (includes other monitoring servers also)
    public const string Network = "network"; // General networking metrics.: interface activity, latency, number of open network connections
    public const string Other = "other"; // Plugins that address seldom used products. Category /other/ is the default, so if the plugin doesn’t declare a category, it is also shown here.
    public const string Printing = "printing"; // Monitor printers and print jobs
    public const string Processes = "processes"; // Process and kernel related measurements
    public const string Radio = "radio"; // Receivers, signal quality, recording, ..
    public const string San = "san"; // Storage Area Network
    public const string Search = "search"; // All kinds of measurement around search engines
    public const string Security = "security"; // Security information: login failures, number of pending update packages for OS, number of CVEs in the running kernel fixed by the latest installed kernel, firewall counters.
    public const string Sensors = "sensors"; // Sensor measurements of device and environment: temperature, power, devices health state, humidity, noise, vibration
    public const string Spamfilter = "spamfilter"; // Spam fighters at work
    public const string Streaming = "streaming"; // 
    public const string System = "system"; // General operating system metrics.: CPU speed and load, interrupts, uptime, logged in users
    public const string Time = "time"; // Time synchronization
    public const string Tv = "tv"; // Video devices and servers
    public const string Virtualization = "virtualization"; // All kind of measurements about server virtualization. Includes also Operating-system-level virtualization
    public const string Voip = "voip"; // Voice over IP servers
    public const string Webserver = "webserver"; // All kinds of webserver measurements and also for related components: requests, bytes, errors, cache hit rate for Apache httpd, nginx, lighttpd, varnish, hitch, and other web servers, caches or TLS wrappers.
    public const string Wiki = "wiki"; // wiki applications
    public const string Wireless = "wireless"; // 
}