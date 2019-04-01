using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using BSP.ReportDataConverter.Implementations;
using BSP.ReportDataConverter.Implementations.DynamicFields;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Config;
using Pinecone.ReportDataConverter.Config.Interfaces;
using Pinecone.ReportFormula.Interpreter.Lookup;
using R2A.ReportApi.Service.Infrastructure;
using R2A.ReportApi.Service.ReportSubmission;
using R2A.ReportApi.Service.ReportSubmission.PdfMailing;
using R2A.ReportApi.Service.ReportSubmission.Pipelines;
using RabbitMQ.Client;
using SendMailUtilities.Utilities;
using SendMailUtilities.Utilities.Config;
using SimpleInjector;
using Topshelf;
using Topshelf.SimpleInjector;
using LogFactory = R2A.ReportApi.Service.Infrastructure.LogFactory;

namespace R2A.ReportApi.Service
{
    class Program
    {
#if DEBUG
        private const bool IsDebug = true;
#else
        private const bool IsDebug = false;
#endif

        public class ServiceRoot
        {
            private readonly IMediator _mediator;
            private CancellationTokenSource _cancellationTokenSource;

            public ServiceRoot(IMediator mediator) {
                this._mediator = mediator;
            }

            public void OnStart()
            {
                //localization
                Thread.CurrentThread.CurrentCulture = _container.GetInstance<CultureInfo>();

                _cancellationTokenSource = new CancellationTokenSource();
                _mediator.Publish(new ServiceStartNotification(), _cancellationTokenSource.Token);
            }

            public void OnStop()
            {
                _cancellationTokenSource.Cancel();
                LogManager.Shutdown();
            }

            
        }

        private static readonly ManualResetEvent QuitEvent = new ManualResetEvent(false);
        private static readonly Container _container = new Container();
        public static void Main(string[] args)
        {
            if (IsDebug) {
                Console.WriteLine("Initiating ReportApi service...");
                //register to Ctrl-C
                Console.CancelKeyPress += (sender, eArgs) =>
                {
                    QuitEvent.Set();
                    eArgs.Cancel = true;
                };
            }

            //NLog
            LogManager.Configuration = new XmlLoggingConfiguration($"{AppContext.BaseDirectory}nlog.config");
            try
            {
                //appsettings.json configurations
                var logger = NLog.LogManager.GetCurrentClassLogger();
                logger.Info($"Inject configurations. Base path: {AppContext.BaseDirectory}");
                logger.Info($"Settings path: {AppContext.BaseDirectory}appsettings.json");
                var builder = new ConfigurationBuilder()
                    .AddJsonFile($"{AppContext.BaseDirectory}appsettings.json", optional: true);
                var configuration = builder.Build();
                var settings = configuration.GetSection("Settings").Get<Settings>();
                logger.Info($"Settings loaded {(settings?.DbConnectionString == null?"un":"")}successfully.");
                _container.RegisterInstance(settings);
                _container.RegisterInstance(configuration.GetSection("RabbitMqConnection").Get<ConnectionFactory>());
                _container.RegisterInstance(configuration.GetSection("MailConfiguration").Get<MailConfiguration>());
                //log factory
                logger.Info($"Log factory loaded {(LogManager.LogFactory == null ? "un" : "")}successfully.");
                _container.Register<ILogFactory,LogFactory>(Lifestyle.Singleton);

                //inject Mediatr and add pipelines as needed, in order of execution
                RegisterMediator(_container, new[]
                {
                    typeof(MetadataValidationBehavior),
                    typeof(XmlFileExtractionBehavior),
                    typeof(XsdSchemaValidationBehavior),
                    typeof(XmlMetadaValidationBehavior),
                    typeof(DynamicFieldsValidationBehavior),
                    typeof(TemplateRequirementCheckBehavior),
                    typeof(ReportFormulaValidatiorBehavior)
                });

                //localization
                var cultureInfo = new CultureInfo("en-PH")
                {
                    NumberFormat =
                    {
                        NumberDecimalDigits = 2,
                        PercentDecimalDigits = 2
                    }
                };
                _container.RegisterInstance(cultureInfo);

                //other services
                _container.Register<MessageDumpService>();
                _container.Register<ServiceRoot>();
                _container.Register<IDynamicOptionsRepository, DynamicOptionsRepositoryAdapter>();
                _container.Register<IDynamicOptionsService, DynamicOptionsService>();
                _container.Register<IDynamicDropdownDataResolver, ServiceDynamicDataResolver>();
                _container.Register<ILookupResolver>(() => BspLookupService.GetService(settings.DbConnectionString));
                _container.Register<SendMailUtilities.Utilities.ILogger,MailingLogAdapter>();
                _container.Register<EmailSender>();

                _container.Verify();
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(e, "Service initialization threw an exception.");
            }

            if (IsDebug)
            {
                var service = _container.GetInstance<ServiceRoot>();
                service.OnStart();
                Console.WriteLine("Service initiated. Press Ctrl-C to shut it down.");
                //quit on Ctrl-C
                QuitEvent.WaitOne();
                service.OnStop();
            }
            else
            {
                var rc = HostFactory.Run(x =>
                {
                    x.UseSimpleInjector(_container);
                    x.UseNLog();
                    x.Service<ServiceRoot>(s =>
                    {
                        s.ConstructUsingSimpleInjector();
                        s.WhenStarted(sr => sr.OnStart());
                        s.WhenStopped(sr => sr.OnStop());
                    });
                    x.RunAsLocalSystem();

                    x.SetDescription("A RabbitMQ message queue listener service for the R2A.ReportApi system.");
                    x.SetDisplayName("R2A.ReportApi.Service");
                    x.SetServiceName("R2A.ReportApi.Service");

                    x.OnException(ex =>
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error(ex);
                    });
                });

                var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
                Environment.ExitCode = exitCode;
            }


        }
        private static void RegisterMediator(Container container, IEnumerable<Type> pipelines)
        {
            var assemblies = new[] { typeof(Program).Assembly };
            container.RegisterSingleton<IMediator, Mediator>();
            container.Register(typeof(IRequestHandler<,>), assemblies);
            container.Register(typeof(IRequestHandler<>), assemblies);

            container.RegisterCollection(typeof(INotificationHandler<>),
                (IEnumerable<Type>)GetAllImplementations(container, typeof(INotificationHandler<>), assemblies));

            //mapping from specific to basic pipeline behaviour
            container.RegisterCollection(typeof(IPipelineBehavior<,>), pipelines);
            //container.Collections.AppendTo(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            //container.Collections.AppendTo(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            container.Collections.AppendTo(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>));
            container.Collections.AppendTo(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>));


            container.RegisterCollection(typeof(IRequestPreProcessor<>),
                (IEnumerable<Type>)GetAllImplementations(container, typeof(IRequestPreProcessor<>), assemblies));
            container.RegisterCollection(typeof(IRequestPostProcessor<,>),
                (IEnumerable<Type>)GetAllImplementations(container, typeof(IRequestPostProcessor<,>), assemblies));


            container.RegisterInstance(new SingleInstanceFactory(container.GetInstance));
            container.RegisterInstance(new MultiInstanceFactory(container.GetAllInstances));
        }

        private static IEnumerable<Type> GetAllImplementations(Container container, Type serviceType,
            Assembly[] assemblies)
        {
            return container.GetTypesToRegister(serviceType, assemblies,
                new TypesToRegisterOptions
                {
                    IncludeGenericTypeDefinitions = true,
                    IncludeComposites = false
                });
        }
    }
}