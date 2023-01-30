using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Common
{
    public class MongoInfo
    {
        public string uri { get; set; }
        public string database { get; set; }
        public MongoInfo() { }
    }

    public class RabbitMQInfo
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }

        public RabbitMQInfo() { }
    }

    public class ConnectionInfos
    {
        public MongoInfo mongodb { get; set; }
        public RabbitMQInfo rabbitmq { get; set; }

        public ConnectionInfos() { }
    }
}
