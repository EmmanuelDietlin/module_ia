using RabbitMQ.Client;
using System.Text.Json;
using AIModule.AI;
using MongoDB.Driver;
using MongoDB.Bson;
using AIModule.Events;
using System.Text;
using RabbitMQ.Client.Events;
using System.IO;
using AIModule.Bases;
using System.Diagnostics;
using AIModule.Reputation;
using AIModule.Common;
using AIModule.Common.constant.owner;
using System.IO.Compression;


class RunAIs
{
    public static void Main()
    {
        try
        {
            //On génère le répertoire de logs s'il n'existe pas déjà
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "logs")))
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "logs"));
            }
            //Si on a déjà un fichier AILogs, on l'archive et on en créé un nouveau
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "logs","AIlogs.txt"))) 
            {
                string file = Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs.txt");
                string newFileName = string.Format(Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs_{0}.gz"), File.GetLastWriteTime(Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs.txt")).ToString("yyyy'-'MM'-'dd'_'HH'-'mm'-'ss"));
                using FileStream originalFileStream = File.Open(file, FileMode.Open);
                using FileStream compressedFileStream = File.Create(newFileName);
                using var compressor = new GZipStream(compressedFileStream, CompressionMode.Compress);
                originalFileStream.CopyTo(compressor);
            }
            File.Create(Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs.txt"));
            var files = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "logs"), "AIlogs_*").Select(x => new FileInfo(x)).ToList();
            //On ne garde que les 10 fichiers les plus récents
            files.Sort((x, y) => y.CreationTime.CompareTo(x.CreationTime));
            while (files.Count > 10)
            {
                files[files.Count - 1].Delete();
                files.RemoveAt(files.Count - 1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        ConnectionInfos infos;
        string connectionInfos = Path.Combine(Directory.GetCurrentDirectory(),"connectionInfos.json");

        using (var reader = new StreamReader(connectionInfos))
        {
            string json = reader.ReadToEnd();
            infos = JsonSerializer.Deserialize<ConnectionInfos>(json);
        }

        try
        {
            //Si on a déjà un fichier de logs d'erreurs, on archive et on en créé un nouveau
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "logs", "Error_logs.txt")))
            {
                string file = Path.Combine(Directory.GetCurrentDirectory(), "logs", "Error_logs.txt");
                string newFileName = string.Format(Path.Combine(Directory.GetCurrentDirectory(), "logs", "Error_logs_{0}.gz"), File.GetLastWriteTime(Path.Combine(Directory.GetCurrentDirectory(), "logs", "Error_logs.txt")).ToString("yyyy'-'MM'-'dd'_'HH'-'mm'-'ss"));
                using FileStream originalFileStream = File.Open(file, FileMode.Open);
                using FileStream compressedFileStream = File.Create(newFileName);
                using var compressor = new GZipStream(compressedFileStream, CompressionMode.Compress);
                originalFileStream.CopyTo(compressor);                
            }
            var files = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "logs"), "Error_logs_*").Select(x => new FileInfo(x)).ToList();
            //On ne garde que les 10 fichiers les plus récents
            files.Sort((x, y) => y.CreationTime.CompareTo(x.CreationTime));
            while (files.Count > 10)
            {
                files[files.Count - 1].Delete();
                files.RemoveAt(files.Count - 1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        File.Create(Path.Combine(Directory.GetCurrentDirectory(), "logs", "Error_logs.txt"));

        //On créé la factory RabbitMQ
        var factory = new ConnectionFactory()
        {
            HostName = infos.rabbitmq.HostName,
            Port = infos.rabbitmq.Port,
            UserName = infos.rabbitmq.UserName,
            Password = infos.rabbitmq.Password,
            VirtualHost = infos.rabbitmq.VirtualHost
        };


        //Récupération des IAs dans la BDD 
        var aiDeltaFilter = Builders<AIDelta>.Filter.Eq(ai => ai.type, OwnerTypeConstant.AI_DELTA);
        List<AIDelta> aisDelta = MongoDBSingleton.Instance().aiDeltaCollection.Find(aiDeltaFilter).ToList();
        var aiGammaFilter = Builders<AIGamma>.Filter.Eq(ai => ai.type, OwnerTypeConstant.AI_GAMMA);
        List<AIGamma> aisGamma = MongoDBSingleton.Instance().aiGammaCollection.Find(aiGammaFilter).ToList();


        //Lancement de  chacune des IAs sur un thread séparé
        var aiThreads = new List<Thread>();
        for (int i = 0; i < aisDelta.Count; i++)
        {
            var AId = aisDelta[i];
            var AIThread = new Thread(() => AId.runAI(factory));
            AIThread.Start();
            aiThreads.Add(AIThread);
            Thread.Sleep(1000);
        }
        for (int i = 0; i < aisGamma.Count; i++)
        {
            var AIg = aisGamma[i];
            var AIThread = new Thread(() => AIg.runAI(factory));
            AIThread.Start();
            aiThreads.Add(AIThread);
            Thread.Sleep(1000);
        }

        //On configure la distribution des Event et EventResult
        //Les Event et EventResults sont récupérés depuis les files correspondantes, et sont ensuite envoyées dans l'exchange aiEventsExchange
        //Qui va ensuite distribuer ces informations à toutes les IAs
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {

            channel.QueueDeclare(queue: "toSchedule",
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);
            channel.ExchangeDeclare(exchange: "aiEventsExchange",
                                        durable: true,
                                        type: ExchangeType.Direct);

            channel.QueueDeclare(queue: "aiReady",
                               durable: true,
                               exclusive: false,
                               autoDelete: false,
                               arguments: null);

            channel.QueueDeclare(queue: "aiEventResolve",
                               durable: true,
                               exclusive: false,
                               autoDelete: false,
                               arguments: null);

            var eventConsumer = new EventingBasicConsumer(channel);
            //Envoi des Event reçus sur la file aiReady vers l'exchange aiEventsExchange
            eventConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                channel.BasicPublish(exchange: "aiEventsExchange",
                    routingKey: "event",
                    basicProperties: null,
                    body: body);
            };
            //Envoi des EventResults reçus sur la file aiEventResolve vers l'exchange aiEventsExchange
            var eventResultConsumer = new EventingBasicConsumer(channel);
            eventResultConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                channel.BasicPublish(exchange: "aiEventsExchange",
                    routingKey: "eventResult",
                    basicProperties: null,
                    body: body);
            };

            channel.BasicConsume(queue: "aiReady",
                                 autoAck: true,
                                 consumer: eventConsumer);


            channel.BasicConsume(queue: "aiEventResolve",
                                 autoAck: true,
                                 consumer: eventResultConsumer);

            //on attend la terminaison de toutes les IAs avant de terminer le programme
            foreach (var i in aiThreads)
            {
                i.Join();
            }

        }        

    }


}



