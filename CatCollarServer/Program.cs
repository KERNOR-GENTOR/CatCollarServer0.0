using CatCollarServer.Command;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CatCollarServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CommandFacad.CreateModels();
            //CreateHostBuilder(args).Build().Run();

            Task taskRecorder = Task.Factory.StartNew(() => { Recorder.Run(); });
            Task taskHttp = Task.Factory.StartNew(() => CreateHostBuilder(args).Build().Run());

            taskRecorder.Wait();
            taskHttp.Wait();

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
